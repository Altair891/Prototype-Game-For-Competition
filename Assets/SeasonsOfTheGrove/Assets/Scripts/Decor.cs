using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Ornamen penghias dunia di luar area bermain: perbukitan, batu bertekstur (bercelah dan berlumut), batu besar, formasi batu bertingkat,
/// lingkaran batu berdiri, pilar/dataran batu berumput, rumah terbengkalai, reruntuhan, kerajaan besar di kejauhan, bunga, jamur,
/// tunggul, batang tumbang, semak, alang-alang, dan dermaga. Semuanya statis dan digabung (Static Batching) agar ringan.
/// </summary>
public static class Decor
{
    const int N = Sim.N;
    class Hill { public float x, z, r, h; public int seed; public bool far, king; }
    class RM { public Mesh m; public int[] map; }
    static readonly List<Hill> hills = new List<Hill>();
    static readonly List<Vector3> block = new List<Vector3>();
    static readonly Dictionary<string, RM> cache = new Dictionary<string, RM>();
    static Material hillM, farM, rockM, woodM, woodD, darkM;
    static Texture2D rockT, woodT;
    static Hill king;
    public static bool Bake;
    public static Material HillMat { get { return hillM; } }
    public static Material FarMat { get { return farM; } }
    public static void Adopt(Material h, Material f) { hillM = h; farM = f; }
    public static void Reset() { hills.Clear(); block.Clear(); cache.Clear(); Perches.Clear(); hillM = null; farM = null; rockM = null; woodM = null; woodD = null; darkM = null; rockT = null; woodT = null; king = null; rng = new System.Random(1234); Bake = false; }
    public static readonly List<Vector3> Perches = new List<Vector3>();
    static System.Random rng = new System.Random(1234);
    static float R() { return (float)rng.NextDouble(); }
    static float R(float a, float b) { return a + (b - a) * R(); }

    // ================= perencanaan =================
    static bool InLake(float x, float z, float rx, float rz) { float a = (x - (N / 2f + 8f)) / rx, b = (z + 14f) / rz; return a * a + b * b < 1f; }
    static void Block(float x, float z, float r) { block.Add(new Vector3(x, z, r)); }

    public static void Plan()
    {
        hills.Clear(); block.Clear(); Perches.Clear(); rng = new System.Random(1234);
        Block(-14, 6, 7); Block(44, 24, 7); Block(30, 52, 6); Block(-12, 36, 8); Block(48, 40, 8); Block(8, 53, 8); Block(-26, -10, 9);
        Block(54, 8, 6); Block(-9, -2, 5); Block(22, 47, 5); Block(38, 56, 7);
        for (int i = 0; i < 16; i++)
            for (int t = 0; t < 20; t++)
            {
                float ang = R() * 6.283f, d = R(58f, 92f), x = 15 + Mathf.Cos(ang) * d, z = 15 + Mathf.Sin(ang) * d;
                if (InLake(x, z, 34f, 19f) || Mathf.Abs(x + 80) < 40 && Mathf.Abs(z + 42) < 40) continue;
                hills.Add(new Hill { x = x, z = z, r = R(18f, 32f), h = R(4f, 11f), seed = i * 7 + 3 }); break;
            }
        king = new Hill { x = -80, z = -42, r = 54, h = 16, seed = 5, king = true }; hills.Add(king);
        for (int i = 0; i < 9; i++) hills.Add(new Hill { x = 15 + Mathf.Cos(i * .72f + R() * .3f) * R(150f, 185f), z = 15 + Mathf.Sin(i * .72f) * R(150f, 185f), r = R(55f, 75f), h = R(22f, 34f), seed = 90 + i, far = true });
    }

    public static bool Blocked(float x, float z)
    {
        if (king != null) { float kx = x - king.x, kz = z - king.z; if (kx * kx + kz * kz < 3600f) return true; }
        return BlockedSites(x, z);
    }
    static bool BlockedSites(float x, float z) { foreach (var b in block) { float dx = x - b.x, dz = z - b.y; if (dx * dx + dz * dz < b.z * b.z) return true; } return false; }

    static float Hy(Hill H, float x, float z)
    {
        float dx = x - H.x, dz = z - H.z, d = Mathf.Sqrt(dx * dx + dz * dz) / H.r; if (d >= 1f) return 0f;
        if (H.king) { float k = Mathf.Clamp01((d - .72f) / .28f); k = k * k * (3f - 2f * k); return H.h * (1f - k); }   // dataran rata untuk kastil
        float shape = Mathf.Pow(Mathf.Cos(d * Mathf.PI * .5f), 1.7f), n = (Mathf.PerlinNoise(x * .06f + H.seed, z * .06f + H.seed * .5f) - .5f) * .55f;
        return Mathf.Max(0f, H.h * shape * (1f + n));
    }
    public static float HeightAt(float x, float z) { float h = 0; foreach (var H in hills) h = Mathf.Max(h, Hy(H, x, z)); return h; }
    /// <summary>Titik di tanah. Di lereng curam objek ditenggelamkan sedikit agar tidak melayang di atas mesh bukit yang berfacet.</summary>
    static Vector3 G(float x, float z)
    {
        float h = HeightAt(x, z), gx = HeightAt(x + 1f, z) - HeightAt(x - 1f, z), gz = HeightAt(x, z + 1f) - HeightAt(x, z - 1f);
        float slope = (Mathf.Abs(gx) + Mathf.Abs(gz)) * .5f;
        return new Vector3(x, Mathf.Max(0f, h - .06f - Mathf.Min(.6f, slope * .3f)), z);
    }
    public static float GroundY(float x, float z) { return G(x, z).y; }

    // ================= bahan & tekstur =================
    static float Hh(int a) { unchecked { a = (a ^ 61) ^ (a >> 16); a *= 9; a ^= a >> 4; a *= 0x27d4eb2d; a ^= a >> 15; return (a & 0xffff) / 65535f; } }
    static Texture2D RockTex()
    {
        if (rockT) return rockT;
        int n = 128; var t = new Texture2D(n, n, TextureFormat.RGBA32, true); t.wrapMode = TextureWrapMode.Repeat; var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float n1 = Mathf.PerlinNoise(x * .07f, y * .07f), n2 = Mathf.PerlinNoise(x * .22f + 9f, y * .22f + 4f), n3 = Mathf.PerlinNoise(x * .04f + 20f, y * .04f);
                float g = Mathf.Lerp(.34f, .68f, n1 * .6f + n2 * .4f); float crack = Mathf.Abs(Mathf.PerlinNoise(x * .05f + 40f, y * .05f + 11f) - .5f) < .025f ? .6f : 1f;
                px[y * n + x] = new Color(g * (1.05f + (n3 - .5f) * .2f) * crack, g * crack, g * (.93f - (n3 - .5f) * .1f) * crack, 1f);
            }
        t.SetPixels(px); t.Apply(true); return rockT = t;
    }
    static Texture2D WoodTex()
    {
        if (woodT) return woodT;
        int n = 128; var t = new Texture2D(n, n, TextureFormat.RGBA32, true); t.wrapMode = TextureWrapMode.Repeat; var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int pl = x / 16; float v = Hh(pl) * .3f, grain = Mathf.PerlinNoise(x * .4f + pl * 7f, y * .03f), k = (x % 16) < 1 ? .4f : (.8f + grain * .4f);
                px[y * n + x] = new Color((.42f + v * .3f) * k, (.33f + v * .25f) * k, (.25f + v * .2f) * k, 1f);
            }
        t.SetPixels(px); t.Apply(true); return woodT = t;
    }
    static void Mats()
    {
        if (hillM != null) return;
        hillM = new Material(U.Mat(new Color(.5f, .64f, .35f), .02f)); farM = new Material(U.Mat(new Color(.55f, .65f, .5f), .02f));
        rockM = U.TexMat(RockTex(), Color.white, .05f); woodM = U.TexMat(WoodTex(), new Color(.8f, .76f, .72f), .05f); woodD = U.TexMat(WoodTex(), new Color(.45f, .42f, .4f), .04f); darkM = U.Mat(new Color(.04f, .035f, .03f), .02f);
    }
    /// <summary>Warna rumput bukit/lumut mengikuti musim.</summary>
    public static void Recolor(Color grass, Color sky) { Mats(); hillM.color = grass; farM.color = Color.Lerp(grass, sky, .55f); }

    // ================= bukit =================
    static Mesh HillMesh(Hill H)
    {
        int seg = 34, rings = H.king ? 18 : 10; var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        v.Add(new Vector3(0, Hy(H, H.x, H.z) - .06f, 0)); uv.Add(new Vector2(H.x, H.z) * .05f);
        for (int ri = 1; ri <= rings; ri++)
            for (int si = 0; si < seg; si++)
            {
                float a = si * 6.2832f / seg, d = ri / (float)rings * H.r, dx = Mathf.Cos(a) * d, dz = Mathf.Sin(a) * d;
                v.Add(new Vector3(dx, Hy(H, H.x + dx, H.z + dz) - .06f, dz)); uv.Add(new Vector2(H.x + dx, H.z + dz) * .05f);
            }
        int I(int ri, int si) { return ri == 0 ? 0 : 1 + (ri - 1) * seg + (si % seg); }
        void T(int a, int b, int c) { if (Vector3.Cross(v[b] - v[a], v[c] - v[a]).y < 0) { int tmp = b; b = c; c = tmp; } t.Add(a); t.Add(b); t.Add(c); }
        for (int si = 0; si < seg; si++) T(0, I(1, si), I(1, si + 1));
        for (int ri = 1; ri < rings; ri++) for (int si = 0; si < seg; si++) { T(I(ri, si), I(ri, si + 1), I(ri + 1, si + 1)); T(I(ri, si), I(ri + 1, si + 1), I(ri + 1, si)); }
        var m = new Mesh(); m.indexFormat = IndexFormat.UInt32; m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m;
    }

    static void BuildHills(Transform root, Transform trees)
    {
        foreach (var H in hills)
        {
            var g = new GameObject("hill"); g.transform.SetParent(root, false); g.transform.position = new Vector3(H.x, 0, H.z);
            g.AddComponent<MeshFilter>().sharedMesh = HillMesh(H); var mr = g.AddComponent<MeshRenderer>(); mr.sharedMaterial = H.far ? farM : hillM; mr.shadowCastingMode = ShadowCastingMode.Off;
            if (H.far) continue;
            int cnt = (int)(H.r * .55f);
            for (int i = 0; i < cnt; i++)
            {
                float a = R() * 6.283f, d = Mathf.Sqrt(R()) * H.r * .82f, x = H.x + Mathf.Cos(a) * d, z = H.z + Mathf.Sin(a) * d;
                if (H.king && d < 40f) continue; if (!H.king && Blocked(x, z)) continue;
                int sp = TreeKit.Pick(x * .5f, z * .5f, R());
                TreeKit.Spawn(trees, G(x, z), R() * 360f, sp, TreeKit.Tint(sp, R()), R(1.1f, 2.3f), 5, false, true);
            }
        }
    }

    // ================= batu =================
    static float Perl(float x, float y, float z) { return (Mathf.PerlinNoise(x, y) + Mathf.PerlinNoise(y + 5.2f, z + 1.3f) + Mathf.PerlinNoise(z + 9.1f, x + 3.7f)) / 3f; }

    /// <summary>Batu low-poly berpermukaan kasar; bidang yang menghadap atas menjadi lumut (submesh 2).</summary>
    static RM Rock(int seed, float rough, float sx, float sy, float sz)
    {
        string key = "r" + seed + "," + rough + "," + sx + "," + sy + "," + sz; RM rm; if (cache.TryGetValue(key, out rm)) return rm;
        var src = Kit.LowSphere(1); var vs = src.vertices; var ts = src.triangles; var rock = new Kit.MB(); var moss = new Kit.MB();
        Vector3 D(Vector3 v)
        {
            Vector3 d = v.normalized; float n = (Perl(d.x * 2.1f + seed, d.y * 2.1f, d.z * 2.1f) - .5f) * 2f * rough; Vector3 p = d * (1f + n);
            p = new Vector3(p.x * sx, p.y * sy, p.z * sz); if (p.y < -sy * .35f) p.y = -sy * .35f; return p;
        }
        for (int i = 0; i < ts.Length; i += 3)
        {
            Vector3 a = D(vs[ts[i]]), b = D(vs[ts[i + 1]]), c = D(vs[ts[i + 2]]), cen = (a + b + c) / 3f; Vector3 nm = Vector3.Cross(b - a, c - a); if (Vector3.Dot(nm, cen) < 0) nm = -nm; nm.Normalize();
            (nm.y > .6f ? moss : rock).Tri(a, b, c, cen);
        }
        return cache[key] = Join(rock, moss);
    }
    static RM Join(Kit.MB rock, Kit.MB moss)
    {
        var parts = new List<Kit.PartSpec>();
        if (rock.t.Count > 0) parts.Add(new Kit.PartSpec { m = rock.Build(), x = Matrix4x4.identity, g = 0 });
        if (moss.t.Count > 0) parts.Add(new Kit.PartSpec { m = moss.Build(), x = Matrix4x4.identity, g = 1 });
        var rm = new RM(); rm.m = Kit.Merge(parts, 2, out rm.map); return rm;
    }

    /// <summary>Pilar/dataran batu bertingkat dengan puncak berumput (seperti gambar 2).</summary>
    static RM Pillar(int seed, float r, float h, int tiers)
    {
        string key = "p" + seed + "," + r + "," + h; RM rm; if (cache.TryGetValue(key, out rm)) return rm;
        var rock = new Kit.MB(); var moss = new Kit.MB(); int seg = 10; var ring = new Vector3[tiers + 1][];
        for (int t = 0; t <= tiers; t++)
        {
            ring[t] = new Vector3[seg]; float y = h * t / tiers, rad = r * (1f - .22f * t / tiers) * (t == 0 ? 1.18f : 1f) * (t % 2 == 0 ? 1.07f : .92f);
            for (int s = 0; s < seg; s++)
            {
                float ang = s * 6.2832f / seg, n = 1f + (Perl(ang * 1.3f + seed * .7f, t * 1.7f, seed) - .5f) * .55f;
                ring[t][s] = new Vector3(Mathf.Cos(ang) * rad * n, y + (Perl(s * .9f, t * 2.3f, seed) - .5f) * h * .05f, Mathf.Sin(ang) * rad * n);
            }
        }
        for (int t = 0; t < tiers; t++)
            for (int s = 0; s < seg; s++)
            {
                Vector3 a = ring[t][s], b = ring[t][(s + 1) % seg], c = ring[t + 1][(s + 1) % seg], d = ring[t + 1][s], o = (a + b + c + d) / 4f; o.y = 0;
                rock.Tri(a, b, c, o); rock.Tri(a, c, d, o);
            }
        Vector3 cen = new Vector3(0, h + r * .14f, 0);
        for (int s = 0; s < seg; s++) moss.Tri(cen, ring[tiers][s], ring[tiers][(s + 1) % seg], Vector3.up);
        return cache[key] = Join(rock, moss);
    }

    static GameObject Put(Transform p, RM rm, Vector3 pos, float yaw, bool shadow)
    {
        var g = new GameObject("rock"); g.transform.SetParent(p, false); g.transform.position = pos; g.transform.rotation = Quaternion.Euler(0, yaw, 0);
        g.AddComponent<MeshFilter>().sharedMesh = rm.m; var r = g.AddComponent<MeshRenderer>(); var mats = new Material[rm.map.Length];
        for (int k = 0; k < mats.Length; k++) mats[k] = rm.map[k] == 0 ? rockM : hillM; r.sharedMaterials = mats; r.shadowCastingMode = shadow ? ShadowCastingMode.On : ShadowCastingMode.Off; return g;
    }
    static void PutRock(Transform root, int seed, float rough, float sx, float sy, float sz, float x, float z, float yaw, bool shadow)
    {
        Vector3 p = G(x, z); p.y += sy * .3f; Put(root, Rock(seed, rough, sx, sy, sz), p, yaw, shadow);
    }

    static void Boulders(Transform root, float cx, float cz, int seed)
    {
        PutRock(root, seed * 3, .3f, 2.3f, 1.8f, 2.1f, cx, cz, R() * 360f, true); Perches.Add(G(cx, cz) + Vector3.up * 2.25f);
        for (int k = 0; k < 6; k++) { float a = R() * 6.283f, d = R(1.8f, 3.6f); PutRock(root, seed * 10 + k, .32f, R(.5f, 1.2f), R(.4f, .9f), R(.5f, 1.1f), cx + Mathf.Cos(a) * d, cz + Mathf.Sin(a) * d, R() * 360f, true); }
    }
    static void Layered(Transform root, float cx, float cz)
    {
        for (int k = 0; k < 6; k++) { Vector3 p = G(cx, cz); p.y += k * .42f + .15f; Put(root, Rock(40 + k, .2f, 3.4f - k * .45f, .5f, 2.8f - k * .4f), p + new Vector3(R(-.2f, .2f), 0, R(-.2f, .2f)), R() * 360f, true); }
        for (int k = 0; k < 4; k++) { float a = R() * 6.283f; PutRock(root, 55 + k, .3f, R(.5f, .9f), R(.4f, .7f), R(.5f, .9f), cx + Mathf.Cos(a) * 3.4f, cz + Mathf.Sin(a) * 3.0f, R() * 360f, true); }
    }
    static void StandingStones(Transform root, float cx, float cz)
    {
        for (int k = 0; k < 9; k++) { float a = k * 6.283f / 9f; PutRock(root, 70 + k, .16f, .5f, 1.9f + R() * .5f, .42f, cx + Mathf.Cos(a) * 4.3f, cz + Mathf.Sin(a) * 4.3f, a * 57.3f, true); Perches.Add(G(cx + Mathf.Cos(a) * 4.3f, cz + Mathf.Sin(a) * 4.3f) + Vector3.up * 2.45f); }
        PutRock(root, 90, .15f, 1.7f, .55f, 1.2f, cx, cz, 20f, true);
        for (int k = 0; k < 3; k++) PutRock(root, 95 + k, .3f, .7f, .45f, .6f, cx + R(-2f, 2f), cz + R(-2f, 2f), R() * 360f, true);
    }

    static void BuildRocks(Transform root)
    {
        Boulders(root, 54, 8, 1); Boulders(root, -9, -2, 2); Boulders(root, 22, 47, 3); Layered(root, 38, 56); StandingStones(root, -26, -10);
        float[][] pl = { new[] { 75f, 42f, 5f, 15f }, new[] { -62f, 38f, 6f, 20f }, new[] { 18f, 100f, 7f, 18f }, new[] { 92f, -5f, 4.5f, 12f }, new[] { -40f, -58f, 5.5f, 16f }, new[] { 62f, 88f, 4f, 10f }, new[] { -85f, 70f, 6.5f, 22f }, new[] { 110f, 50f, 5f, 17f } };
        for (int i = 0; i < pl.Length; i++) { Vector3 p = G(pl[i][0], pl[i][1]); p.y -= .3f; Put(root, Pillar(11 + i, pl[i][2], pl[i][3], 5), p, R() * 360f, false); }
        for (int i = 0; i < 110; i++)
        {
            float a = R() * 6.283f, d = R(19f, 62f), x = 15 + Mathf.Cos(a) * d, z = 15 + Mathf.Sin(a) * d;
            if (x > -1.5f && x < N + 1.5f && z > -1.5f && z < N + 1.5f || InLake(x, z, 30f, 15f) || Blocked(x, z) && R() < .6f) continue;
            float q = R(.25f, .9f); PutRock(root, i % 9, .32f, q * R(.8f, 1.4f), q * R(.5f, .9f), q * R(.8f, 1.3f), x, z, R() * 360f, false);
        }
    }

    // ================= rumah terbengkalai =================
    static void AbHouse(Transform root, float x, float z, float yaw, float sc)
    {
        var g = new GameObject("abandoned").transform; g.SetParent(root, false); g.position = G(x, z); g.rotation = Quaternion.Euler(0, yaw, 0); g.localScale = Vector3.one * sc;
        GameObject P(Mesh m, Material mt, Vector3 p, Vector3 e) { return Kit.Part(g, m, mt, p, e, true); }
        var sph = Kit.Sphere(1, true); Perches.Add(g.position + new Vector3(0, 2.6f * sc, 0));
        P(Kit.BoxC(3.2f, .1f, 2.6f), woodD, new Vector3(0, .05f, 0), Vector3.zero);
        P(Kit.BoxC(3.2f, 1.7f, .12f), woodM, new Vector3(0, .95f, -1.3f), Vector3.zero);
        P(Kit.BoxC(.12f, 1.7f, 2.6f), woodM, new Vector3(-1.6f, .95f, 0), new Vector3(0, 0, 3));
        P(Kit.BoxC(.12f, .9f, 2.6f), woodD, new Vector3(1.6f, .5f, 0), new Vector3(0, 0, -6));
        P(Kit.BoxC(1.15f, 1.7f, .12f), woodM, new Vector3(-1.02f, .95f, 1.3f), Vector3.zero);
        P(Kit.BoxC(.9f, 1.3f, .12f), woodM, new Vector3(1.1f, .75f, 1.3f), new Vector3(0, 0, -5));
        P(Kit.BoxC(1.0f, .2f, .12f), woodD, new Vector3(0, 1.55f, 1.3f), new Vector3(0, 0, 6));
        P(Kit.BoxC(.95f, 1.35f, .03f), darkM, new Vector3(0, .78f, 1.18f), Vector3.zero);
        P(Kit.BoxC(2.0f, .08f, 3.0f), woodD, new Vector3(-.85f, 2.05f, 0), new Vector3(0, 0, 28));
        P(Kit.BoxC(1.4f, .08f, 3.0f), woodD, new Vector3(1.0f, 1.45f, .1f), new Vector3(0, 0, -34));
        P(Kit.BoxC(.14f, .14f, 3.1f), woodD, new Vector3(0, 2.45f, 0), Vector3.zero);
        for (int i = 0; i < 5; i++) P(Kit.BoxC(.08f, 1.6f, .08f), woodD, new Vector3(.75f, 1.75f, -1.0f + i * .5f), new Vector3(0, 0, -30 + i * 4));
        P(Kit.BoxC(.5f, 2.3f, .5f), Kit.Stone, new Vector3(-1.3f, 1.15f, -1.0f), Vector3.zero); P(Kit.BoxC(.5f, .3f, .5f), Kit.Stone, new Vector3(-1.3f, 2.4f, -1.0f), new Vector3(10, 0, 8));
        for (int i = 0; i < 12; i++) Kit.Sc(P(sph, hillM, new Vector3(R(-1.6f, 1.6f), R(.1f, 2.1f), R(-1.4f, 1.4f)), Vector3.zero), R(.25f, .5f), R(.12f, .25f), R(.2f, .45f));
        for (int i = 0; i < 6; i++) P(Kit.FrC(4, .015f, .01f, .9f, false), hillM, new Vector3(R(-1.5f, 1.5f), 1.9f - R(0, .4f), 1.35f), new Vector3(180, 0, R(-8, 8)));
        P(Kit.FrC(10, .2f, .22f, .5f), woodD, new Vector3(1.9f, .25f, 1.9f), new Vector3(0, 20, 80));
        for (int i = 0; i < 5; i++) P(Kit.FrC(6, .05f, .045f, .9f), woodD, new Vector3(-2.4f + i * .55f, 0, 2.3f), new Vector3((i % 2) * 8 - 4, 0, (i % 3) * 6 - 6));
        P(Kit.BoxC(2.4f, .06f, .06f), woodD, new Vector3(-1.3f, .7f, 2.3f), new Vector3(0, 0, 5));
        for (int i = 0; i < 8; i++) P(Kit.BoxC(R(.8f, 1.4f), .05f, .22f), woodD, new Vector3(R(-2.8f, 2.8f), .04f, R(-2.4f, 2.8f)), new Vector3(0, R(0, 180), R(-8, 8)));
        for (int i = 0; i < 4; i++) P(Kit.FrC(5, .05f, 0f, .35f, false), hillM, new Vector3(R(-2f, 2f), 0, R(2f, 3f)), Vector3.zero);
    }

    // ================= reruntuhan =================
    static void Ruin(Transform root, float x, float z, float yaw, int seed)
    {
        var g = new GameObject("ruin").transform; g.SetParent(root, false); g.position = G(x, z); g.rotation = Quaternion.Euler(0, yaw, 0);
        var sph = Kit.Sphere(1, true);
        for (int ix = -2; ix <= 2; ix++) for (int iz = -2; iz <= 2; iz++) if (R() > .3f) Kit.Part(g, Kit.BoxC(.95f, .1f, .95f), Kit.StoneDark, new Vector3(ix, .05f, iz), new Vector3(0, R(-4, 4), 0));
        for (int k = 0; k < 14; k++)
        {
            float a = k * 6.283f / 14f; int lv = 1 + (int)(R() * 3.2f);
            for (int l = 0; l < lv; l++) Kit.Part(g, Kit.BoxC(.85f, .45f, .55f), Kit.Stone, new Vector3(-3.2f + Mathf.Cos(a) * 1.6f, .25f + l * .46f, -2.4f + Mathf.Sin(a) * 1.6f), new Vector3(0, -a * 57.3f + R(-5, 5), 0));
        }
        for (int k = 0; k < 8; k++) { int lv = 1 + (int)(R() * 3.4f); for (int l = 0; l < lv; l++) Kit.Part(g, Kit.BoxC(.9f, .45f, .5f), Kit.Stone, new Vector3(-3f + k * .9f, .25f + l * .46f, 2.7f), new Vector3(0, R(-4, 4), 0)); }
        float[] ch = { 2.4f, 1.7f, .9f };
        for (int k = 0; k < 3; k++) { var c = Kit.Part(g, Kit.FrC(10, .34f, .3f, ch[k]), Kit.Stone, new Vector3(1.8f + k * 1.3f, 0, 0.4f), Vector3.zero); Kit.Part(g, Kit.BoxC(.7f, .22f, .7f), Kit.Stone, new Vector3(1.8f + k * 1.3f, ch[k] + .1f, .4f), new Vector3(R(-12, 12), R(0, 90), R(-12, 12))); }
        Kit.Part(g, Kit.FrC(10, .34f, .3f, 2.2f), Kit.Stone, new Vector3(1.5f, .3f, -1.8f), new Vector3(0, 30, 90)); Kit.Part(g, Kit.FrC(10, .34f, .3f, 1.8f), Kit.Stone, new Vector3(3.8f, .3f, -1.2f), new Vector3(0, -50, 90));
        for (int i = 0; i < 6; i++) { float a = Mathf.PI * (i + .5f) / 9f; Kit.Part(g, Kit.BoxC(.5f, .26f, .4f), Kit.Stone, new Vector3(-1.0f, 1.6f + Mathf.Sin(a) * .9f, 2.0f - Mathf.Cos(a) * .9f + 2.2f), new Vector3(a * 57.3f - 90f, 0, 0)); }
        for (int i = 0; i < 10; i++) { float q = R(.15f, .4f); Kit.Sc(Kit.Part(g, Kit.Sphere(1, false), rockM, new Vector3(R(-3.5f, 3.5f), q * .3f, R(-3.5f, 3.5f))), q * 1.2f, q * .7f, q); }
        for (int i = 0; i < 16; i++) Kit.Sc(Kit.Part(g, sph, hillM, new Vector3(R(-3.5f, 3.5f), R(.05f, 1.6f), R(-3.5f, 3.5f))), R(.2f, .45f), R(.1f, .22f), R(.2f, .4f));
    }

    // ================= kerajaan besar =================
    static void BuildKingdom(Transform root)
    {
        float y = king.h - .06f; float yaw = Mathf.Atan2(15 - king.x, 15 - king.z) * Mathf.Rad2Deg;
        var g = new GameObject("castle").transform; g.SetParent(root, false); g.position = new Vector3(king.x, y, king.z); g.rotation = Quaternion.Euler(0, yaw, 0); g.localScale = Vector3.one * 2.6f;
        var win = new List<Material>(); float S = 8f; var wall = Kit.Stone;
        Kit.Part(g, Kit.BoxC(2 * S + 5f, 5f, 2 * S + 5f), Kit.StoneDark, new Vector3(0, -2.4f, 0));   // fondasi batu: menutup celah dengan tanah
        Kit.Part(g, Kit.BoxC(2 * S + 1.2f, 2.4f, 1f), wall, new Vector3(0, 1.2f, -S)); Kit.Part(g, Kit.BoxC(1f, 2.4f, 2 * S + 1.2f), wall, new Vector3(-S, 1.2f, 0)); Kit.Part(g, Kit.BoxC(1f, 2.4f, 2 * S + 1.2f), wall, new Vector3(S, 1.2f, 0));
        Kit.Part(g, Kit.BoxC(S - 1.5f, 2.4f, 1f), wall, new Vector3(-(S + 1.5f) / 2f - .2f, 1.2f, S)); Kit.Part(g, Kit.BoxC(S - 1.5f, 2.4f, 1f), wall, new Vector3((S + 1.5f) / 2f + .2f, 1.2f, S));
        for (int i = 0; i < 20; i++)
        {
            float t = -S + .3f + i * (2 * S - .6f) / 19f;
            Kit.Part(g, Kit.BoxC(.55f, .55f, 1f), wall, new Vector3(t, 2.68f, -S)); Kit.Part(g, Kit.BoxC(1f, .55f, .55f), wall, new Vector3(-S, 2.68f, t)); Kit.Part(g, Kit.BoxC(1f, .55f, .55f), wall, new Vector3(S, 2.68f, t));
            if (Mathf.Abs(t) > 2.6f) Kit.Part(g, Kit.BoxC(.55f, .55f, 1f), wall, new Vector3(t, 2.68f, S));
        }
        Castle.SquareTower(g, new Vector3(-2.6f, 0, S), 1.9f, 4.4f); Castle.SquareTower(g, new Vector3(2.6f, 0, S), 1.9f, 4.4f); Kit.Part(g, Kit.BoxC(3.4f, 1.1f, 1.4f), wall, new Vector3(0, 3.8f, S));
        Castle.RoundTower(g, new Vector3(-S, 0, -S), 1.5f, 5.4f, true); Castle.RoundTower(g, new Vector3(S, 0, -S), 1.5f, 5.0f, false); Castle.RoundTower(g, new Vector3(-S, 0, S), 1.5f, 5.2f, false); Castle.RoundTower(g, new Vector3(S, 0, S), 1.5f, 5.6f, true);
        Castle.SquareTower(g, new Vector3(2.8f, 0, -2.6f), 3.2f, 7.6f); Castle.SpireTower(g, new Vector3(-3.2f, 0, -3f), 2.2f, 9.4f); Castle.Octagon(g, new Vector3(-3.2f, 0, 3.2f), 2.4f, 3.0f);
        Castle.House(g, new Vector3(3.2f, 0, 3.6f), 2.4f, 1.9f, 1.6f, 1.2f, win, true, .8f); Castle.House(g, new Vector3(0, 0, 5.2f), 1.9f, 1.6f, 1.4f, 1.0f, win, true, .6f);
        foreach (var tp in new[] { new Vector3(0, 8.6f, 0) }) { }
        Color[] fc = { new Color(.75f, .22f, .17f), new Color(.16f, .43f, .69f), new Color(.85f, .63f, .22f) };
        float[][] fp = { new[] { 2.8f, 7.9f, -2.6f }, new[] { -3.2f, 11.2f, -3f }, new[] { -S, 7.4f, -S }, new[] { S, 7.6f, S } };
        for (int i = 0; i < fp.Length; i++) { Kit.Part(g, Kit.FrC(6, .02f, .02f, 1.2f), U.Mat(new Color(.3f, .22f, .15f), .05f), new Vector3(fp[i][0], fp[i][1], fp[i][2])); Kit.Part(g, Kit.BoxC(.9f, .5f, .03f), U.Mat(fc[i % 3], .05f), new Vector3(fp[i][0] + .45f, fp[i][1] + .9f, fp[i][2]), Vector3.zero, false); }
        // desa di kaki bukit: setiap rumah berdiri di tinggi tanah sebenarnya + fondasi batu
        Quaternion q = Quaternion.Euler(0, yaw, 0);
        for (int k = 0; k < 16; k++)
        {
            for (int t = 0; t < 8; t++)
            {
                float ox = R(-34f, 34f), oz = R(40f, 58f); Vector3 wp = new Vector3(king.x, 0, king.z) + q * new Vector3(ox, 0, oz);
                if (BlockedSites(wp.x, wp.z) || InLake(wp.x, wp.z, 34f, 19f)) continue;
                var hg = new GameObject("house").transform; hg.SetParent(root, false); hg.position = G(wp.x, wp.z); hg.rotation = Quaternion.Euler(0, yaw + 180f + R(-35f, 35f), 0); hg.localScale = Vector3.one * 1.7f;
                float w2 = R(2.0f, 2.8f), d2 = R(1.8f, 2.4f);
                Kit.Part(hg, Kit.BoxC(w2 + .2f, 1.6f, d2 + .2f), Kit.StoneDark, new Vector3(0, -.75f, 0));
                Castle.House(hg, Vector3.zero, w2, d2, R(1.3f, 1.7f), R(1.0f, 1.3f), win, R() < .6f, .5f); break;
            }
        }
    }

    // ================= flora =================
    static RM Patch(string kind, int seed)
    {
        string key = "f" + kind + seed; RM rm; if (cache.TryGetValue(key, out rm)) return rm;
        var l = new List<Kit.PartSpec>(); var sph0 = Kit.Sphere(0, true); var sph1 = Kit.Sphere(1, true);
        var stem = Kit.Cache("fst", () => Kit.Frustum(4, .008f, .006f, .26f, 1f, false)); var rd = Kit.Cache("fre", () => Kit.Frustum(4, .012f, .008f, 1f, 1f, false)); var head = Kit.Cache("frh", () => Kit.Frustum(6, .03f, .03f, .14f, 1f, false)); var ms = Kit.Cache("fms", () => Kit.Frustum(6, .03f, .025f, .1f, 1f, false));
        var sr = new System.Random(seed * 17 + kind.Length);
        for (int k = 0; k < (kind == "reed" ? 12 : kind == "mush" ? 5 : 24); k++)
        {
            float a = (float)sr.NextDouble() * 6.283f, d = Mathf.Sqrt((float)sr.NextDouble()) * (kind == "mush" ? .5f : 1.3f); Vector3 p = new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d); float rr = (float)sr.NextDouble();
            if (kind == "flower") { l.Add(new Kit.PartSpec { m = stem, x = Matrix4x4.TRS(p, Quaternion.identity, Vector3.one), g = 0 }); l.Add(new Kit.PartSpec { m = sph0, x = Matrix4x4.TRS(p + Vector3.up * .27f, Quaternion.identity, Vector3.one * (.04f + rr * .015f)), g = 1 + k % 3 }); }
            else if (kind == "mush") { float s = .7f + rr; l.Add(new Kit.PartSpec { m = ms, x = Matrix4x4.TRS(p, Quaternion.identity, Vector3.one * s), g = 0 }); l.Add(new Kit.PartSpec { m = sph1, x = Matrix4x4.TRS(p + Vector3.up * .1f * s, Quaternion.identity, new Vector3(.07f, .04f, .07f) * s), g = 1 }); }
            else { float h = .7f + rr * .6f; l.Add(new Kit.PartSpec { m = rd, x = Matrix4x4.TRS(p, Quaternion.Euler((rr - .5f) * 10f, 0, (rr - .5f) * 10f), new Vector3(1, h * 1.1f, 1)), g = 0 }); l.Add(new Kit.PartSpec { m = head, x = Matrix4x4.TRS(p + Vector3.up * h * .98f, Quaternion.identity, Vector3.one), g = 1 }); }
        }
        rm = new RM(); rm.m = Kit.Merge(l, 4, out rm.map); return cache[key] = rm;
    }
    static void PutPatch(Transform p, RM rm, Color[] pal, Vector3 pos, float yaw, float sc)
    {
        var g = new GameObject("patch"); g.transform.SetParent(p, false); g.transform.position = pos; g.transform.rotation = Quaternion.Euler(0, yaw, 0); g.transform.localScale = Vector3.one * sc;
        g.AddComponent<MeshFilter>().sharedMesh = rm.m; var r = g.AddComponent<MeshRenderer>(); var mats = new Material[rm.map.Length]; for (int k = 0; k < mats.Length; k++) mats[k] = U.Mat(pal[rm.map[k]], .1f);
        r.sharedMaterials = mats; r.shadowCastingMode = ShadowCastingMode.Off;
    }

    static bool OutsideMap(float x, float z) { return !(x > -1.5f && x < N + 1.5f && z > -1.5f && z < N + 1.5f); }
    static bool Scatter(float rMin, float rMax, out float x, out float z)
    {
        for (int t = 0; t < 12; t++)
        {
            float a = R() * 6.283f, d = R(rMin, rMax); x = 15 + Mathf.Cos(a) * d; z = 15 + Mathf.Sin(a) * d;
            if (OutsideMap(x, z) && !InLake(x, z, 30f, 15f)) return true;
        }
        x = z = 0; return false;
    }

    static void BuildFlora(Transform root)
    {
        Color[] fp = { new Color(.3f, .55f, .25f), new Color(.35f, .55f, 1f), new Color(.9f, .2f, .22f), new Color(1f, .92f, .45f) };
        Color[] mp = { new Color(.92f, .9f, .82f), new Color(.85f, .2f, .18f), Color.white, Color.white };
        Color[] rp = { new Color(.3f, .5f, .2f), new Color(.38f, .24f, .14f), Color.white, Color.white };
        float x, z;
        for (int i = 0; i < 70; i++) if (Scatter(17f, 52f, out x, out z)) PutPatch(root, Patch("flower", i % 5), fp, G(x, z), R() * 360f, R(.8f, 1.4f));
        for (int i = 0; i < 28; i++) if (Scatter(19f, 46f, out x, out z)) PutPatch(root, Patch("mush", i % 4), mp, G(x, z), R() * 360f, R(.9f, 1.5f));
        float cx = N / 2f + 8f, cz = -14f;
        for (int i = 0; i < 26; i++) { float a = R() * 6.283f, k = 1.0f + R() * .1f; PutPatch(root, Patch("reed", i % 4), rp, new Vector3(cx + Mathf.Cos(a) * 26f * k, -.01f, cz + Mathf.Sin(a) * 11f * k), R() * 360f, R(1f, 1.6f)); }
        var bark = U.Mat(new Color(.36f, .25f, .16f), .05f); var cut = U.Mat(new Color(.72f, .58f, .38f), .05f);
        for (int i = 0; i < 30; i++)
        {
            if (!Scatter(20f, 50f, out x, out z)) continue; Vector3 p = G(x, z); float yaw = R() * 360f;
            if (R() < .55f) { Kit.Part(root, Kit.FrC(8, .26f, .23f, .32f), bark, p, new Vector3(0, yaw, 0), false); Kit.Part(root, Kit.FrC(8, .21f, .21f, .02f), cut, p + Vector3.up * .32f, new Vector3(0, yaw, 0), false); }
            else { Kit.Part(root, Kit.FrC(8, .18f, .18f, 1.7f), bark, p + Vector3.up * .18f, new Vector3(0, yaw, 90), false); Kit.Part(root, Kit.FrC(8, .15f, .15f, .02f), cut, p + Vector3.up * .18f + Quaternion.Euler(0, yaw, 0) * new Vector3(-1.7f, 0, 0), new Vector3(0, yaw, 90), false); }
        }
        for (int i = 0; i < 70; i++) if (Scatter(19f, 50f, out x, out z)) { int sp = R() < .6f ? 8 : TreeKit.Pick(x, z, R()); TreeKit.Spawn(root, G(x, z), R() * 360f, sp, TreeKit.Tint(sp, R()), R(.45f, .95f), 5, false, true); }
    }

    // ================= dermaga =================
    static void BuildDock(Transform root)
    {
        var g = new GameObject("dock").transform; g.SetParent(root, false); g.position = new Vector3(N / 2f + 12f, 0, -2.6f);
        GameObject P(Mesh m, Material mt, Vector3 p, Vector3 e) { return Kit.Part(g, m, mt, p, e, true); }
        for (int i = 0; i < 20; i++) P(Kit.BoxC(1.6f, .08f, .42f), woodM, new Vector3(0, .12f, -i * .46f), new Vector3(0, R(-2f, 2f), 0));
        foreach (float sx in new[] { -.8f, .8f }) { P(Kit.BoxC(.12f, .1f, 9.3f), woodD, new Vector3(sx, .06f, -4.6f), Vector3.zero); for (int i = 0; i < 5; i++) P(Kit.FrC(8, .09f, .08f, 1.1f), woodD, new Vector3(sx * 1.06f, -.45f, -i * 2.1f - .2f), Vector3.zero); }
        P(Kit.BoxC(1.9f, .28f, .8f), woodD, new Vector3(2.0f, -.04f, -6.2f), new Vector3(0, 8, 0)); P(Kit.BoxC(1.7f, .04f, .55f), darkM, new Vector3(2.0f, .1f, -6.2f), new Vector3(0, 8, 0));
        P(Kit.GableC(.8f, .8f, .26f), woodD, new Vector3(3.0f, -.04f, -6.1f), new Vector3(0, 98, 0));
        P(Kit.BoxC(.5f, .06f, .55f), woodM, new Vector3(2.0f, .14f, -6.2f), new Vector3(0, 8, 0)); P(Kit.FrC(6, .02f, .02f, 1.5f), woodD, new Vector3(1.6f, .25f, -5.7f), new Vector3(0, 0, 70));
    }

    // ================= titik masuk =================
    static Transform New(Transform p, string n) { var g = new GameObject(n).transform; g.SetParent(p, false); return g; }
    static void Fin(Transform t) { if (!Bake) StaticBatchingUtility.Combine(t.gameObject); }

    public static void Build(Transform parent)
    {
        Mats(); if (hills.Count == 0) Plan();
        var hr = New(parent, "Hills"); var tr = New(parent, "HillTrees"); BuildHills(hr, tr); Fin(hr); Fin(tr);
        var rr = New(parent, "Rocks"); BuildRocks(rr); Fin(rr);
        var hh = New(parent, "AbandonedHouses"); AbHouse(hh, -14, 6, 35, 1f); AbHouse(hh, 44, 24, -50, 1.15f); AbHouse(hh, 30, 52, 160, .85f); Fin(hh);
        var ru = New(parent, "Ruins"); Ruin(ru, -12, 36, 20, 1); Ruin(ru, 48, 40, -30, 2); Ruin(ru, 8, 53, 90, 3); Fin(ru);
        var kg = New(parent, "Kingdom"); BuildKingdom(kg); Fin(kg);
        var fl = New(parent, "Flora"); BuildFlora(fl); Fin(fl);
        var dk = New(parent, "Dock"); BuildDock(dk); Fin(dk);
    }
}
