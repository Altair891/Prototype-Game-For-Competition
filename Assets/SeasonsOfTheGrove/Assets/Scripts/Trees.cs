using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 10 jenis pohon bergaya "Nature Tree Pack" (bulat, kubus, lonjong, cemara bertingkat, cemara bulat, palem, tiga gumpal, tumpukan, semak, sipres).
/// Tiap jenis = SATU mesh (2-3 submesh) = 1 renderer per pohon. Ini jauh lebih ringan dibanding puluhan objek per pohon.
/// </summary>
public static class TreeKit
{
    public const int Species = 10;
    public static bool Bake;   // true saat membuat dunia permanen: pohon diberi komponen BakedTree
    static readonly Mesh[] meshes = new Mesh[Species];
    static readonly int[][] maps = new int[Species][];
    static readonly float[] tops = { 1.85f, 1.6f, 2.4f, 2.15f, 1.9f, 2.5f, 2.1f, 1.95f, 1.35f, 2.2f };
    static readonly float[] ST = { 0, 0, 1f, .36f, .66f, 1f };
    static readonly Mesh[] meshesLow = new Mesh[Species];
    static readonly int[][] mapsLow = new int[Species][];
    static bool curLow;
    static Mesh seed; static int[] seedMap;
    static List<Kit.PartSpec> L;
    static Material bark, fruit;
    static readonly Material[,] crown = new Material[4, 6];
    static readonly Material[] burnt = new Material[5];
    static readonly Dictionary<int, Material[]> sets = new Dictionary<int, Material[]>();
    static readonly Color[] TINT = { Color.white, new Color(1.12f, 1.08f, .62f), new Color(.72f, .95f, 1.1f), new Color(1.35f, .9f, .55f) };
    static Color[] leaf, seat;

    public static float Top(int sp) { return tops[sp]; }
    public static float Scale(int stage, float pr)
    {
        if (stage <= 2) return 1f; if (stage >= 5) return 1f;
        return Mathf.Lerp(ST[stage], ST[stage + 1], pr);
    }

    public static void Reset()
    {
        for (int i = 0; i < Species; i++) { meshes[i] = null; meshesLow[i] = null; maps[i] = null; mapsLow[i] = null; }
        seed = null; seedMap = null; bark = null; fruit = null; sets.Clear(); curLow = false; L = null; Bake = false;
        for (int t = 0; t < 4; t++) for (int s = 0; s < 6; s++) crown[t, s] = null; for (int i = 0; i < 5; i++) burnt[i] = null;
    }

    public static void Init(Color[] leafC, Color[] seatC)
    {
        sets.Clear(); leaf = leafC; seat = seatC;
        bark = U.Mat(U.Hex(0x7a5a3b), .05f); fruit = U.Mat(U.Hex(0xd8483a), .4f);
        for (int t = 0; t < 4; t++) for (int s = 2; s <= 5; s++) crown[t, s] = new Material(U.Mat(leafC[s], .05f));
        for (int i = 0; i < 5; i++) burnt[i] = U.Mat(Color.Lerp(U.Hex(0x2a2420), U.Hex(0x6a5a3a), i / 4f), .05f);
        Recolor(0, false);
    }

    /// <summary>Warna daun mengikuti musim (dan biru tipis saat Air Berkah). Dipanggil jarang.</summary>
    public static void Recolor(int se, bool wet)
    {
        for (int t = 0; t < 4; t++)
            for (int s = 2; s <= 5; s++)
            {
                Color c = Color.Lerp(leaf[s], seat[se], .22f); c = new Color(c.r * TINT[t].r, c.g * TINT[t].g, c.b * TINT[t].b, 1f);
                if (wet) c = Color.Lerp(c, U.Hex(0x4fa8ff), .3f);
                crown[t, s].color = c;
            }
    }

    // ---------- pembangun ----------
    static void Add(Mesh m, Vector3 p, Vector3 s, int g, Vector3 e) { L.Add(new Kit.PartSpec { m = m, x = Matrix4x4.TRS(p, Quaternion.Euler(e), s), g = g }); }
    static void Ball(Vector3 p, Vector3 s, int g = 1, int det = 1) { Add(Kit.Sphere(curLow ? Mathf.Max(0, det - 1) : det, true), p, s, g, Vector3.zero); }
    static void Trunk(float r0, float r1, float h) { Add(Kit.Cache("tk" + r0 + "," + r1 + "," + h, () => Kit.Frustum(6, r0, r1, h, 1f, false)), Vector3.zero, Vector3.one, 0, Vector3.zero); }
    static void Tier(float r, float h, float y) { Add(Kit.Cache("tt" + r + "," + h, () => Kit.Frustum(8, r, 0f, h, 1f, false)), new Vector3(0, y, 0), Vector3.one, 1, Vector3.zero); }
    static void Cube(Vector3 p, Vector3 s, int g, float yaw) { Add(Kit.Cache("cube1", () => Kit.Box(1, 1, 1, 1f)), p, s, g, new Vector3(0, yaw, 0)); }

    static Mesh Blade(float len, float w)
    {
        return Kit.Cache("bl" + len + "," + w, () =>
        {
            var b = new Kit.MB(); var o = Vector3.zero; var l = new Vector3(-w / 2, .03f, len * .35f); var r = new Vector3(w / 2, .03f, len * .35f); var t = new Vector3(0, -len * .14f, len);
            b.Tri(o, l, r, Vector3.up); b.Tri(l, t, r, Vector3.up); b.Tri(o, l, r, Vector3.down); b.Tri(l, t, r, Vector3.down); return b.Build();
        });
    }

    static Mesh Build(int sp, out int[] map)
    {
        L = new List<Kit.PartSpec>();
        switch (sp)
        {
            case 0: // bulat bergumpal + buah
                Trunk(.1f, .075f, .75f); Ball(new Vector3(0, 1.1f, 0), new Vector3(.58f, .52f, .58f), 1, 2);
                for (int k = 0; k < 7; k++) { float a = k * .9f; Ball(new Vector3(Mathf.Cos(a) * .42f, .95f + (k % 3) * .18f, Mathf.Sin(a) * .42f), new Vector3(.24f, .2f, .24f)); }
                for (int k = 0; k < 4; k++) { float a = k * 1.6f; Ball(new Vector3(Mathf.Cos(a) * .52f, 1.0f + (k % 2) * .25f, Mathf.Sin(a) * .52f), Vector3.one * .06f, 2, 0); }
                break;
            case 1: // kubus
                Trunk(.11f, .09f, .65f); Cube(new Vector3(0, 1.1f, 0), new Vector3(.9f, .8f, .9f), 1, 15f);
                Cube(new Vector3(.34f, 1.52f, .2f), Vector3.one * .34f, 1, 40f); Cube(new Vector3(-.3f, 1.55f, -.22f), Vector3.one * .3f, 1, 10f); Cube(new Vector3(-.4f, 1.25f, .3f), Vector3.one * .28f, 1, 25f);
                break;
            case 2: // lonjong tinggi
                Trunk(.07f, .055f, .9f); Ball(new Vector3(0, 1.55f, 0), new Vector3(.4f, .78f, .4f), 1, 2); break;
            case 3: // cemara bertingkat
                Trunk(.08f, .07f, .4f); Tier(.62f, .75f, .3f); Tier(.5f, .7f, .7f); Tier(.38f, .65f, 1.1f); Tier(.26f, .6f, 1.5f); break;
            case 4: // cemara bulat bertingkat (oranye musim gugur)
                Trunk(.08f, .07f, .45f); Ball(new Vector3(0, .65f, 0), new Vector3(.55f, .3f, .55f)); Ball(new Vector3(0, 1.0f, 0), new Vector3(.42f, .28f, .42f)); Ball(new Vector3(0, 1.32f, 0), new Vector3(.3f, .25f, .3f));
                Tier(.16f, .38f, 1.5f); for (int k = 0; k < 6; k++) { float a = k * 1.05f; Ball(new Vector3(Mathf.Cos(a) * .5f, .62f, Mathf.Sin(a) * .5f), new Vector3(.16f, .12f, .16f)); }
                break;
            case 5: // palem
                Trunk(.1f, .065f, 1.6f);
                for (int k = 0; k < 9; k++) Add(Blade(.95f, .28f), new Vector3(0, 1.6f, 0), Vector3.one, 1, new Vector3(-18f, k * 40f, 0));
                Ball(new Vector3(0, 1.58f, 0), Vector3.one * .12f, 2, 0); break;
            case 6: // tiga gumpal
                Trunk(.08f, .07f, .9f); Ball(new Vector3(-.3f, 1.25f, 0), new Vector3(.27f, .55f, .27f)); Ball(new Vector3(.28f, 1.2f, .05f), new Vector3(.27f, .5f, .27f)); Ball(new Vector3(0, 1.45f, -.05f), new Vector3(.3f, .62f, .3f)); break;
            case 7: // tumpukan gumpal
                Trunk(.1f, .08f, .5f); Ball(new Vector3(0, .85f, 0), new Vector3(.5f, .38f, .5f)); Ball(new Vector3(.04f, 1.25f, .05f), new Vector3(.4f, .34f, .4f)); Ball(new Vector3(0, 1.6f, 0), new Vector3(.3f, .3f, .3f)); break;
            case 8: // semak
                Trunk(.08f, .07f, .25f); Ball(new Vector3(0, .7f, 0), new Vector3(.6f, .5f, .6f), 1, 2);
                for (int k = 0; k < 8; k++) { float a = k * .79f; Ball(new Vector3(Mathf.Cos(a) * .48f, .6f + (k % 2) * .2f, Mathf.Sin(a) * .48f), new Vector3(.2f, .17f, .2f)); }
                break;
            default: // sipres
                Trunk(.06f, .05f, .35f); Ball(new Vector3(0, 1.2f, 0), new Vector3(.3f, .9f, .3f), 1, 2); Ball(new Vector3(.12f, 1.9f, 0), new Vector3(.14f, .22f, .14f)); break;
        }
        var m = Kit.Merge(L, 3, out map); L = null; return m;
    }

    static void BuildSeed()
    {
        L = new List<Kit.PartSpec>();
        Add(Kit.Cache("sd", () => Kit.Frustum(5, .02f, .014f, .22f, 1f, false)), Vector3.zero, Vector3.one, 0, Vector3.zero);
        foreach (float x in new[] { -1f, 1f }) Add(Kit.Sphere(0, true), new Vector3(x * .08f, .22f, 0), new Vector3(.1f, .04f, .07f), 1, new Vector3(0, 0, -x * 28f));
        seed = Kit.Merge(L, 3, out seedMap); L = null;
    }

    /// <summary>low=true: versi berpoligon lebih sedikit untuk pohon latar jauh (hutan, bukit).</summary>
    public static Mesh MeshFor(int sp, int stage, bool low = false)
    {
        if (stage <= 2) { if (!seed) BuildSeed(); return seed; }
        if (low) { if (!meshesLow[sp]) { curLow = true; meshesLow[sp] = Build(sp, out mapsLow[sp]); curLow = false; } return meshesLow[sp]; }
        if (!meshes[sp]) meshes[sp] = Build(sp, out maps[sp]);
        return meshes[sp];
    }

    public static Material[] Mats(int sp, int tint, int stage, int burn, bool low = false)
    {
        int key = (((sp * 4 + tint) * 6 + stage) * 6 + (burn + 1)) * 2 + (low ? 1 : 0);
        Material[] a; if (sets.TryGetValue(key, out a)) return a;
        MeshFor(sp, stage, low); int[] map = stage <= 2 ? seedMap : (low ? mapsLow[sp] : maps[sp]); a = new Material[map.Length];
        Material cm = burn >= 0 ? burnt[burn] : crown[tint, stage];
        for (int k = 0; k < map.Length; k++) a[k] = map[k] == 0 ? bark : map[k] == 1 ? cm : (stage == 5 && burn < 0 ? fruit : cm);
        sets[key] = a; return a;
    }

    // ---------- pilihan jenis per lokasi ----------
    static float Hash01(float x, float z) { return Mathf.Abs(Mathf.Sin(x * 12.9898f + z * 78.233f) * 43758.5453f) % 1f; }
    static readonly int[][] zones = { new[] { 0, 8, 0 }, new[] { 1, 7, 1 }, new[] { 2, 6, 9 }, new[] { 3, 4, 3 }, new[] { 5, 0, 6 }, new[] { 9, 2, 7 } };

    /// <summary>Jenis dipilih lewat noise sehingga membentuk rumpun (hutan cemara, kebun bulat, dst), 70% jenis utama + 30% acak.</summary>
    public static int Pick(float x, float z, float r)
    {
        float n = Mathf.PerlinNoise(x * .14f + 13.7f, z * .14f + 3.3f); int zone = Mathf.Clamp((int)(n * 6f), 0, 5);
        if (r < .7f) return zones[zone][(int)(Hash01(x, z) * 3f) % 3];
        return (int)(Hash01(z + 3.1f, x + 7.7f) * Species) % Species;
    }
    public static int Tint(int sp, float r)
    {
        if (sp == 4) return r < .55f ? 3 : 0; if (sp == 3) return r < .3f ? 2 : 0;
        return r < .55f ? 0 : r < .75f ? 1 : r < .92f ? 2 : 3;
    }

    public static GameObject Spawn(Transform parent, Vector3 pos, float yaw, int sp, int tint, float scale, int stage, bool shadow, bool low = false)
    {
        var g = new GameObject("tree"); g.transform.SetParent(parent, false); g.transform.position = pos; g.transform.rotation = Quaternion.Euler(0, yaw, 0); g.transform.localScale = Vector3.one * scale;
        g.AddComponent<MeshFilter>().sharedMesh = MeshFor(sp, stage, low); var r = g.AddComponent<MeshRenderer>(); r.sharedMaterials = Mats(sp, tint, stage, -1, low);
        r.shadowCastingMode = shadow ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        if (Bake) { var bt = g.AddComponent<BakedTree>(); bt.species = sp; bt.tint = tint; bt.low = low; }
        return g;
    }
}
