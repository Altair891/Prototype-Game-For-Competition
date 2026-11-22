using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[System.Serializable]
public class Tile
{
    public int s, pv;                 // state, previous state (saat terbakar)
    public float t, d, hp = 100, m = 100, cl, bt, ox, oy;
    public void Reset() { s = 0; pv = 0; t = 0; d = 0; hp = 100; m = 100; cl = 0; bt = 0; }
}
[System.Serializable]
public class Wall { public float x, z, rep; public int st, hit; }
[System.Serializable]
public class Intr
{
    public float x, z, tt, h, fx, fz;
    public int mode, ty, wid, tk = -1, ti;   // mode: 0 cari, 1 beraksi, 2 kabur
    public bool ex, del, hasF;
    public GameObject go;
}

/// <summary>Seluruh logika game (tanpa grafik). Grafik membaca state ini dan mendengar event-nya.</summary>
public class Sim : MonoBehaviour
{
    public const int N = 30, WIN = 60, ZX = 1, ZZ = N - 9;
    public const float GX = ZX + 5.4f, GZ = ZZ + 2.2f, SEASON_LEN = 240f;
    public static Sim I;
    public static readonly string[] SeasonName = { "Plain", "Spring", "Autumn", "Winter" };
    public static readonly string[] IntName = { "Pembakar", "Penebang", "Penjarah", "Viking" };
    static readonly float[] PB = { 0, 0, 0, .10f, .18f, .28f };

    public Tile[] tl = new Tile[N * N];
    public List<Wall> wl = new List<Wall>();
    public List<Intr> ints = new List<Intr>();
    public List<int> burning = new List<int>();

    public float clock, st, qt, wet, speed = 1f, toastT;
    public int se, q, wid, wTot, wExp, coins, fires, waves, ms, brush = 2, adults;
    public bool pend = true, wa, win, paused = true, npc, guard, boost, up, started;
    public string toast = "";

    public event Action<int> Dirty;
    public event Action<string> Snd;
    public event Action<string, float, float> Fx;
    public event Action SeasonChanged, Restarted;
    public event Action<Intr> Spawned;
    public event Action<int> WallDirty;

    int tk; float ta;
    public float HoldTime { get { return guard ? 1.5f : 2.5f; } }
    public int RepairCost { get { int n = 0; foreach (var w in wl) if (w.st > 0) n++; return 15 * n; } }

    void Awake() { I = this; }

    /// <summary>Letak segmen tembok (urutan tetap). Dipakai Sim dan pembuat dunia permanen agar indeksnya selalu sama.</summary>
    public static List<Vector2> WallCells()
    {
        var l = new List<Vector2>();
        for (int z = ZZ; z < ZZ + 8; z++)
            for (int x = ZX; x < ZX + 8; x++)
            {
                bool edge = x == ZX || x == ZX + 7 || z == ZZ || z == ZZ + 7, gate = x == ZX + 7 && (z == ZZ + 3 || z == ZZ + 4);
                if (edge && !gate) l.Add(new Vector2(x + .5f, z + .5f));
            }
        return l;
    }

    public bool IsTree(int i) { return i >= 0 && tl[i].s >= 2 && tl[i].s <= 5; }
    void SetS(int i, int ns)
    {
        var T = tl[i];
        if (T.s == 6) burning.Remove(i);
        T.s = ns;
        if (ns == 6) burning.Add(i);
        if (Dirty != null) Dirty(i);
    }
    void Clr(int i) { var T = tl[i]; T.hp = 100; T.m = 100; T.t = 0; T.cl = 0; SetS(i, 0); }
    void Ignite(int i)
    {
        if (wet > 0) return;
        var T = tl[i]; T.pv = T.s; T.m = 100; SetS(i, 6);
    }
    public void Toast(string s) { toast = s; toastT = 5f; }
    void Sound(string n) { if (Snd != null) Snd(n); }
    void Emit(string k, float x, float z) { if (Fx != null) Fx(k, x, z); }

    public void NewGame()
    {
        clock = st = qt = wet = 0; se = 0; q = wid = wTot = wExp = coins = fires = waves = ms = 0;
        pend = true; wa = false; win = false; npc = guard = boost = up = false; tk = 0; ta = 0; toastT = 0; toast = "";
        foreach (var o in ints) if (o.go) Destroy(o.go);
        ints.Clear(); burning.Clear();
        for (int i = 0; i < N * N; i++)
        {
            if (tl[i] == null) tl[i] = new Tile();
            tl[i].Reset(); tl[i].ox = Random.Range(-.15f, .15f); tl[i].oy = Random.Range(-.15f, .15f);
        }
        for (int z = ZZ; z < ZZ + 8; z++) for (int x = ZX; x < ZX + 8; x++) tl[z * N + x].s = 7;
        for (int z = ZZ; z < ZZ + 7; z++) for (int x = ZX + 9; x < ZX + 16; x++) tl[z * N + x].s = 1;
        wl.Clear();
        foreach (var c in WallCells()) wl.Add(new Wall { x = c.x, z = c.y });
        for (int i = 0; i < N * N; i++) if (Dirty != null) Dirty(i);
        for (int j = 0; j < wl.Count; j++) if (WallDirty != null) WallDirty(j);
        if (Restarted != null) Restarted();
    }

    void Update()
    {
        if (toastT > 0) toastT -= Time.deltaTime;
        adults = 0;
        for (int i = 0; i < tl.Length; i++) if (tl[i] != null && tl[i].s == 5) adults++;
        if (paused || win) return;
        float rd = Mathf.Min(Time.deltaTime, .1f);
        Step(rd * speed);
    }

    void Step(float dt)
    {
        clock += dt; st += dt;
        if (st >= SEASON_LEN)
        {
            st = 0; se = (se + 1) % 4; pend = true; guard = boost = false;
            Sound("horn"); Toast("Musim berganti: " + SeasonName[se]);
            if (SeasonChanged != null) SeasonChanged();
        }
        float gm = boost ? 1.2f : 1f;
        for (int i = 0; i < tl.Length; i++)
        {
            var T = tl[i];
            if (T.s >= 2 && T.s <= 4)
            {
                T.t += dt * gm;
                if (T.t >= T.d) { T.t = 0; T.d = Random.Range(20f, 30f); SetS(i, T.s + 1); }
            }
        }
        wet = Mathf.Max(0, wet - dt);
        for (int j = 0; j < wl.Count; j++)
        {
            var w = wl[j];
            if (w.st > 0) { w.rep += dt; if (w.rep >= 60f) { w.st = 0; w.rep = 0; w.hit = 0; if (WallDirty != null) WallDirty(j); } }
        }
        ta += dt;
        while (ta >= 1f) { ta -= 1f; Tick(); }

        if (adults >= 5 && ms < 1) { ms = 1; Toast("Kebunmu mulai dikenal. Pengganggu pertama akan datang."); }
        if (adults >= 15 && ms < 2) { ms = 2; npc = true; Toast("Warga menetap di balik tembok. Klik mereka untuk berdagang."); }
        if (adults >= 30 && ms < 3) { ms = 3; Toast("Wilayahmu makmur. Kerajaan mulai memperhatikan."); }

        if (pend && adults >= 5) { pend = false; StartWave(); }
        if (wa && q > 0) { qt -= dt; if (qt <= 0) { SpawnIntruder(); q--; qt = 10f; } }
        UpdateIntruders(dt);
        if (wa && q == 0 && !ints.Exists(o => o.wid == wid))
        {
            wa = false; waves++;
            int g = 20 + 10 * wExp; coins += g; Sound("coin"); Toast("Gelombang selesai: +" + g + " koin");
        }
        if (adults >= WIN) win = true;
    }

    void StartWave()
    {
        int n = Mathf.Min(adults / 5, 6);
        wid++; wa = true; q = n; qt = 0; wTot = n; wExp = 0;
        Toast("Gelombang " + IntName[se] + ": " + n + " intruder datang dari tepi peta");
    }

    void SpawnIntruder()
    {
        int e = Random.Range(0, 4); float p = Random.value * N, x, z;
        if (e == 0) { x = p; z = -.5f; } else if (e == 1) { x = p; z = N + .5f; } else if (e == 2) { x = -.5f; z = p; } else { x = N + .5f; z = p; }
        var o = new Intr { x = x, z = z, ty = se, wid = wid };
        ints.Add(o);
        if (Spawned != null) Spawned(o);
    }

    void Tick()
    {
        var alive = new List<int>();
        foreach (var i in new List<int>(burning))
        {
            var T = tl[i]; T.hp -= 5;
            if (T.hp <= 0) Clr(i); else alive.Add(i);
        }
        if (++tk % 4 == 0)
            foreach (var i in alive)
            {
                int c = i % N, r = i / N;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int x = c + dx, y = r + dy;
                        if (x < 0 || y < 0 || x >= N || y >= N) continue;
                        int j = y * N + x; var T = tl[j];
                        if (T.s >= 2 && T.s <= 5 && wet <= 0 && Random.value < PB[T.s]) Ignite(j);
                    }
            }
    }

    // ---------- intruder ----------
    bool Ok(Intr o)
    {
        if (o.tk == 0) return IsTree(o.ti);
        if (o.tk == 1) return wl[o.ti].st < 2;
        return o.tk == 2;
    }
    public Vector2 TargetPos(Intr o)
    {
        if (o.tk == 0) return new Vector2(o.ti % N + .5f, o.ti / N + .5f);
        if (o.tk == 1) return new Vector2(wl[o.ti].x, wl[o.ti].z);
        return new Vector2(GX, GZ);
    }
    bool Claimed(Intr me, int kind, int idx)
    {
        foreach (var q2 in ints) if (q2 != me && q2.tk == kind && q2.ti == idx && q2.mode != 2) return true;
        return false;
    }
    bool Pick(Intr o)
    {
        if (o.ty == 2) { o.tk = 2; o.ti = 0; return true; }
        float bd = 1e9f; int b = -1;
        if (o.ty == 3)
        {
            for (int j = 0; j < wl.Count; j++)
            {
                if (wl[j].st >= 2 || Claimed(o, 1, j)) continue;
                float d = Vector2.Distance(new Vector2(wl[j].x, wl[j].z), new Vector2(o.x, o.z));
                if (d < bd) { bd = d; b = j; }
            }
            if (b < 0) return false; o.tk = 1; o.ti = b; return true;
        }
        for (int i = 0; i < tl.Length; i++)
        {
            if (!IsTree(i) || Claimed(o, 0, i)) continue;
            float d = Vector2.Distance(new Vector2(i % N + .5f, i / N + .5f), new Vector2(o.x, o.z));
            if (d < bd) { bd = d; b = i; }
        }
        if (b < 0) return false; o.tk = 0; o.ti = b; return true;
    }
    void Hit(Intr o)
    {
        if (o.tk == 2)
        {
            int n = wl.Exists(w => w.st == 2) ? 10 : 4, l = Mathf.Min(n, coins);
            coins -= l; Toast("Penjarah mencuri " + l + " koin"); Sound("bad");
        }
        else if (o.tk == 1)
        {
            var w = wl[o.ti];
            if (up) { w.hit++; if (w.hit % 2 == 1) return; }
            w.st++; w.rep = 0; Sound("bad");
            if (WallDirty != null) WallDirty(o.ti);
        }
        else
        {
            if (o.ty == 1) { Clr(o.ti); Sound("bad"); } else Ignite(o.ti);
        }
    }
    void UpdateIntruders(float dt)
    {
        foreach (var o in ints)
        {
            float sp = 2f * dt;
            if (o.mode == 0)
            {
                if (o.tk < 0 || !Ok(o)) { if (!Pick(o)) { o.mode = 2; continue; } }
                var tp = TargetPos(o); float dx = tp.x - o.x, dz = tp.y - o.z, d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < .7f) { o.mode = 1; o.tt = 3f + Random.value; } else { o.x += dx / d * sp; o.z += dz / d * sp; }
            }
            else if (o.mode == 1)
            {
                if (!Ok(o)) { o.mode = 0; o.tk = -1; continue; }
                o.tt -= dt;
                if (o.tt <= 0) { Hit(o); o.mode = 2; }
            }
            else
            {
                if (!o.hasF)
                {
                    float[] dd = { o.z, N - o.z, o.x, N - o.x }; int m = 0;
                    for (int k = 1; k < 4; k++) if (dd[k] < dd[m]) m = k;
                    if (m == 0) { o.fx = o.x; o.fz = -1; } else if (m == 1) { o.fx = o.x; o.fz = N + 1; } else if (m == 2) { o.fx = -1; o.fz = o.z; } else { o.fx = N + 1; o.fz = o.z; }
                    o.hasF = true;
                }
                float dx = o.fx - o.x, dz = o.fz - o.z, d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < .3f) o.del = true; else { o.x += dx / d * sp * 1.5f; o.z += dz / d * sp * 1.5f; }
            }
        }
        ints.RemoveAll(o => { if (o.del) { if (o.go) Destroy(o.go); return true; } return false; });
    }

    /// <summary>Dipanggil saat pemain menahan klik pada intruder (waktu nyata).</summary>
    public void Hold(Intr o, float rd)
    {
        if (o.mode == 2) return;
        o.h += rd;
        if (o.h >= HoldTime) { o.ex = true; o.mode = 2; wExp++; Toast("Intruder diusir"); Sound("ok"); }
    }

    // ---------- brush ----------
    public void Brush(float x, float z, float dt)
    {
        int r0 = Mathf.Max(0, Mathf.FloorToInt(z - 2)), r1 = Mathf.Min(N - 1, Mathf.CeilToInt(z + 2));
        int c0 = Mathf.Max(0, Mathf.FloorToInt(x - 2)), c1 = Mathf.Min(N - 1, Mathf.CeilToInt(x + 2));
        for (int r = r0; r <= r1; r++)
            for (int c = c0; c <= c1; c++)
            {
                int i = r * N + c; var T = tl[i];
                float dx = c + .5f - x, dz = r + .5f - z, d = Mathf.Sqrt(dx * dx + dz * dz);
                if (brush == 0 && d < .6f && T.s == 1)
                {
                    T.t = 0; T.d = Random.Range(20f, 30f); SetS(i, 2); Sound("plant");
                    for (int j = 0; j < 5; j++) Emit("sparkle", c + .5f, r + .5f);
                }
                else if (brush == 1 && d < 1.3f && T.s == 6)
                {
                    T.m -= 90f * dt; if (Random.value < .4f) Emit("rain", c + .5f, r + .5f);
                    if (T.m <= 0) { SetS(i, T.pv); fires++; Sound("ok"); Toast("Api padam"); }
                }
                else if (brush == 2 && d < 1.2f && T.s == 0)
                {
                    T.cl += dt * 3.5f; if (Random.value < .3f) Emit("dust", c + .5f, r + .5f);
                    if (T.cl >= 1f) { T.cl = 0; SetS(i, 1); } else if (Dirty != null) Dirty(i);
                }
                else if (brush == 2 && d < .6f && T.s == 8) { coins++; SetS(i, 1); Sound("ok"); }
                else if (brush == 3 && d < .6f && (T.s == 0 || T.s == 1) && coins >= 2)
                {
                    T.bt = clock; coins -= 2; SetS(i, 8); Sound("build");
                    for (int j = 0; j < 6; j++) Emit("sparkle", c + .5f, r + .5f);
                }
            }
    }

    // ---------- toko ----------
    public bool CanBuy(int npcI, int item)
    {
        if (npcI == 0 && item == 0) return RepairCost > 0 && coins >= RepairCost;
        if (npcI == 0 && item == 1) return !up && coins >= 50;
        if (npcI == 1) return coins >= 25;
        if (npcI == 2) return coins >= 40;
        return coins >= 30;
    }
    public void Buy(int npcI, int item)
    {
        if (!CanBuy(npcI, item)) return;
        if (npcI == 0 && item == 0) { coins -= RepairCost; for (int j = 0; j < wl.Count; j++) { wl[j].st = 0; wl[j].rep = 0; wl[j].hit = 0; if (WallDirty != null) WallDirty(j); } }
        else if (npcI == 0) { coins -= 50; up = true; }
        else if (npcI == 1) { coins -= 25; wet = 30f; }
        else if (npcI == 2) { coins -= 40; guard = true; }
        else { coins -= 30; boost = true; }
        Sound("coin");
    }

    public string Hint()
    {
        if (toastT > 0) return toast;
        int left = Mathf.CeilToInt(SEASON_LEN - st);
        if (left <= 30 && adults >= 5) return "Gelombang " + IntName[(se + 1) % 4] + " datang dalam " + left + " detik";
        int clean = 0, trees = 0;
        for (int i = 0; i < tl.Length; i++) { int s = tl[i].s; if (s == 1) clean++; if (s >= 2 && s <= 6) trees++; }
        if (clean == 0 && trees == 0) return "Pilih Sapu (3) lalu usap tanah kotor";
        if (clean > 0 && trees == 0) return "Pilih Benih (1) lalu usap tanah bersih";
        if (pend) return "Tumbuhkan 5 pohon dewasa agar gelombang pertama datang";
        return "";
    }
}
