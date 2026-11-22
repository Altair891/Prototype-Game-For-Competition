using System.Collections.Generic;
using UnityEngine;

public enum Mode { Menu, Game }

/// <summary>Seluruh UI (menu utama gaya Tiny Glade, HUD, toko, jeda, pengaturan) memakai IMGUI: tanpa prefab / package tambahan.</summary>
public class GameUI : MonoBehaviour
{
    public Mode mode = Mode.Menu;
    public bool pauseMenu, how, story, settings, photo, showMap = true, showFps;
    public int shopNpc = -1;

    Sim S; World W; CamRig C; Sfx A;
    Texture2D logo, tPanel, tEdge, tBtn, tHov, tOn, tBar, tVig, tGlow, tShade, tWarm, tWhite, tDim;
    GUIStyle st;
    List<Rect> rects = new List<Rect>(), next = new List<Rect>();
    float u = 1f;
    int shadowLevel = 2, aaLevel = 2;
    static readonly Color[] ACC = { new Color(.42f, .7f, .35f), new Color(.9f, .55f, .7f), new Color(.91f, .56f, .18f), new Color(.5f, .66f, .88f) };
    static readonly string[] SHADOW = { "Mati", "Rendah", "Tinggi" };

    float hudNext, fpsAcc, winT, lastAdj; int fpsN, winN; bool autoQ = true;
    string sTimer = "", sWave = "", sTrees = "", sCoin = "", sWalls = "", sHint = "", sFx = "", sFps = "";
    static readonly string[] BN = { "Benih (1)", "Hujan (2)", "Sapu (3)", "Pagar (4)" };

    /// <summary>Teks HUD dihitung 4x per detik (bukan tiap event OnGUI) agar tidak ada alokasi string/GC tiap frame.</summary>
    void Update()
    {
        if (S == null) return;
        fpsAcc += Time.unscaledDeltaTime; fpsN++;
        winT += Time.unscaledDeltaTime; winN++;
        if (winT >= 4f)
        {
            float avg = winN / winT; winT = 0; winN = 0;
            if (autoQ && mode == Mode.Game && !S.paused && avg < 42f && Time.unscaledTime - lastAdj > 8f)
            {
                lastAdj = Time.unscaledTime;
                if (Gfx.Level > 0) { Gfx.Level--; ApplyQuality(); SaveP(); S.Toast("Kualitas grafis diturunkan otomatis agar FPS stabil"); }
                else if (Gfx.Reflections) { Gfx.Reflections = false; SaveP(); S.Toast("Refleksi air dimatikan otomatis agar FPS stabil"); }
            }
        }
        if (Time.unscaledTime < hudNext) return;
        hudNext = Time.unscaledTime + .25f;
        if (showFps) sFps = Mathf.RoundToInt(fpsN / Mathf.Max(fpsAcc, .001f)) + " FPS   " + (fpsAcc / Mathf.Max(fpsN, 1) * 1000f).ToString("0.0") + " ms";
        fpsAcc = 0; fpsN = 0;
        if (mode != Mode.Game) return;
        int left = Mathf.CeilToInt(Sim.SEASON_LEN - S.st); sTimer = left + " dtk";
        int act = S.q; foreach (var o in S.ints) if (o.wid == S.wid) act++;
        sWave = S.wa ? "Gelombang " + Sim.IntName[S.se] + ": " + act + " intruder" : S.pend ? "Gelombang menunggu 5 pohon dewasa" : "Gelombang berikutnya " + left + " dtk";
        sTrees = S.adults + "/" + Sim.WIN; sCoin = "Koin " + S.coins;
        int wu = 0; foreach (var wl in S.wl) if (wl.st == 0) wu++; sWalls = "Tembok " + wu + "/" + S.wl.Count;
        sHint = S.Hint();
        string fx = ""; if (S.wet > 0) fx += "Air Berkah " + Mathf.CeilToInt(S.wet) + "s   "; if (S.guard) fx += "Penjaga   "; if (S.boost) fx += "Bibit Unggul   "; if (S.up) fx += "Tembok kuat"; sFx = fx;
    }
    void OnApplicationQuit() { Gfx.Restore(); }

    public void Setup(Sim s, World w, CamRig c, Sfx a)
    {
        S = s; W = w; C = c; A = a;
        logo = Resources.Load<Texture2D>("Logo");
        tPanel = U.Solid(new Color(.29f, .19f, .11f, .86f)); tEdge = U.Solid(new Color(.16f, .1f, .06f, .95f));
        tBtn = U.Solid(new Color(.45f, .31f, .18f, .95f)); tHov = U.Solid(new Color(.58f, .41f, .24f, .98f)); tOn = U.Solid(new Color(.3f, .55f, .24f, .98f));
        tBar = U.Solid(new Color(.1f, .06f, .03f, .9f)); tWhite = U.Solid(Color.white); tDim = U.Solid(new Color(.05f, .09f, .06f, .72f));
        tWarm = U.Solid(new Color(1f, .78f, .5f, .06f));
        tVig = U.Radial(128, new Color(.16f, .08f, .03f), 0f, .6f, 2.6f);
        tGlow = U.Radial(64, new Color(1f, .93f, .75f), .5f, 0f, 1.2f);
        tShade = new Texture2D(64, 1, TextureFormat.RGBA32, false); tShade.wrapMode = TextureWrapMode.Clamp;
        for (int i = 0; i < 64; i++) tShade.SetPixel(i, 0, new Color(.05f, .12f, .1f, Mathf.Lerp(.45f, 0f, i / 63f))); tShade.Apply();
        showMap = PlayerPrefs.GetInt("sotg_map", 1) == 1; shadowLevel = PlayerPrefs.GetInt("sotg_shadow", 2); aaLevel = PlayerPrefs.GetInt("sotg_aa", 2);
        A.mute = PlayerPrefs.GetInt("sotg_mute", 0) == 1; Gfx.Level = PlayerPrefs.GetInt("sotg_gfx", 1); Gfx.Reflections = PlayerPrefs.GetInt("sotg_refl", 1) == 1; showFps = PlayerPrefs.GetInt("sotg_fps", 0) == 1; autoQ = PlayerPrefs.GetInt("sotg_auto", 1) == 1;
        ApplyQuality();
    }

    void ApplyQuality()
    {
        Gfx.Apply();
        QualitySettings.shadows = shadowLevel == 0 ? ShadowQuality.Disable : shadowLevel == 1 ? ShadowQuality.HardOnly : ShadowQuality.All;
        QualitySettings.shadowResolution = shadowLevel == 2 ? ShadowResolution.VeryHigh : ShadowResolution.Medium;
        QualitySettings.antiAliasing = aaLevel == 0 ? 0 : aaLevel == 1 ? 2 : 4;
    }
    void SaveP() { PlayerPrefs.SetInt("sotg_map", showMap ? 1 : 0); PlayerPrefs.SetInt("sotg_shadow", shadowLevel); PlayerPrefs.SetInt("sotg_aa", aaLevel); PlayerPrefs.SetInt("sotg_mute", A.mute ? 1 : 0); PlayerPrefs.SetInt("sotg_gfx", Gfx.Level); PlayerPrefs.SetInt("sotg_refl", Gfx.Reflections ? 1 : 0); PlayerPrefs.SetInt("sotg_fps", showFps ? 1 : 0); PlayerPrefs.SetInt("sotg_auto", autoQ ? 1 : 0); PlayerPrefs.Save(); }

    // ---------- status ----------
    public bool Blocking { get { return mode == Mode.Menu || how || story || settings || pauseMenu || (S != null && S.win); } }
    public bool Over(Vector2 mp)
    {
        if (Blocking) return true;
        Vector2 g = new Vector2(mp.x, Screen.height - mp.y);
        foreach (var r in rects) if (r.Contains(g)) return true;
        return false;
    }
    public void StartGame(bool fresh)
    {
        if (fresh || !S.started) { S.NewGame(); S.started = true; S.Toast("Sapu tanah kotor, lalu tanam benih"); }
        mode = Mode.Game; S.paused = false; pauseMenu = false; shopNpc = -1;
    }
    public void ToMenu() { mode = Mode.Menu; S.paused = true; pauseMenu = false; shopNpc = -1; photo = false; }
    public void OnEsc()
    {
        if (how || story || settings) { how = story = settings = false; return; }
        if (photo) { photo = false; return; }
        if (shopNpc >= 0) { shopNpc = -1; return; }
        if (mode == Mode.Game && !S.win) { pauseMenu = !pauseMenu; S.paused = pauseMenu; }
    }

    // ---------- helper gambar ----------
    void Reg(Rect r) { if (Event.current.type == EventType.Repaint) next.Add(r); }
    void Box(Rect r, Texture2D fill, float bw)
    {
        GUI.DrawTexture(r, tEdge); GUI.DrawTexture(new Rect(r.x + bw, r.y + bw, r.width - 2 * bw, r.height - 2 * bw), fill);
    }
    void Panel(Rect r) { Reg(r); Box(r, tPanel, 2.5f * u); }
    void Txt(Rect r, string s, float size, Color c, TextAnchor a = TextAnchor.MiddleLeft, bool bold = false)
    {
        st.fontSize = Mathf.RoundToInt(size); st.alignment = a; st.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        st.normal.textColor = new Color(0, 0, 0, .5f); GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), s, st);
        st.normal.textColor = c; GUI.Label(r, s, st);
    }
    bool Btn(Rect r, string s, bool on = false, float size = 15f)
    {
        Reg(r); bool hov = r.Contains(Event.current.mousePosition);
        Box(r, on ? tOn : hov ? tHov : tBtn, 2f * u);
        bool c = GUI.Button(r, GUIContent.none, GUIStyle.none);
        Txt(r, s, size * u, Color.white, TextAnchor.MiddleCenter, true);
        return c;
    }
    void Bar(Rect r, float f, Color c)
    {
        GUI.DrawTexture(r, tBar); var fr = new Rect(r.x, r.y, r.width * Mathf.Clamp01(f), r.height); GUI.color = c; GUI.DrawTexture(fr, tWhite); GUI.color = Color.white;
    }

    void Init()
    {
        if (st != null) return;
        st = new GUIStyle(GUI.skin.label); st.wordWrap = false; st.clipping = TextClipping.Overflow; st.richText = false;
        try { var f = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Trebuchet MS", "Arial", "Helvetica", "Verdana" }, 24); if (f) st.font = f; } catch { }
    }

    void OnGUI()
    {
        if (S == null || W == null || !W.Ready) return;
        Init(); u = Mathf.Max(.7f, Screen.height / 800f);
        if (Event.current.type == EventType.Repaint) next.Clear();
        if (mode == Mode.Menu) DrawMenu(); else DrawGame();
        DrawModals();
        if (Event.current.type == EventType.Repaint) { var t = rects; rects = next; next = t; }
    }

    // ---------- menu utama ----------
    void DrawMenu()
    {
        float w0 = Screen.width, h0 = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, w0 * .6f, h0), tShade);
        GUI.DrawTexture(new Rect(0, 0, w0, h0), tVig);
        float lw = Mathf.Min(w0 * .4f, 440f * u), lh = logo ? lw * logo.height / logo.width : lw * .7f;
        var lr = new Rect(w0 * .07f, h0 * .06f, lw, lh);
        if (logo) GUI.DrawTexture(lr, logo, ScaleMode.ScaleToFit, true);
        else Txt(lr, "Seasons of the Grove", 46 * u, Color.white, TextAnchor.MiddleCenter, true);
        float cx = lr.x + lw / 2f, y = lr.yMax + h0 * .05f, iw = 380f * u, ih = 58f * u;
        var items = new List<string>(); if (S.started && !S.win) items.Add("LANJUTKAN"); items.Add("BARU"); items.Add("CARA BERMAIN");
        foreach (var it in items)
        {
            var r = new Rect(cx - iw / 2f, y, iw, ih); Reg(r); bool hov = r.Contains(Event.current.mousePosition);
            Txt(r, it, (hov ? 42f : 38f) * u, Color.white, TextAnchor.MiddleCenter, true);
            if (hov) GUI.DrawTexture(new Rect(cx - 90f * u, r.yMax - 6f * u, 180f * u, 3f * u), tWhite);
            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
            {
                if (it == "LANJUTKAN") StartGame(false); else if (it == "BARU") StartGame(true); else how = true;
            }
            y += ih + 6f * u;
        }
        y += h0 * .05f; float sq = 62f * u, gap = 26f * u, x0 = cx - (3 * sq + 2 * gap) / 2f;
        string[] lab = { "CERITA", "PENGATURAN", "LAYAR PENUH" }, glyph = { "i", "*", "[ ]" };
        for (int i = 0; i < 3; i++)
        {
            var r = new Rect(x0 + i * (sq + gap), y, sq, sq); Reg(r); bool hov = r.Contains(Event.current.mousePosition);
            GUI.color = new Color(1, 1, 1, hov ? .5f : .3f); GUI.DrawTexture(r, tWhite); GUI.color = Color.white;
            Txt(r, glyph[i], 26 * u, Color.white, TextAnchor.MiddleCenter, true);
            Txt(new Rect(r.x - 30 * u, r.yMax + 6 * u, sq + 60 * u, 22 * u), lab[i], 12 * u, Color.white, TextAnchor.MiddleCenter, true);
            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
            {
                if (i == 0) story = true; else if (i == 1) settings = true; else Screen.fullScreen = !Screen.fullScreen;
            }
        }
        var sr = new Rect(w0 - 86f * u, 22f * u, 62f * u, 62f * u); Reg(sr);
        GUI.color = new Color(1, 1, 1, sr.Contains(Event.current.mousePosition) ? .5f : .3f); GUI.DrawTexture(sr, tWhite); GUI.color = Color.white;
        Txt(sr, A.mute ? "OFF" : "SND", 16 * u, Color.white, TextAnchor.MiddleCenter, true);
        if (GUI.Button(sr, GUIContent.none, GUIStyle.none)) { A.mute = !A.mute; SaveP(); }
        Txt(new Rect(w0 - 100f * u, 90f * u, 90f * u, 24f * u), "v1.0", 15 * u, Color.white, TextAnchor.MiddleRight);
    }

    // ---------- HUD ----------
    void DrawGame()
    {
        float w0 = Screen.width, h0 = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, w0, h0), tVig); GUI.DrawTexture(new Rect(0, 0, w0, h0), tWarm);
        if (showFps) Txt(new Rect(w0 / 2f - 110 * u, 4 * u, 220 * u, 22 * u), sFps, 13 * u, Color.white, TextAnchor.MiddleCenter);
        bool sv; Vector2 sp = W.SunScreen(out sv);
        if (sv && Night() < .8f) { float gs = 700f * u; GUI.color = new Color(1, 1, 1, Mathf.Clamp01(W.Dn)); GUI.DrawTexture(new Rect(sp.x - gs / 2, h0 - sp.y - gs / 2, gs, gs), tGlow); GUI.color = Color.white; }
        if (photo) { if (Btn(new Rect(w0 - 190 * u, 10 * u, 180 * u, 36 * u), "Keluar mode foto")) photo = false; return; }

        // kiri atas: logo + musim
        float x = 10 * u; if (logo) { var lr = new Rect(x, 10 * u, 96 * u, 66 * u); Reg(lr); GUI.DrawTexture(lr, logo, ScaleMode.ScaleToFit, true); x += 104 * u; }
        var sc = new Rect(x, 10 * u, 250 * u, 70 * u); Panel(sc);
        Txt(new Rect(sc.x + 10 * u, sc.y + 4 * u, 150 * u, 26 * u), Sim.SeasonName[S.se], 20 * u, Color.white, TextAnchor.MiddleLeft, true);
        Txt(new Rect(sc.xMax - 90 * u, sc.y + 4 * u, 80 * u, 26 * u), sTimer, 15 * u, Color.white, TextAnchor.MiddleRight);
        Bar(new Rect(sc.x + 10 * u, sc.y + 32 * u, sc.width - 20 * u, 9 * u), S.st / Sim.SEASON_LEN, ACC[S.se]);
        string wv = sWave;
        Txt(new Rect(sc.x + 10 * u, sc.y + 44 * u, sc.width - 14 * u, 22 * u), wv, 12 * u, Color.white);

        // kanan atas: statistik + tombol
        var st2 = new Rect(w0 - 250 * u, 10 * u, 240 * u, 84 * u); Panel(st2);
        Txt(new Rect(st2.x + 10 * u, st2.y + 4 * u, 160 * u, 24 * u), "Pohon dewasa", 15 * u, Color.white);
        Txt(new Rect(st2.xMax - 90 * u, st2.y + 4 * u, 80 * u, 24 * u), sTrees, 16 * u, Color.white, TextAnchor.MiddleRight, true);
        Bar(new Rect(st2.x + 10 * u, st2.y + 30 * u, st2.width - 20 * u, 9 * u), S.adults / (float)Sim.WIN, ACC[S.se]);
        Txt(new Rect(st2.x + 10 * u, st2.y + 44 * u, 110 * u, 30 * u), sCoin, 16 * u, new Color(1f, .86f, .4f), TextAnchor.MiddleLeft, true);
        Txt(new Rect(st2.x + 120 * u, st2.y + 44 * u, 110 * u, 30 * u), sWalls, 14 * u, Color.white);
        float bx = w0 - 250 * u, by = 102 * u, bw = 58 * u, bh = 34 * u;
        if (Btn(new Rect(bx, by, bw, bh), "Jeda", false, 13)) { pauseMenu = true; S.paused = true; }
        if (Btn(new Rect(bx + (bw + 4 * u), by, bw, bh), "x" + S.speed, false, 13)) S.speed = S.speed == 1f ? 3f : 1f;
        if (Btn(new Rect(bx + 2 * (bw + 4 * u), by, bw, bh), "Fokus", false, 13)) C.Focus();
        if (Btn(new Rect(bx + 3 * (bw + 4 * u), by, bw, bh), "Foto", false, 13)) photo = true;
        if (Btn(new Rect(bx, by + bh + 4 * u, bw, bh), "Kiri", false, 13)) C.Yaw -= .4f;
        if (Btn(new Rect(bx + (bw + 4 * u), by + bh + 4 * u, bw, bh), "Kanan", false, 13)) C.Yaw += .4f;
        if (Btn(new Rect(bx + 2 * (bw + 4 * u), by + bh + 4 * u, bw, bh), A.mute ? "OFF" : "SND", false, 13)) { A.mute = !A.mute; SaveP(); }
        if (Btn(new Rect(bx + 3 * (bw + 4 * u), by + bh + 4 * u, bw, bh), "Peta", showMap, 13)) { showMap = !showMap; SaveP(); }

        // bawah: hotbar + pesan
        string[] bn = BN; float hw = 112 * u, hh = 52 * u, hx = (w0 - 4 * hw - 3 * 6 * u) / 2f, hy = h0 - hh - 14 * u;
        Panel(new Rect(hx - 8 * u, hy - 8 * u, 4 * hw + 3 * 6 * u + 16 * u, hh + 16 * u));
        for (int i = 0; i < 4; i++) if (Btn(new Rect(hx + i * (hw + 6 * u), hy, hw, hh), bn[i], S.brush == i, 16)) S.brush = i;
        string msg = sHint;
        if (msg != "") { var mr = new Rect(w0 / 2f - 290 * u, hy - 52 * u, 580 * u, 36 * u); Reg(mr); GUI.DrawTexture(mr, tDim); Txt(mr, msg, 15 * u, Color.white, TextAnchor.MiddleCenter, false); }
        string fx = sFx;
        if (fx != "") Txt(new Rect(12 * u, h0 - 40 * u, 420 * u, 28 * u), fx, 14 * u, Color.white);

        // peta mini
        if (showMap)
        {
            float ms = 150 * u; var mr = new Rect(w0 - ms - 14 * u, h0 - ms - 14 * u, ms, ms); Panel(new Rect(mr.x - 4 * u, mr.y - 4 * u, ms + 8 * u, ms + 8 * u));
            GUI.DrawTextureWithTexCoords(mr, W.GroundTex, new Rect(0, 1, 1, -1));
            float k = ms / Sim.N; Rect P(float px, float pz, float sz) { return new Rect(mr.x + px * k - sz / 2, mr.y + pz * k - sz / 2, sz, sz); }
            foreach (var wl in S.wl) { GUI.color = wl.st == 0 ? new Color(.6f, .58f, .54f) : wl.st == 1 ? new Color(.85f, .63f, .22f) : new Color(.75f, .22f, .16f); GUI.DrawTexture(P(wl.x, wl.z, k), tWhite); }
            GUI.color = new Color(1, .2f, .2f); foreach (var o in S.ints) GUI.DrawTexture(P(o.x, o.z, 6 * u), tWhite);
            GUI.color = Color.white; GUI.DrawTexture(P(W.Farmer.position.x, W.Farmer.position.z, 7 * u), tWhite);
            GUI.color = new Color(1, .83f, .3f); var cr = P(C.Ts.x, C.Ts.z, 16 * u); GUI.DrawTexture(new Rect(cr.x, cr.y, cr.width, 2), tWhite); GUI.DrawTexture(new Rect(cr.x, cr.yMax - 2, cr.width, 2), tWhite); GUI.DrawTexture(new Rect(cr.x, cr.y, 2, cr.height), tWhite); GUI.DrawTexture(new Rect(cr.xMax - 2, cr.y, 2, cr.height), tWhite); GUI.color = Color.white;
            var ev = Event.current;
            if (ev.type == EventType.MouseDown && ev.button == 0 && mr.Contains(ev.mousePosition)) { C.T.x = (ev.mousePosition.x - mr.x) / ms * Sim.N; C.T.z = (ev.mousePosition.y - mr.y) / ms * Sim.N; C.follow = false; ev.Use(); }
        }
        DrawShop();
    }
    float Night() { return W.Night; }

    void DrawShop()
    {
        if (shopNpc < 0 || !S.npc) return;
        float w0 = Screen.width; var r = new Rect(w0 - 330 * u, 190 * u, 320 * u, 230 * u); Panel(r);
        Txt(new Rect(r.x + 12 * u, r.y + 6 * u, 290 * u, 28 * u), World.NpcName(shopNpc), 19 * u, Color.white, TextAnchor.MiddleLeft, true);
        Txt(new Rect(r.x + 12 * u, r.y + 34 * u, 290 * u, 24 * u), "Koin: " + S.coins, 15 * u, new Color(1f, .86f, .4f));
        string[][] it = {
            new[] { "Perbaiki semua tembok - " + S.RepairCost, "Tembok kuat - 50" },
            new[] { "Air Berkah (api tak menyebar, 30 dtk) - 25" }, new[] { "Penjaga (usir 1,5 dtk, satu musim) - 40" }, new[] { "Bibit Unggul (+20% tumbuh, satu musim) - 30" } };
        float y = r.y + 62 * u;
        for (int j = 0; j < it[shopNpc].Length; j++)
        {
            bool can = S.CanBuy(shopNpc, j); GUI.enabled = can;
            if (Btn(new Rect(r.x + 12 * u, y, r.width - 24 * u, 42 * u), it[shopNpc][j], false, 13)) S.Buy(shopNpc, j);
            GUI.enabled = true; y += 48 * u;
        }
        if (Btn(new Rect(r.x + 12 * u, r.yMax - 46 * u, r.width - 24 * u, 36 * u), "Tutup", false, 14)) shopNpc = -1;
    }

    // ---------- modal ----------
    void Modal(string title, string body, System.Action extra = null)
    {
        float w0 = Screen.width, h0 = Screen.height; GUI.DrawTexture(new Rect(0, 0, w0, h0), tDim);
        float pw = Mathf.Min(620f * u, w0 - 30), ph = Mathf.Min(560f * u, h0 - 30); var r = new Rect((w0 - pw) / 2f, (h0 - ph) / 2f, pw, ph); Panel(r);
        Txt(new Rect(r.x + 18 * u, r.y + 10 * u, pw - 36 * u, 34 * u), title, 24 * u, Color.white, TextAnchor.MiddleLeft, true);
        st.wordWrap = true; Txt(new Rect(r.x + 18 * u, r.y + 52 * u, pw - 36 * u, ph - 130 * u), body, 15 * u, Color.white, TextAnchor.UpperLeft); st.wordWrap = false;
        if (extra != null) extra();
        if (Btn(new Rect(r.center.x - 70 * u, r.yMax - 54 * u, 140 * u, 40 * u), "Tutup", false, 16)) { how = story = settings = false; }
    }

    void DrawModals()
    {
        float w0 = Screen.width, h0 = Screen.height;
        if (S.win && mode == Mode.Game)
        {
            GUI.DrawTexture(new Rect(0, 0, w0, h0), tDim); var r = new Rect(w0 / 2 - 260 * u, h0 / 2 - 170 * u, 520 * u, 340 * u); Panel(r);
            Txt(new Rect(r.x, r.y + 14 * u, r.width, 40 * u), "Hutan sudah pulih", 30 * u, Color.white, TextAnchor.MiddleCenter, true);
            int m = Mathf.FloorToInt(S.clock / 60), sc = Mathf.FloorToInt(S.clock % 60);
            st.wordWrap = true; Txt(new Rect(r.x + 24 * u, r.y + 66 * u, r.width - 48 * u, 150 * u), Sim.WIN + " pohon dewasa tumbuh bersamaan. Petani itu kini kaya dan disegani.\n\nWaktu " + m + " menit " + sc + " detik\n" + S.fires + " kebakaran dipadamkan, " + S.waves + " gelombang bertahan, " + S.coins + " koin.", 16 * u, Color.white, TextAnchor.UpperCenter); st.wordWrap = false;
            if (Btn(new Rect(r.x + 30 * u, r.yMax - 64 * u, 210 * u, 44 * u), "Main lagi", false, 16)) { S.NewGame(); S.started = true; S.paused = false; }
            if (Btn(new Rect(r.xMax - 240 * u, r.yMax - 64 * u, 210 * u, 44 * u), "Menu utama", false, 16)) ToMenu();
        }
        else if (pauseMenu && mode == Mode.Game)
        {
            GUI.DrawTexture(new Rect(0, 0, w0, h0), tDim); var r = new Rect(w0 / 2 - 170 * u, h0 / 2 - 170 * u, 340 * u, 340 * u); Panel(r);
            Txt(new Rect(r.x, r.y + 10 * u, r.width, 40 * u), "Jeda", 26 * u, Color.white, TextAnchor.MiddleCenter, true);
            float y = r.y + 62 * u;
            if (!how && !story && !settings)
            {
                if (Btn(new Rect(r.x + 30 * u, y, r.width - 60 * u, 48 * u), "Lanjut", false, 17)) { pauseMenu = false; S.paused = false; }
                if (Btn(new Rect(r.x + 30 * u, y + 56 * u, r.width - 60 * u, 48 * u), "Cara Bermain", false, 17)) how = true;
                if (Btn(new Rect(r.x + 30 * u, y + 112 * u, r.width - 60 * u, 48 * u), "Pengaturan", false, 17)) settings = true;
                if (Btn(new Rect(r.x + 30 * u, y + 168 * u, r.width - 60 * u, 48 * u), "Menu Utama", false, 17)) ToMenu();
            }
        }
        if (how) Modal("Cara Bermain",
            "TUJUAN\nTumbuhkan " + Sim.WIN + " pohon dewasa bersamaan. Tidak ada kondisi kalah; serangan hanya memperlambat.\n\n" +
            "ALAT (angka 1-4)\n1 Benih: tanam di tanah bersih. Beri jarak, api merambat ke tetangga.\n2 Hujan: usap pohon terbakar sampai bar biru habis.\n3 Sapu: ubah tanah kotor jadi bersih; bongkar pagar.\n4 Pagar: tembok batu 2 koin, jadi sekat api dan ditumbuhi sulur.\n\n" +
            "MUSUH MUSIMAN\nTiap pergantian musim datang satu gelombang (peringatan 30 detik).\nPlain: Pembakar menyulut. Spring: Penebang menebang. Autumn: Penjarah mencuri koin. Winter: Viking mendobrak tembok.\nMusim hanya mengganti tampilan dan jenis musuh, bukan kesulitan.\n\n" +
            "KONTROL\nKlik-tahan intruder 2,5 detik untuk mengusir. Setelah 15 pohon dewasa, klik warga untuk berdagang.\nKamera: WASD/panah geser, klik kanan + geser putar, roda zoom ke kursor, klik tengah + geser.\nKlik petani atau tekan F untuk fokus ke petani. H mode foto, Esc jeda.");
        if (story) Modal("Cerita",
            "Abad ke-10. Di luar wilayah sebuah kerajaan, seorang petani pohon mewarisi tanah gundul penuh puing.\n\nDengan sabar ia menyapu, menanam, dan merawat. Kebunnya tumbuh, warga mulai menetap di balik tembok, dan koin mengalir lewat dagang.\n\nNamun kerajaan terdekat punya banyak musuh. Tiap musim berganti, penyerang datang: pembakar, penebang, penjarah, dan Viking dari utara. Petani harus menjaga kebun dan wilayahnya.\n\nKalau " + Sim.WIN + " pohon berdiri bersamaan, ia akan kaya dan disegani.");
        if (settings) Modal("Pengaturan", "", () =>
        {
            float pw = Mathf.Min(620f * u, w0 - 30), ph = Mathf.Min(560f * u, h0 - 30), x = (w0 - pw) / 2f + 40 * u, y = (h0 - ph) / 2f + 56 * u, bw = 200 * u, bh = 40 * u;
            Txt(new Rect(x, y, 200 * u, bh), "Suara", 17 * u, Color.white); if (Btn(new Rect(x + 260 * u, y, bw, bh), A.mute ? "Mati" : "Nyala")) { A.mute = !A.mute; SaveP(); }
            y += 46 * u; Txt(new Rect(x, y, 200 * u, bh), "Bayangan", 17 * u, Color.white); if (Btn(new Rect(x + 260 * u, y, bw, bh), SHADOW[shadowLevel])) { shadowLevel = (shadowLevel + 1) % 3; ApplyQuality(); SaveP(); }
            y += 46 * u; Txt(new Rect(x, y, 200 * u, bh), "Anti-aliasing", 17 * u, Color.white); if (Btn(new Rect(x + 260 * u, y, bw, bh), aaLevel == 0 ? "Mati" : aaLevel == 1 ? "2x" : "4x")) { aaLevel = (aaLevel + 1) % 3; ApplyQuality(); SaveP(); }
            y += 46 * u; Txt(new Rect(x, y, 200 * u, bh), "Peta mini", 17 * u, Color.white); if (Btn(new Rect(x + 260 * u, y, bw, bh), showMap ? "Tampil" : "Sembunyi")) { showMap = !showMap; SaveP(); }
            y += 46 * u; Txt(new Rect(x, y, 200 * u, bh), "Layar penuh", 17 * u, Color.white); if (Btn(new Rect(x + 260 * u, y, bw, bh), Screen.fullScreen ? "Nyala" : "Mati")) Screen.fullScreen = !Screen.fullScreen;
            y += 46 * u; Txt(new Rect(x, y, 200 * u, bh), "Kualitas grafis", 17 * u, Color.white); if (Btn(new Rect(x + 260 * u, y, bw, bh), Gfx.Level == 0 ? "Rendah" : Gfx.Level == 1 ? "Sedang" : "Tinggi")) { Gfx.Level = (Gfx.Level + 1) % 3; ApplyQuality(); SaveP(); }
            y += 46 * u; Txt(new Rect(x, y, 200 * u, bh), "Refleksi air", 17 * u, Color.white); if (Btn(new Rect(x + 260 * u, y, bw, bh), Gfx.Reflections ? "Nyala" : "Mati")) { Gfx.Reflections = !Gfx.Reflections; SaveP(); }
            y += 46 * u; Txt(new Rect(x, y, 200 * u, bh), "Tampilkan FPS", 17 * u, Color.white); if (Btn(new Rect(x + 260 * u, y, bw, bh), showFps ? "Nyala" : "Mati")) { showFps = !showFps; SaveP(); }
            y += 46 * u; Txt(new Rect(x, y, 200 * u, bh), "Kualitas otomatis", 17 * u, Color.white); if (Btn(new Rect(x + 260 * u, y, bw, bh), autoQ ? "Nyala" : "Mati")) { autoQ = !autoQ; SaveP(); }
        });
    }
}
