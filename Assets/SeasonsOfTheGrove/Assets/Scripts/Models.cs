using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Generator mesh prosedural: kotak, frustum/kerucut, torus, bola low-poly, atap pelana + tekstur batu/genteng.</summary>
public static class Kit
{
    const float TS = 1f / 1.3f;   // tekstur berulang tiap 1,3 unit dunia
    static Texture2D stoneT, roofT;
    static Material stoneM, stoneD, rubbleM, roofM;
    static readonly Dictionary<string, Mesh> mc = new Dictionary<string, Mesh>();

    public static void Reset() { stoneT = null; roofT = null; stoneM = null; stoneD = null; rubbleM = null; roofM = null; mc.Clear(); }
    public static Mesh Cache(string k, System.Func<Mesh> f) { Mesh m; if (!mc.TryGetValue(k, out m) || m == null) { m = f(); mc[k] = m; } return m; }
    public static Material Stone { get { if (!stoneM) stoneM = U.TexMat(StoneTex(), Color.white, .04f); return stoneM; } }
    public static Material StoneDark { get { if (!stoneD) stoneD = U.TexMat(StoneTex(), new Color(.7f, .66f, .62f), .04f); return stoneD; } }
    public static Material Rubble { get { if (!rubbleM) rubbleM = U.TexMat(StoneTex(), new Color(.5f, .46f, .42f), .02f); return rubbleM; } }
    public static Material Roof { get { if (!roofM) roofM = U.TexMat(RoofTex(), Color.white, .1f); return roofM; } }

    static float Hh(int a) { unchecked { a = (a ^ 61) ^ (a >> 16); a *= 9; a ^= a >> 4; a *= 0x27d4eb2d; a ^= a >> 15; return (a & 0xffff) / 65535f; } }

    static Texture2D StoneTex()
    {
        if (stoneT) return stoneT;
        int n = 128; var t = new Texture2D(n, n, TextureFormat.RGBA32, true); t.wrapMode = TextureWrapMode.Repeat; var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int row = y / 16, bx = (x + (row % 2) * 16) % n, col = bx / 32; bool mortar = (y % 16) < 2 || (bx % 32) < 2;
                float v = Hh(row * 7 + col) * .26f - .13f; float nz = (Hh(x * 131 + y * 17) - .5f) * .1f;
                Color c = mortar ? new Color(.66f, .62f, .56f) : new Color(.56f + v, .44f + v * .8f, .34f + v * .6f);
                px[y * n + x] = c * (1f + nz);
            }
        t.SetPixels(px); t.Apply(true); return stoneT = t;
    }
    static Texture2D RoofTex()
    {
        if (roofT) return roofT;
        int n = 128; var t = new Texture2D(n, n, TextureFormat.RGBA32, true); t.wrapMode = TextureWrapMode.Repeat; var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                int row = y / 16, bx = (x + (row % 2) * 12) % n, col = bx / 24; float fy = (y % 16) / 16f, fx = (bx % 24) / 24f;
                float v = Hh(row * 11 + col) * .2f - .1f; float edge = fy < .12f ? .75f : (Mathf.Abs(fx - .5f) > .46f ? .85f : 1f);
                px[y * n + x] = new Color(.40f + v, .47f + v, .52f + v) * edge;
            }
        t.SetPixels(px); t.Apply(true); return roofT = t;
    }

    // ---------- builder mesh ----------
    public class MB
    {
        public List<Vector3> v = new List<Vector3>(); public List<Vector2> uv = new List<Vector2>(); public List<int> t = new List<int>();
        public void Quad(Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl, float s)
        {
            float u = (br - bl).magnitude * s, w = (tl - bl).magnitude * s; int i = v.Count;
            v.Add(bl); v.Add(br); v.Add(tr); v.Add(tl);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(u, 0)); uv.Add(new Vector2(u, w)); uv.Add(new Vector2(0, w));
            t.Add(i); t.Add(i + 3); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 1);
        }
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0) { var tmp = b; b = c; c = tmp; }
            int i = v.Count; v.Add(a); v.Add(b); v.Add(c);
            uv.Add(new Vector2(a.x + a.z, a.y) * TS); uv.Add(new Vector2(b.x + b.z, b.y) * TS); uv.Add(new Vector2(c.x + c.z, c.y) * TS);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
        }
        public Mesh Build() { var m = new Mesh(); m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds(); return m; }
    }

    public static Mesh Box(float w, float h, float d, float s = TS)
    {
        var b = new MB(); float x = w / 2, y = h / 2, z = d / 2;
        b.Quad(new Vector3(-x, -y, -z), new Vector3(x, -y, -z), new Vector3(x, y, -z), new Vector3(-x, y, -z), s);
        b.Quad(new Vector3(x, -y, z), new Vector3(-x, -y, z), new Vector3(-x, y, z), new Vector3(x, y, z), s);
        b.Quad(new Vector3(x, -y, -z), new Vector3(x, -y, z), new Vector3(x, y, z), new Vector3(x, y, -z), s);
        b.Quad(new Vector3(-x, -y, z), new Vector3(-x, -y, -z), new Vector3(-x, y, -z), new Vector3(-x, y, z), s);
        b.Quad(new Vector3(-x, y, -z), new Vector3(x, y, -z), new Vector3(x, y, z), new Vector3(-x, y, z), s);
        return b.Build();
    }

    /// <summary>Silinder meruncing/kerucut: alas di y=0, puncak di y=h. smooth=false memberi tampilan low-poly bersegi.</summary>
    public static Mesh Frustum(int n, float r0, float r1, float h, float s = TS, bool smooth = true)
    {
        var b = new MB();
        for (int i = 0; i < n; i++)
        {
            float a0 = i * 2 * Mathf.PI / n, a1 = (i + 1) * 2 * Mathf.PI / n;
            var p00 = new Vector3(Mathf.Cos(a0) * r0, 0, Mathf.Sin(a0) * r0); var p01 = new Vector3(Mathf.Cos(a1) * r0, 0, Mathf.Sin(a1) * r0);
            var p10 = new Vector3(Mathf.Cos(a0) * r1, h, Mathf.Sin(a0) * r1); var p11 = new Vector3(Mathf.Cos(a1) * r1, h, Mathf.Sin(a1) * r1);
            b.Quad(p00, p01, p11, p10, s);
        }
        if (r1 > .001f) for (int i = 0; i < n; i++)
            {
                float a0 = i * 2 * Mathf.PI / n, a1 = (i + 1) * 2 * Mathf.PI / n;
                b.Tri(new Vector3(0, h, 0), new Vector3(Mathf.Cos(a0) * r1, h, Mathf.Sin(a0) * r1), new Vector3(Mathf.Cos(a1) * r1, h, Mathf.Sin(a1) * r1), Vector3.up);
            }
        var m = b.Build();
        if (smooth)
        {
            var nm = m.normals;
            for (int i = 0; i < n; i++)
            {
                float a0 = i * 2 * Mathf.PI / n, a1 = (i + 1) * 2 * Mathf.PI / n;
                var n0 = new Vector3(Mathf.Cos(a0) * h, r0 - r1, Mathf.Sin(a0) * h).normalized; var n1 = new Vector3(Mathf.Cos(a1) * h, r0 - r1, Mathf.Sin(a1) * h).normalized;
                nm[i * 4] = n0; nm[i * 4 + 3] = n0; nm[i * 4 + 1] = n1; nm[i * 4 + 2] = n1;
            }
            m.normals = nm;
        }
        return m;
    }

    /// <summary>Atap pelana: bubungan sejajar sumbu z, lereng di sisi +/-x. Alas di y=0.</summary>
    public static Mesh Gable(float w, float d, float h)
    {
        var b = new MB(); float x = w / 2, z = d / 2, s = 1f / 1.0f;
        b.Quad(new Vector3(x, 0, -z), new Vector3(x, 0, z), new Vector3(0, h, z), new Vector3(0, h, -z), s);
        b.Quad(new Vector3(-x, 0, z), new Vector3(-x, 0, -z), new Vector3(0, h, -z), new Vector3(0, h, z), s);
        return b.Build();
    }
    public static Mesh GableEnds(float w, float d, float h)
    {
        var b = new MB(); float x = w / 2, z = d / 2;
        b.Tri(new Vector3(-x, 0, -z), new Vector3(x, 0, -z), new Vector3(0, h, -z), new Vector3(0, 0, -1));
        b.Tri(new Vector3(x, 0, z), new Vector3(-x, 0, z), new Vector3(0, h, z), new Vector3(0, 0, 1));
        return b.Build();
    }

    public static Mesh Torus(float R, float r, int seg = 14, int minor = 5)
    {
        var b = new MB();
        for (int i = 0; i < seg; i++)
            for (int j = 0; j < minor; j++)
            {
                float t0 = i * 6.2832f / seg, t1 = (i + 1) * 6.2832f / seg, f0 = j * 6.2832f / minor, f1 = (j + 1) * 6.2832f / minor;
                Vector3 P(float t, float f) { return new Vector3(Mathf.Cos(t) * (R + r * Mathf.Cos(f)), r * Mathf.Sin(f), Mathf.Sin(t) * (R + r * Mathf.Cos(f))); }
                Vector3 C(float t) { return new Vector3(Mathf.Cos(t) * R, 0, Mathf.Sin(t) * R); }
                var a = P(t0, f0); var b2 = P(t1, f0); var c = P(t1, f1); var d = P(t0, f1); var o = (a + c) / 2 - C((t0 + t1) / 2);
                b.Tri(a, b2, c, o); b.Tri(a, c, d, o);
            }
        return b.Build();
    }

    /// <summary>Bola low-poly (icosahedron) berpermukaan bersegi. Radius 1 pada skala 1.</summary>
    public static Mesh LowSphere(int detail)
    {
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        var vs = new List<Vector3> { new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0), new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t), new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1) };
        for (int i = 0; i < vs.Count; i++) vs[i] = vs[i].normalized;
        int[] f = { 0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8, 3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1 };
        var tris = new List<Vector3[]>();
        for (int i = 0; i < f.Length; i += 3) tris.Add(new[] { vs[f[i]], vs[f[i + 1]], vs[f[i + 2]] });
        for (int d = 0; d < detail; d++)
        {
            var nt = new List<Vector3[]>();
            foreach (var tr in tris)
            {
                var ab = ((tr[0] + tr[1]) / 2).normalized; var bc = ((tr[1] + tr[2]) / 2).normalized; var ca = ((tr[2] + tr[0]) / 2).normalized;
                nt.Add(new[] { tr[0], ab, ca }); nt.Add(new[] { tr[1], bc, ab }); nt.Add(new[] { tr[2], ca, bc }); nt.Add(new[] { ab, bc, ca });
            }
            tris = nt;
        }
        var b = new MB();
        foreach (var tr in tris) b.Tri(tr[0], tr[1], tr[2], tr[0] + tr[1] + tr[2]);
        return b.Build();
    }

    public struct PartSpec { public Mesh m; public Matrix4x4 x; public int g; }

    /// <summary>Bola unit (radius 1). smooth=true memberi normal halus (bentuk bulat seperti gambar referensi).</summary>
    public static Mesh Sphere(int detail, bool smooth)
    {
        return Cache("sph" + detail + smooth, () =>
        {
            var m = LowSphere(detail);
            if (smooth) { var v = m.vertices; var n = new Vector3[v.Length]; for (int i = 0; i < v.Length; i++) n[i] = v[i].normalized; m.normals = n; }
            return m;
        });
    }

    /// <summary>Gabungkan banyak bagian menjadi SATU mesh dengan satu submesh per grup material (1 renderer, bukan puluhan).</summary>
    public static Mesh Merge(List<PartSpec> parts, int groups, out int[] map)
    {
        var gm = new List<Mesh>(); var mp = new List<int>();
        for (int g = 0; g < groups; g++)
        {
            var ci = new List<CombineInstance>();
            foreach (var p in parts) if (p.g == g) { var c = new CombineInstance(); c.mesh = p.m; c.transform = p.x; ci.Add(c); }
            if (ci.Count == 0) continue;
            var m = new Mesh(); m.indexFormat = IndexFormat.UInt32; m.CombineMeshes(ci.ToArray(), true, true); gm.Add(m); mp.Add(g);
        }
        var fin = new Mesh(); fin.indexFormat = IndexFormat.UInt32; var cis = new CombineInstance[gm.Count];
        for (int i = 0; i < gm.Count; i++) { cis[i].mesh = gm[i]; cis[i].transform = Matrix4x4.identity; }
        fin.CombineMeshes(cis, false, false); fin.RecalculateBounds(); map = mp.ToArray(); return fin;
    }

    public static GameObject Part(Transform p, Mesh m, Material mat, Vector3 pos, Vector3 euler = default(Vector3), bool shadow = true)
    {
        var g = new GameObject("part"); g.transform.SetParent(p, false); g.transform.localPosition = pos; g.transform.localEulerAngles = euler;
        g.AddComponent<MeshFilter>().sharedMesh = m; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
        r.shadowCastingMode = shadow ? ShadowCastingMode.On : ShadowCastingMode.Off; return g;
    }
    public static GameObject Sc(GameObject g, float x, float y, float z) { g.transform.localScale = new Vector3(x, y, z); return g; }
    public static Mesh BoxC(float w, float h, float d, float s = TS) { return Cache("b" + w + "," + h + "," + d, () => Box(w, h, d, s)); }
    public static Mesh GableC(float w, float d, float h) { return Cache("g" + w + "," + d + "," + h, () => Gable(w, d, h)); }
    public static Mesh GableEndsC(float w, float d, float h) { return Cache("ge" + w + "," + d + "," + h, () => GableEnds(w, d, h)); }
    public static Mesh FrC(int n, float r0, float r1, float h, bool smooth = true) { return Cache("f" + n + "," + r0 + "," + r1 + "," + h + smooth, () => Frustum(n, r0, r1, h, TS, smooth)); }
}

/// <summary>Karakter chibi low-poly (mengacu gambar 1): kepala besar, topi dengan telinga, kerah cincin, lengan melayang, celana kembung.</summary>
[System.Serializable]
public class Chibi
{
    public struct Style { public Color skin, hair, top, sleeve, bloom, legs, boot, collar, hatC, hatC2; public int hat; }
    public Transform root, armL, armR, legL, legR, handR, bloomT, torso, mouth, beard, flower, glasses; public Transform[] hats = new Transform[4];
    public Renderer[] topR, sleeveR, bloomR, legsR, cuffR; Transform[] legMesh = new Transform[2], bootT = new Transform[2];
    Mesh ls1, ls0;

    public static Chibi Build(Transform parent, Style s, bool allHats)
    {
        var c = new Chibi(); var g = new GameObject("chibi").transform; g.SetParent(parent, false); c.root = g;
        c.ls1 = Kit.Cache("ls1", () => Kit.LowSphere(1)); c.ls0 = Kit.Cache("ls0", () => Kit.LowSphere(0));
        Material M(Color col) { return U.Mat(col, .08f); }
        c.bloomT = Kit.Part(g, Kit.FrC(10, .27f, .17f, .34f, false), M(s.bloom), new Vector3(0, .33f, 0)).transform;
        var cuff = Kit.Part(g, Kit.FrC(10, .19f, .19f, .06f, false), M(s.bloom * .85f), new Vector3(0, .66f, 0)); c.cuffR = new[] { cuff.GetComponent<Renderer>() };
        var tor = Kit.Part(g, Kit.FrC(8, .12f, .095f, .45f, false), M(s.top), new Vector3(0, .62f, 0)); c.torso = tor.transform;
        Kit.Part(g, Kit.Cache("torus", () => Kit.Torus(.14f, .032f, 14, 5)), M(s.collar), new Vector3(0, 1.1f, 0));
        Kit.Sc(Kit.Part(g, c.ls1, M(s.skin), new Vector3(0, 1.4f, 0)), .3f, .3f, .3f);
        foreach (float x in new[] { -.1f, .1f }) Kit.Sc(Kit.Part(g, c.ls0, M(Color.black), new Vector3(x, 1.38f, .28f), default(Vector3), false), .035f, .045f, .03f);
        c.mouth = Kit.Sc(Kit.Part(g, Kit.BoxC(1, 1, 1), M(new Color(.5f, .15f, .15f)), new Vector3(0, 1.27f, .29f), default(Vector3), false), .09f, .02f, .02f).transform;
        for (int k = 0; k < 7; k++)
        {
            float a = -1.1f + k * .37f;
            Kit.Part(g, Kit.FrC(4, .05f, 0f, .17f, false), M(s.hair), new Vector3(Mathf.Sin(a) * .26f, 1.53f, Mathf.Cos(a) * .26f), new Vector3(180, a * Mathf.Rad2Deg, 0), false);
        }
        c.beard = Kit.Sc(Kit.Part(g, c.ls1, M(s.hair), new Vector3(0, 1.25f, .2f)), .17f, .12f, .1f).transform;
        c.flower = Kit.Sc(Kit.Part(g, c.ls0, M(new Color(1f, .5f, .66f)), new Vector3(.24f, 1.52f, .12f), default(Vector3), false), .06f, .06f, .06f).transform;
        c.glasses = Kit.Sc(Kit.Part(g, Kit.BoxC(1, 1, 1), M(Color.black), new Vector3(0, 1.38f, .3f), default(Vector3), false), .27f, .05f, .03f).transform;
        c.beard.gameObject.SetActive(false); c.flower.gameObject.SetActive(false); c.glasses.gameObject.SetActive(false);
        var slot = new GameObject("hat").transform; slot.SetParent(g, false); slot.localPosition = new Vector3(0, 1.5f, 0);
        if (allHats) for (int i = 0; i < 4; i++) { var hs = new GameObject("hat" + i).transform; hs.SetParent(slot, false); c.hats[i] = hs; MakeHat(hs, i, i == 1 ? new Color(.85f, .76f, .48f) : s.hatC, s.hatC2, c); }
        else { MakeHat(slot, s.hat, s.hatC, s.hatC2, c); }
        // lengan: tali bahu + manset besar + tangan
        c.topR = new[] { tor.GetComponent<Renderer>() };
        var sl = new List<Renderer>(); var lg = new List<Renderer>(); var bl = new List<Renderer> { c.bloomT.GetComponent<Renderer>() };
        for (int i = 0; i < 2; i++)
        {
            float sx = i == 0 ? -1f : 1f; var piv = new GameObject(i == 0 ? "armL" : "armR").transform; piv.SetParent(g, false); piv.localPosition = new Vector3(sx * .17f, 1.0f, 0);
            Kit.Part(piv, Kit.FrC(5, .022f, .022f, .2f, false), M(s.collar), new Vector3(0, -.2f, 0), new Vector3(0, 0, sx * 12f), false);
            var cf = Kit.Part(piv, Kit.FrC(8, .12f, .095f, .3f, false), M(s.sleeve), new Vector3(sx * .08f, -.5f, 0)); sl.Add(cf.GetComponent<Renderer>());
            Kit.Sc(Kit.Part(piv, c.ls0, M(s.skin), new Vector3(sx * .08f, -.58f, 0)), .06f, .07f, .06f);
            var hand = new GameObject("hand").transform; hand.SetParent(piv, false); hand.localPosition = new Vector3(sx * .08f, -.6f, .05f);
            if (i == 0) c.armL = piv; else { c.armR = piv; c.handR = hand; }
        }
        for (int i = 0; i < 2; i++)
        {
            float sx = i == 0 ? -1f : 1f; var piv = new GameObject(i == 0 ? "legL" : "legR").transform; piv.SetParent(g, false); piv.localPosition = new Vector3(sx * .1f, .34f, 0);
            var lm = Kit.Part(piv, Kit.FrC(6, .075f, .07f, .3f, false), M(s.legs), new Vector3(0, -.3f, 0)); lg.Add(lm.GetComponent<Renderer>());
            Kit.Sc(Kit.Part(piv, c.ls1, M(s.boot), new Vector3(0, -.3f, .03f)), .095f, .055f, .13f);
            if (i == 0) c.legL = piv; else c.legR = piv;
        }
        c.sleeveR = sl.ToArray(); c.legsR = lg.ToArray(); c.bloomR = bl.ToArray();
        return c;
    }

    static void MakeHat(Transform h, int kind, Color c, Color c2, Chibi ch)
    {
        Material M(Color col) { return U.Mat(col, .08f); }
        var s1 = ch.ls1;
        switch (kind)
        {
            case 0: // beanie + telinga kecil
                Kit.Sc(Kit.Part(h, s1, M(c), Vector3.zero), .34f, .27f, .34f); Kit.Part(h, Kit.Cache("torus2", () => Kit.Torus(.3f, .04f, 14, 5)), M(c2), new Vector3(0, -.1f, 0));
                foreach (float x in new[] { -1f, 1f }) Kit.Part(h, Kit.FrC(5, .06f, 0f, .15f, false), M(c2), new Vector3(x * .33f, -.04f, 0), new Vector3(0, 0, -x * 70f));
                break;
            case 1: Kit.Part(h, Kit.FrC(12, .44f, 0f, .24f, false), M(c), new Vector3(0, -.06f, 0)); break;
            case 2:
                Kit.Part(h, Kit.FrC(12, .28f, .25f, .12f, false), M(Color.white), new Vector3(0, -.03f, 0)); Kit.Part(h, Kit.FrC(12, .285f, .285f, .04f, false), M(c2), new Vector3(0, -.03f, 0));
                Kit.Part(h, Kit.FrC(12, .3f, .3f, .015f, false), M(Color.white), new Vector3(0, -.04f, 0)); break;
            case 3:
                Kit.Sc(Kit.Part(h, s1, M(new Color(.55f, .57f, .6f)), Vector3.zero), .33f, .26f, .33f); Kit.Part(h, Kit.Cache("torus2", () => Kit.Torus(.3f, .04f, 14, 5)), M(new Color(.35f, .35f, .38f)), new Vector3(0, -.09f, 0));
                foreach (float x in new[] { -1f, 1f }) Kit.Part(h, Kit.FrC(6, .05f, 0f, .22f, false), M(new Color(.94f, .92f, .82f)), new Vector3(x * .3f, 0f, 0), new Vector3(0, 0, -x * 40f));
                break;
            case 4:
                Kit.Sc(Kit.Part(h, s1, M(c), new Vector3(0, -.02f, 0)), .36f, .32f, .36f); Kit.Part(h, Kit.FrC(6, .1f, 0f, .26f, false), M(c), new Vector3(0, -.08f, -.3f), new Vector3(-70, 0, 0));
                break;
            case 5:
                Kit.Sc(Kit.Part(h, s1, M(new Color(.6f, .62f, .66f)), Vector3.zero), .33f, .27f, .33f); Kit.Sc(Kit.Part(h, Kit.BoxC(1, 1, 1), M(c2), new Vector3(0, .28f, 0)), .05f, .2f, .2f);
                break;
            case 6:
                Kit.Part(h, Kit.FrC(14, .4f, .4f, .03f, false), M(c), new Vector3(0, -.04f, 0)); Kit.Part(h, Kit.FrC(12, .2f, .16f, .18f, false), M(c), new Vector3(0, -.02f, 0));
                Kit.Sc(Kit.Part(h, Kit.BoxC(1, 1, 1), M(c2), new Vector3(.12f, .16f, 0), new Vector3(0, 0, -25)), .03f, .22f, .08f);
                break;
            case 7:
                Kit.Part(h, Kit.FrC(12, .31f, .27f, .1f, false), M(c), new Vector3(0, -.06f, 0)); Kit.Sc(Kit.Part(h, Kit.BoxC(1, 1, 1), M(c), new Vector3(0, -.08f, .3f)), .3f, .03f, .16f);
                break;
            default: Kit.Part(h, Kit.FrC(10, .3f, 0f, .6f, false), M(c), new Vector3(0, -.05f, 0)); break;
        }
    }

    public void SetCloth(Color top, Color sleeve, Color bloom, Color legs)
    {
        foreach (var r in topR) r.sharedMaterial = U.Mat(top, .08f);
        foreach (var r in sleeveR) r.sharedMaterial = U.Mat(sleeve, .08f);
        foreach (var r in bloomR) r.sharedMaterial = U.Mat(bloom, .08f);
        foreach (var r in cuffR) r.sharedMaterial = U.Mat(bloom * .85f, .08f);
        foreach (var r in legsR) r.sharedMaterial = U.Mat(legs, .08f);
    }
    public void SetHat(int i) { for (int k = 0; k < 4; k++) if (hats[k]) hats[k].gameObject.SetActive(k == i); }
    public void Animate(float phase, float amt)
    {
        float a = Mathf.Sin(phase) * 28f * amt;
        armL.localRotation = Quaternion.Euler(a, 0, 0); armR.localRotation = Quaternion.Euler(-a, 0, 0);
        legL.localRotation = Quaternion.Euler(-a, 0, 0); legR.localRotation = Quaternion.Euler(a, 0, 0);
    }
}

/// <summary>Domba bergaya gambar 2: wol krem berlekuk, kepala panjang berwajah kemerahan, telinga jingga, kaki ramping. Dibuat dari mesh gabungan (6 renderer per domba).</summary>
public static class SheepKit
{
    static Mesh body, head, leg; static int[] bm, hm, lm; static bool built;
    public static void Reset() { built = false; body = null; head = null; leg = null; }
    static void Add(List<Kit.PartSpec> l, Mesh m, Vector3 p, Vector3 s, int g, Vector3 e) { l.Add(new Kit.PartSpec { m = m, x = Matrix4x4.TRS(p, Quaternion.Euler(e), s), g = g }); }
    static void Ensure()
    {
        if (built) return; built = true;
        var sp = Kit.Sphere(1, true); var cone = Kit.Cache("shc", () => Kit.Frustum(6, .045f, 0f, .16f, 1f, false));
        var cyl = Kit.Cache("shl", () => Kit.Frustum(6, .035f, .03f, .4f, 1f, false)); var hoof = Kit.Cache("shh", () => Kit.Frustum(6, .04f, .04f, .04f, 1f, false));
        var l = new List<Kit.PartSpec>();
        Add(l, sp, new Vector3(0, .62f, 0), new Vector3(.33f, .29f, .5f), 0, Vector3.zero);
        for (int j = 0; j < 14; j++)
        {
            float a = j * 2.4f; var p = new Vector3(Mathf.Cos(a) * .27f + Mathf.Sin(a) * .08f, .62f + Mathf.Sin(a) * .22f, Mathf.Sin(a * 1.7f) * .42f + (j % 5 - 2) * .045f);
            Add(l, sp, p, new Vector3(.17f + (j % 3) * .02f, .15f, .19f), j % 2 == 0 ? 0 : 1, Vector3.zero);
        }
        Add(l, sp, new Vector3(0, .62f, .46f), new Vector3(.23f, .23f, .21f), 0, Vector3.zero); Add(l, sp, new Vector3(0, .6f, -.48f), new Vector3(.25f, .23f, .21f), 1, Vector3.zero);
        Add(l, sp, new Vector3(0, .78f, .52f), new Vector3(.13f, .17f, .15f), 0, Vector3.zero); Add(l, sp, new Vector3(0, .68f, -.58f), Vector3.one * .08f, 0, Vector3.zero);
        body = Kit.Merge(l, 2, out bm);
        l = new List<Kit.PartSpec>();
        Add(l, sp, new Vector3(0, -.02f, .08f), new Vector3(.1f, .12f, .18f), 0, Vector3.zero); Add(l, sp, new Vector3(0, -.08f, .26f), new Vector3(.065f, .06f, .08f), 0, Vector3.zero);
        Add(l, sp, new Vector3(0, .1f, 0), new Vector3(.1f, .06f, .09f), 3, Vector3.zero);
        for (int s = -1; s <= 1; s += 2)
        {
            Add(l, sp, new Vector3(s * .075f, .03f, .17f), Vector3.one * .0175f, 1, Vector3.zero); Add(l, sp, new Vector3(s * .03f, -.07f, .33f), Vector3.one * .009f, 1, Vector3.zero);
            Add(l, cone, new Vector3(s * .1f, .06f, -.02f), Vector3.one, 2, new Vector3(0, 0, -s * 78f));
        }
        head = Kit.Merge(l, 4, out hm);
        l = new List<Kit.PartSpec>(); Add(l, cyl, Vector3.zero, Vector3.one, 0, Vector3.zero); Add(l, hoof, Vector3.zero, Vector3.one, 1, Vector3.zero); leg = Kit.Merge(l, 2, out lm);
    }
    static GameObject Obj(Transform p, Mesh m, Material[] mats, Vector3 pos) { var g = new GameObject("m"); g.transform.SetParent(p, false); g.transform.localPosition = pos; g.AddComponent<MeshFilter>().sharedMesh = m; g.AddComponent<MeshRenderer>().sharedMaterials = mats; return g; }

    public static void Build(Transform g, bool dark, out Transform head_, out Transform[] legs)
    {
        Ensure();
        Color wool = new Color(.94f, .95f, .8f), wool2 = new Color(.88f, .9f, .72f), face = dark ? new Color(.2f, .17f, .15f) : new Color(.96f, .84f, .72f), orange = new Color(.95f, .62f, .26f), hoofC = new Color(.25f, .2f, .17f);
        Color[] bp = { wool, wool2 }, hp = { face, Color.black, orange, wool }, lp = { face, hoofC };
        var bmat = new Material[bm.Length]; for (int k = 0; k < bm.Length; k++) bmat[k] = U.Mat(bp[bm[k]], .05f);
        var hmat = new Material[hm.Length]; for (int k = 0; k < hm.Length; k++) hmat[k] = U.Mat(hp[hm[k]], .05f);
        var lmat = new Material[lm.Length]; for (int k = 0; k < lm.Length; k++) lmat[k] = U.Mat(lp[lm[k]], .05f);
        Obj(g, body, bmat, Vector3.zero);
        var hd = new GameObject("head").transform; hd.SetParent(g, false); hd.localPosition = new Vector3(0, .85f, .66f); Obj(hd, head, hmat, Vector3.zero); head_ = hd;
        legs = new Transform[4]; int n = 0;
        foreach (float dx in new[] { -1f, 1f }) foreach (float dz in new[] { -1f, 1f })
            {
                var lg = new GameObject("leg").transform; lg.SetParent(g, false); lg.localPosition = new Vector3(dx * .17f, .42f, dz * .32f); Obj(lg, leg, lmat, new Vector3(0, -.4f, 0)); legs[n++] = lg;
            }
    }
}

/// <summary>Kit kastil bergaya gambar 3: menara bulat/persegi/oktagon/spire, dinding bergerigi, gerbang melengkung, rumah beratap genteng.</summary>
public static class Castle
{
    static Material Dark { get { return U.Mat(new Color(.08f, .07f, .06f), .02f); } }
    static Material Wood { get { return U.Mat(new Color(.4f, .27f, .16f), .08f); } }

    public static void Merlons(Transform p, Vector3 center, float radius, int n, float y, float w = .22f, float h = .26f, float d = .2f)
    {
        for (int i = 0; i < n; i++) { float a = i * 6.2832f / n; Kit.Part(p, Kit.BoxC(w, h, d), Kit.Stone, center + new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius), new Vector3(0, -a * Mathf.Rad2Deg, 0)); }
    }

    public static void RoundTower(Transform p, Vector3 pos, float r, float h, bool conical)
    {
        Kit.Part(p, Kit.FrC(14, r, r * .93f, h), Kit.Stone, pos);
        for (int k = 0; k < 4; k++) { float a = k * 1.7f + .4f; Kit.Part(p, Kit.BoxC(.09f, .3f, .08f), Dark, pos + new Vector3(Mathf.Cos(a) * r * .94f, h * (.3f + .17f * k), Mathf.Sin(a) * r * .94f), new Vector3(0, -a * Mathf.Rad2Deg, 0), false); }
        Kit.Part(p, Kit.FrC(14, r * 1.12f, r * 1.0f, .18f), Kit.Stone, pos + new Vector3(0, h, 0));
        if (conical) Kit.Part(p, Kit.FrC(14, r * 1.22f, 0f, h * .55f), Kit.Roof, pos + new Vector3(0, h + .18f, 0));
        else { Merlons(p, pos, r * 1.05f, 9, h + .31f); Kit.Part(p, Kit.FrC(14, r * 1.0f, r * 1.0f, .03f), Kit.StoneDark, pos + new Vector3(0, h + .18f, 0)); }
    }

    public static void SquareTower(Transform p, Vector3 pos, float w, float h)
    {
        Kit.Part(p, Kit.BoxC(w, h, w), Kit.Stone, pos + new Vector3(0, h / 2f, 0));
        for (int k = 0; k < 3; k++) { Kit.Part(p, Kit.BoxC(.1f, .32f, .06f), Dark, pos + new Vector3((k - 1) * .26f, h * (.35f + .15f * k), -w / 2f - .01f), default(Vector3), false); Kit.Part(p, Kit.BoxC(.06f, .32f, .1f), Dark, pos + new Vector3(w / 2f + .01f, h * (.35f + .15f * k), (k - 1) * .26f), default(Vector3), false); }
        Kit.Part(p, Kit.BoxC(w + .24f, .18f, w + .24f), Kit.Stone, pos + new Vector3(0, h + .09f, 0));
        foreach (float sx in new[] { -1f, 1f }) foreach (float sz in new[] { -1f, 1f })
            {
                var c = pos + new Vector3(sx * (w / 2f + .02f), h + .18f, sz * (w / 2f + .02f));
                Kit.Part(p, Kit.FrC(8, .14f, .12f, .5f), Kit.Stone, c); Kit.Part(p, Kit.FrC(8, .19f, 0f, .38f), Kit.Roof, c + new Vector3(0, .5f, 0));
            }
        for (int i = 0; i < 3; i++) { Kit.Part(p, Kit.BoxC(.2f, .24f, .16f), Kit.Stone, pos + new Vector3(-.28f + i * .28f, h + .3f, -w / 2f - .1f)); Kit.Part(p, Kit.BoxC(.2f, .24f, .16f), Kit.Stone, pos + new Vector3(-.28f + i * .28f, h + .3f, w / 2f + .1f)); }
    }

    public static void SpireTower(Transform p, Vector3 pos, float w, float h)
    {
        Kit.Part(p, Kit.BoxC(w, h, w), Kit.Stone, pos + new Vector3(0, h / 2f, 0));
        Kit.Part(p, Kit.BoxC(w + .08f, .12f, w + .08f), U.Mat(new Color(.93f, .92f, .88f), .05f), pos + new Vector3(0, h * .78f, 0));
        for (int k = 0; k < 3; k++) Kit.Part(p, Kit.BoxC(.08f, .3f, .06f), Dark, pos + new Vector3(0, h * (.25f + .17f * k), -w / 2f - .01f), default(Vector3), false);
        Kit.Part(p, Kit.BoxC(w + .2f, .14f, w + .2f), Kit.Stone, pos + new Vector3(0, h + .07f, 0));
        Kit.Part(p, Kit.FrC(4, w * .78f, 0f, 1.45f), Kit.Roof, pos + new Vector3(0, h + .14f, 0), new Vector3(0, 45, 0));
    }

    public static void Octagon(Transform p, Vector3 pos, float r, float h)
    {
        Kit.Part(p, Kit.FrC(8, r, r * .96f, h, false), Kit.Stone, pos);
        for (int i = 0; i < 8; i++) { float a = i * .7854f; Kit.Part(p, Kit.FrC(8, .05f, .05f, .25f), U.Mat(new Color(.93f, .92f, .88f), .05f), pos + new Vector3(Mathf.Cos(a) * r * .98f, h - .06f, Mathf.Sin(a) * r * .98f)); }
        Kit.Part(p, Kit.FrC(8, r * 1.32f, 0f, h * .85f, false), Kit.Roof, pos + new Vector3(0, h + .15f, 0));
        Kit.Part(p, Kit.FrC(8, r * 1.0f, r * 1.0f, .15f, false), Kit.StoneDark, pos + new Vector3(0, h, 0));
    }

    /// <summary>Rumah batu beratap genteng pelana. Pintu menghadap +z.</summary>
    public static void House(Transform p, Vector3 pos, float w, float d, float wallH, float roofH, List<Material> windows, bool chimney, float chx = 0f)
    {
        Kit.Part(p, Kit.BoxC(w, wallH, d), Kit.Stone, pos + new Vector3(0, wallH / 2f, 0));
        Kit.Part(p, Kit.GableEndsC(w, d, roofH), Kit.Stone, pos + new Vector3(0, wallH, 0));
        Kit.Part(p, Kit.GableC(w + .24f, d + .24f, roofH + .06f), Kit.Roof, pos + new Vector3(0, wallH - .02f, 0));
        Kit.Part(p, Kit.BoxC(.32f, .5f, .05f), Wood, pos + new Vector3(0, .25f, d / 2f + .01f), default(Vector3), false);
        Kit.Part(p, Kit.FrC(10, .16f, .16f, .04f), Wood, pos + new Vector3(0, .5f, d / 2f + .02f), new Vector3(90, 0, 0), false);
        foreach (float sx in new[] { -1f, 1f })
        {
            var wm = U.Glow(new Color(1f, .91f, .66f)); windows.Add(wm);
            var win = Kit.Part(p, Kit.BoxC(.2f, .24f, .04f), wm, pos + new Vector3(sx * w * .3f, wallH * .62f, d / 2f + .015f), default(Vector3), false);
            Kit.Part(p, Kit.BoxC(.26f, .3f, .03f), U.Mat(new Color(.93f, .92f, .88f), .05f), pos + new Vector3(sx * w * .3f, wallH * .62f, d / 2f + .005f), default(Vector3), false);
        }
        if (chimney) Kit.Part(p, Kit.BoxC(.26f, .8f, .26f), Kit.Stone, pos + new Vector3(chx, wallH + roofH * .5f, -d * .2f));
    }

    public static void Well(Transform p, Vector3 pos)
    {
        Kit.Part(p, Kit.FrC(10, .42f, .42f, .45f), Kit.Stone, pos);
        Kit.Part(p, Kit.FrC(10, .32f, .32f, .02f), U.Mat(new Color(.12f, .37f, .48f), .9f), pos + new Vector3(0, .43f, 0), default(Vector3), false);
        foreach (float d in new[] { -.4f, .4f }) Kit.Part(p, Kit.FrC(6, .03f, .03f, .85f), Wood, pos + new Vector3(d, .45f, 0));
        Kit.Part(p, Kit.GableC(1.0f, .9f, .35f), Kit.Roof, pos + new Vector3(0, 1.28f, 0), new Vector3(0, 90, 0));
    }

    public static void Barrel(Transform p, Vector3 pos)
    {
        Kit.Part(p, Kit.FrC(10, .17f, .2f, .42f), Wood, pos); foreach (float y in new[] { .08f, .33f }) Kit.Part(p, Kit.FrC(10, .205f, .205f, .025f), U.Mat(new Color(.22f, .22f, .22f), .3f), pos + new Vector3(0, y, 0));
    }

    /// <summary>Seluruh zona berdinding (kastil kecil). Dinding bersegmen dibuat terpisah di World.</summary>
    public static void Build(Transform g, Transform dyn, List<Material> windowMats, List<Light> lanterns, List<Material> lanternMats, List<Transform> flags, int ZX, int ZZ, int IX, int IZ)
    {
        Color[] fc = { new Color(.75f, .22f, .17f), new Color(.16f, .43f, .69f), new Color(.85f, .63f, .22f), new Color(.31f, .6f, .29f) };
        float x0 = ZX + .5f, x1 = ZX + 7.5f, z0 = ZZ + .5f, z1 = ZZ + 7.5f;
        var cs = new[] { new Vector3(x0, 0, z0), new Vector3(x1, 0, z0), new Vector3(x0, 0, z1), new Vector3(x1, 0, z1) }; float[] th = { 2.5f, 3.0f, 2.3f, 2.4f };
        RoundTower(g, cs[0], .66f, th[0], false); SpireTower(g, cs[1], .95f, th[1]); RoundTower(g, cs[2], .64f, th[2], true); SquareTower(g, cs[3], 1.0f, th[3]);
        float[] top = { th[0] + .45f, th[1] + 1.7f, th[2] + .85f, th[3] + .6f };
        for (int i = 0; i < 4; i++)
        {
            var c = cs[i] + new Vector3(0, top[i], 0);
            Kit.Part(g, Kit.FrC(6, .015f, .015f, .9f), Wood, c);
            var fl = Kit.Part(dyn, Kit.BoxC(.55f, .3f, .02f), U.Mat(fc[i], .05f), c + new Vector3(.3f, .62f, 0), default(Vector3), false); flags.Add(fl.transform);
            var lg = new GameObject("lantern"); lg.transform.position = cs[i] + new Vector3(0, top[i] - .3f, 0); lg.transform.SetParent(g, true); var lt = lg.AddComponent<Light>(); lt.type = LightType.Point; lt.range = 9f; lt.color = new Color(1f, .69f, .31f); lt.intensity = 0; lt.shadows = LightShadows.None; lanterns.Add(lt);
            var lm = U.Glow(new Color(1f, .82f, .5f)); Kit.Sc(Kit.Part(g, Kit.Cache("ls0", () => Kit.LowSphere(0)), lm, cs[i] + new Vector3(0, top[i] - .3f, 0), default(Vector3), false), .1f, .1f, .1f); lanternMats.Add(lm);
        }
        // gerbang melengkung (sisi timur)
        float gx = ZX + 7.5f, gz = ZZ + 4.0f;
        foreach (float dz in new[] { -.8f, .8f }) Kit.Part(g, Kit.BoxC(.7f, 1.7f, .55f), Kit.Stone, new Vector3(gx, .85f, gz + dz));
        int N = 9; for (int i = 0; i < N; i++) { float a = Mathf.PI * (i + .5f) / N; Kit.Part(g, Kit.BoxC(.7f, .26f, .4f), Kit.Stone, new Vector3(gx, 1.7f + Mathf.Sin(a) * .62f, gz - Mathf.Cos(a) * .62f), new Vector3(a * Mathf.Rad2Deg - 90f, 0, 0)); }
        Kit.Part(g, Kit.BoxC(.8f, .6f, 2.3f), Kit.Stone, new Vector3(gx, 2.75f, gz));
        for (int i = 0; i < 4; i++) Kit.Part(g, Kit.BoxC(.7f, .26f, .3f), Kit.Stone, new Vector3(gx, 3.2f, gz - .9f + i * .6f));
        for (int k = 0; k < 7; k++) Kit.Part(g, Kit.FrC(6, .022f, .022f, .95f), U.Mat(new Color(.16f, .16f, .16f), .3f), new Vector3(gx + .05f, 1.5f, gz - .45f + k * .15f));
        Kit.Part(g, Kit.BoxC(2.2f, .08f, 1.2f), Wood, new Vector3(gx + 1.3f, -.02f, gz));
        // rumah petani, lumbung oktagon
        House(g, new Vector3(IX + 1.2f, 0, IZ + 1.2f), 1.8f, 1.7f, 1.0f, .85f, windowMats, true, .55f);
        Octagon(g, new Vector3(Sim.GX, 0, Sim.GZ), .95f, 1.05f);
        Well(g, new Vector3(IX + 3f, 0, IZ + 2.7f));
        foreach (var b in new[] { new Vector3(IX + 5.4f, 0, IZ + 2.2f), new Vector3(IX + 5.7f, 0, IZ + 2.5f), new Vector3(IX + .4f, 0, IZ + 2.6f) }) Barrel(g, b);
        foreach (var c in new[] { new Vector3(IX + 2.2f, .17f, IZ + 2.9f), new Vector3(IX + 2.6f, .17f, IZ + 3.2f) }) Kit.Part(g, Kit.BoxC(.36f, .34f, .36f), Wood, c);
        foreach (float k in new[] { 0f, .5f }) Kit.Part(g, Kit.FrC(10, .22f, .22f, .5f), U.Mat(new Color(.85f, .72f, .35f), .05f), new Vector3(IX + 4.9f + k, .22f, IZ + 3.1f), new Vector3(0, 0, 90));
    }
}
