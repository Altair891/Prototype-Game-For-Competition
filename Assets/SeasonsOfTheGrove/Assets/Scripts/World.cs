using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

/// <summary>Seluruh tampilan 3D. Membaca Sim dan memperbarui hanya yang berubah (event Dirty), supaya ringan.</summary>
public class World : MonoBehaviour
{
    const int N = Sim.N, ZX = Sim.ZX, ZZ = Sim.ZZ, IX = ZX + 1, IZ = ZZ + 1;
    static readonly Color[] SOIL = { U.Hex(0xa67c52), U.Hex(0x9a7b55), U.Hex(0xb07a40), U.Hex(0xdfe8ee) };
    static readonly Color[] DIRT = { U.Hex(0x6e6150), U.Hex(0x675f4e), U.Hex(0x6b5a44), U.Hex(0x9fa9b0) };
    static readonly Color[] GRASS = { U.Hex(0x7fa35a), U.Hex(0x8fbf6a), U.Hex(0xa8823a), U.Hex(0xe8f0f4) };
    public static readonly Color[] SEAT = { U.Hex(0x4fa84c), U.Hex(0x9be070), U.Hex(0xe08a2e), U.Hex(0xcfe0e8) };
    static readonly Color[] SKYC = { U.Hex(0xbfe0f0), U.Hex(0xcfe9f2), U.Hex(0xe8c79a), U.Hex(0xcfd9e3) };
    public static readonly Color[] LEAF = { Color.black, Color.black, U.Hex(0xb5e48c), U.Hex(0x8bd16a), U.Hex(0x52b052), U.Hex(0x2b7a3a) };
    static readonly Color[] HAT = { U.Hex(0xc0392b), U.Hex(0x7a4b2a), U.Hex(0xd98324), U.Hex(0x4a6fa5) };
    static readonly float[] RAD = { 0, 0, .12f, .22f, .32f, .42f };
    static readonly string[] NPCN = { "Tukang Tembok", "Tabib Air", "Penjaga", "Saudagar" };
    static readonly Color[] NPCC = { U.Hex(0x8a6d3b), U.Hex(0x3b8a6d), U.Hex(0x6d3b8a), U.Hex(0xb5493b) };

    Sim S; Camera cam; Light sun; ReflectionProbe probe;
    Transform treesRoot, fenceRoot, wallRoot, npcRoot, dynRoot, animalRoot;
    Texture2D gt; Color[] px; bool texDirty;
    readonly HashSet<int> dirty = new HashSet<int>();
    public Texture2D GroundTex { get { return gt; } }

    // ---- pohon ----
    class TV { public GameObject root; public MeshFilter mf; public MeshRenderer mr; public int species, tint, lastStage = -1, lastBurn = -99, ls = -1; public float pop, nextT, baseScale; }
    readonly Dictionary<int, TV> tv = new Dictionary<int, TV>();
    readonly Dictionary<int, Transform[]> fences = new Dictionary<int, Transform[]>();
    class FireV { public Transform flame, bar; }
    readonly Dictionary<int, FireV> fires = new Dictionary<int, FireV>(); readonly Stack<FireV> firePool = new Stack<FireV>();
    Material flameM, barM; bool wetOn; Transform fireRoot;
    public Vector3[] castlePerch = new Vector3[0];
    float envT, probeT2;

    // ---- kastil / dinding ----
    class WV { public Transform root, body; public Transform[] m = new Transform[3]; public bool alongX; }
    readonly List<WV> walls = new List<WV>();
    readonly List<Material> windowMats = new List<Material>();
    readonly List<Light> lanterns = new List<Light>();
    readonly List<Material> lanternMats = new List<Material>();
    readonly List<Transform> flags = new List<Transform>();
    Transform[] npcG = new Transform[4];
    public Vector3[] NpcPos = new Vector3[4];

    // ---- petani ----
    public Transform Farmer;
    Transform[] legU = new Transform[2], legL = new Transform[2], hats = new Transform[4];
    Transform torso, beard, flower, glasses, mouth; Transform[] arms = new Transform[2];
    Renderer[] legUR = new Renderer[2], torsoR = new Renderer[3];
    public Vector3 FarmerTarget;
    int dressed = -1; float walk;

    // ---- hewan / langit ----
    class SheepV { public Transform root, head; public Transform[] legs; public Vector3 tgt; public float wait; }
    readonly List<SheepV> sheep = new List<SheepV>();
    class BirdV { public Transform root; public Transform[] w; public float ph; }
    readonly List<BirdV> birds = new List<BirdV>();
    readonly List<Transform> cloudsHigh = new List<Transform>(), cloudShadow = new List<Transform>();
    Chibi farmerC; Chibi[] npcC = new Chibi[4]; readonly Dictionary<Intr, Chibi> ic = new Dictionary<Intr, Chibi>();
    Material skyM; ParticleSystem ps; LineRenderer cursor;
    Vector3 sunDir; public float Night, Dn = 1f; public Vector3 SunDirection { get { return sunDir; } }
    float probeT, ambT;

    public Vector3 CursorPos; public bool CursorOn;

    void Start()
    {
        S = Sim.I;
        cam = Camera.main; if (!cam) { var cg = new GameObject("Main Camera"); cg.tag = "MainCamera"; cam = cg.AddComponent<Camera>(); cg.AddComponent<AudioListener>(); }
        cam.nearClipPlane = .1f; cam.farClipPlane = 400f; cam.fieldOfView = 45f; cam.allowHDR = true;
#if UNITY_2023_1_OR_NEWER
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) l.enabled = false;
#else
        foreach (var l in FindObjectsOfType<Light>()) l.enabled = false;
#endif

        var sg = new GameObject("Sun"); sun = sg.AddComponent<Light>(); sun.type = LightType.Directional; sun.shadows = LightShadows.Soft; sun.shadowStrength = .9f; RenderSettings.sun = sun;
        var skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader) { skyM = new Material(skyShader); RenderSettings.skybox = skyM; cam.clearFlags = CameraClearFlags.Skybox; } else { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = SKYC[0]; }
        RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;

        treesRoot = new GameObject("Trees").transform; fenceRoot = new GameObject("Fences").transform; wallRoot = new GameObject("Walls").transform;
        npcRoot = new GameObject("NPC").transform; dynRoot = new GameObject("Dyn").transform; animalRoot = new GameObject("Animals").transform;

        var baked = FindBaked(); bool actors = baked != null && baked.farmer != null;
        Safe(BuildMaterials, "Material");
        if (actors) Safe(BuildGroundData, "Data tanah"); else Safe(BuildGround, "Tanah");
        if (baked != null) { Safe(() => AdoptBaked(baked), "Dunia permanen"); Safe(BuildProbe, "Probe refleksi"); }
        else { Safe(BuildScenery, "Hutan dan ornamen"); Safe(BuildLake, "Danau"); Safe(BuildZone, "Kastil zona"); }
        if (!actors) { Safe(BuildNpcs, "NPC"); Safe(BuildFarmer, "Petani"); Safe(BuildAnimals, "Hewan"); Safe(BuildClouds, "Awan"); }
        Safe(BuildParticles, "Partikel");
        var cg2 = new GameObject("Cursor"); cursor = cg2.AddComponent<LineRenderer>(); cursor.useWorldSpace = false; cursor.loop = true; cursor.positionCount = 32; cursor.widthMultiplier = .06f;
        cursor.sharedMaterial = RingMat(); cursor.startColor = cursor.endColor = new Color(1, 1, 1, .85f);
        for (int i = 0; i < 32; i++) { float a = i / 32f * Mathf.PI * 2; cursor.SetPosition(i, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a))); }

        S.Dirty += i => dirty.Add(i);
        S.Fx += Emit; S.SeasonChanged += OnSeason; S.Spawned += OnSpawn; S.WallDirty += OnWall; S.Restarted += OnRestart;
        S.NewGame();
        OnSeason();
        Safe(() => gameObject.AddComponent<Aviary>().Init(this), "Burung");
        ready = Farmer != null && farmerC != null && npcC[0] != null;
        if (!ready) Debug.LogError("[Grove] Dunia belum lengkap (lihat error di atas). Game dihentikan agar tidak membanjiri Console.");
    }

    bool ready;
    public bool Ready { get { return ready; } }
    static Material ringM;
    static Material RingMat() { if (!ringM) ringM = new Material(Shader.Find("Sprites/Default")); return ringM; }
    /// <summary>Satu langkah pembangunan gagal tidak lagi membatalkan seluruh dunia: error dicatat lalu langkah berikutnya tetap jalan.</summary>
    void Safe(System.Action a, string name) { try { a(); } catch (System.Exception e) { Debug.LogError("[Grove] Gagal membangun '" + name + "': " + e); } }
    BakedWorld FindBaked()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<BakedWorld>();
#else
        return FindObjectOfType<BakedWorld>();
#endif
    }

    void OnRestart() { if (Farmer == null) return; ic.Clear(); foreach (var k in new List<int>(fires.Keys)) RemoveFire(k); Farmer.position = new Vector3(IX + 3, 0, IZ + 3); FarmerTarget = Farmer.position; foreach (var kv in tv) kv.Value.root.SetActive(false); foreach (var kv in fences) { kv.Value[0].gameObject.SetActive(false); kv.Value[1].gameObject.SetActive(false); } }

    // =============== material ===============
    void BuildMaterials()
    {
        TreeKit.Init(LEAF, SEAT);
        flameM = U.Glow(U.Hex(0xff7a1a)); U.SetGlow(flameM, U.Hex(0xff7a1a), 2f); barM = U.Mat(U.Hex(0x4fa8ff));
        fireRoot = new GameObject("Fire").transform;
    }

    // =============== tanah ===============
    Renderer tileR, bigR;
    static void KillCollider(GameObject g) { var c = g.GetComponent<Collider>(); if (!c) return; if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
    void BuildGroundData() { gt = new Texture2D(N, N, TextureFormat.RGBA32, false); gt.filterMode = FilterMode.Point; gt.wrapMode = TextureWrapMode.Clamp; px = new Color[N * N]; }
    void BuildGround() { BuildGroundData(); BuildGroundObjects(null); }
    void BuildGroundObjects(Transform parent)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad); KillCollider(q); q.name = "Tiles"; q.transform.SetParent(parent, false);
        q.transform.position = new Vector3(N / 2f, 0, N / 2f); q.transform.rotation = Quaternion.Euler(90, 0, 0); q.transform.localScale = new Vector3(N, N, 1);
        var m = new Material(U.Mat(Color.white, .02f)); m.mainTexture = gt; tileR = q.GetComponent<Renderer>(); tileR.sharedMaterial = m; tileR.shadowCastingMode = ShadowCastingMode.Off;
        var big = GameObject.CreatePrimitive(PrimitiveType.Plane); KillCollider(big); big.name = "Ground"; big.transform.SetParent(parent, false);
        big.transform.position = new Vector3(N / 2f, -.05f, N / 2f); big.transform.localScale = Vector3.one * 40f;
        bigR = big.GetComponent<Renderer>(); bigM = new Material(U.Mat(GRASS[0], .02f)); bigR.sharedMaterial = bigM;
    }
    void BakeFillTiles()
    {
        for (int z = 0; z < N; z++)
            for (int x = 0; x < N; x++)
            {
                Color c = DIRT[0];
                if (x >= ZX && x < ZX + 8 && z >= ZZ && z < ZZ + 8) c = U.Hex(0xb8ad94); else if (x >= ZX + 9 && x < ZX + 16 && z >= ZZ && z < ZZ + 7) c = SOIL[0];
                c *= 1f + (((x * 7 + z * 13) % 9) - 4) * .01f; c.a = 1f; px[z * N + x] = c;
            }
        gt.SetPixels(px); gt.Apply(false);
    }
    Material bigM;

    Color TileColor(int i)
    {
        var T = S.tl[i]; float v = 1f + T.ox * .5f; int se = S.se; Color c;
        switch (T.s)
        {
            case 0: c = Color.Lerp(DIRT[se], SOIL[se], T.cl); break;
            case 7: c = U.Hex(0xb8ad94); break;
            case 6: c = Color.Lerp(SOIL[se], Color.black, .4f); break;
            case 2: case 3: case 4: case 5: c = Color.Lerp(SOIL[se], GRASS[se], .5f); break;
            default: c = SOIL[se]; break;
        }
        c *= v; c.a = 1f; return c;
    }

    // =============== pohon ===============
    static float Hash01(float x, float z) { return Mathf.Abs(Mathf.Sin(x * 12.9898f + z * 78.233f) * 43758.5453f) % 1f; }

    TV GetTV(int i)
    {
        TV v; if (tv.TryGetValue(i, out v)) return v;
        v = new TV(); var T = S.tl[i]; int tx = i % N, tz = i / N;
        float r1 = Hash01(tx * 3.1f, tz * 7.7f), r2 = Hash01(tz * 5.3f + 1f, tx * 2.9f + 2f);
        v.species = TreeKit.Pick(tx, tz, r1); v.tint = TreeKit.Tint(v.species, r2); v.baseScale = .88f + Hash01(tx + .5f, tz + .25f) * .3f;
        var g = new GameObject("tree" + i); g.transform.SetParent(treesRoot, false);
        g.transform.position = new Vector3(tx + .5f + T.ox, 0, tz + .5f + T.oy); g.transform.rotation = Quaternion.Euler(0, r2 * 360f, 0);
        v.root = g; v.mf = g.AddComponent<MeshFilter>(); v.mr = g.AddComponent<MeshRenderer>(); v.mr.shadowCastingMode = ShadowCastingMode.On;
        tv[i] = v; return v;
    }

    /// <summary>Hanya mengganti mesh/material saat tahap berubah; selain itu cukup mengatur skala (murah).</summary>
    void LayoutTree(int i, TV v)
    {
        var T = S.tl[i]; int s = T.s == 6 ? T.pv : T.s; if (s < 2) s = 2;
        int burn = T.s == 6 ? Mathf.Clamp(Mathf.FloorToInt(T.hp / 20f), 0, 4) : -1;
        if (s != v.lastStage || burn != v.lastBurn) { v.lastStage = s; v.lastBurn = burn; v.mf.sharedMesh = TreeKit.MeshFor(v.species, s); v.mr.sharedMaterials = TreeKit.Mats(v.species, v.tint, s, burn); }
        float pr = (T.s >= 2 && T.s < 5 && T.d > 0) ? T.t / T.d : 0f;
        float sc = TreeKit.Scale(s, pr) * v.baseScale * (1f + .3f * v.pop * Mathf.Sin((1f - v.pop) * 14f));
        v.root.transform.localScale = new Vector3(sc, sc, sc);
    }

    void AddFire(int i)
    {
        if (fires.ContainsKey(i)) return;
        FireV f = firePool.Count > 0 ? firePool.Pop() : MakeFire(); f.flame.gameObject.SetActive(true); f.bar.gameObject.SetActive(true); fires[i] = f;
    }
    void RemoveFire(int i)
    {
        FireV f; if (!fires.TryGetValue(i, out f)) return;
        f.flame.gameObject.SetActive(false); f.bar.gameObject.SetActive(false); firePool.Push(f); fires.Remove(i);
    }
    FireV MakeFire()
    {
        var f = new FireV(); var fl = U.Cone(fireRoot, Vector3.zero, .18f, .5f, Color.white, 6); var r1 = fl.GetComponent<Renderer>(); r1.sharedMaterial = flameM; r1.shadowCastingMode = ShadowCastingMode.Off; f.flame = fl.transform;
        var br = U.Cube(fireRoot, Vector3.zero, new Vector3(.6f, .06f, .06f), Color.white); var r2 = br.GetComponent<Renderer>(); r2.sharedMaterial = barM; r2.shadowCastingMode = ShadowCastingMode.Off; f.bar = br.transform; return f;
    }
    void UpdateFire(float time)
    {
        foreach (var kv in fires)
        {
            int i = kv.Key; var T = S.tl[i]; var f = kv.Value; TV v; float h = tv.TryGetValue(i, out v) ? TreeKit.Top(v.species) * v.root.transform.localScale.y : 1f;
            float fl = Mathf.Sin(time * 11f + i); Vector3 b = new Vector3(i % N + .5f, 0, i / N + .5f);
            f.flame.position = b + Vector3.up * h * .55f; f.flame.localScale = new Vector3(.36f * (1 + .2f * fl), .5f * (1 + .4f * fl), .36f * (1 + .2f * fl));
            f.bar.position = b + Vector3.up * (h + .35f); f.bar.localScale = new Vector3(Mathf.Max(.01f, T.m / 100f) * .6f, .06f, .06f); f.bar.rotation = cam.transform.rotation;
        }
    }

    void Refresh(int i)
    {
        var T = S.tl[i]; px[i] = TileColor(i); texDirty = true;
        bool tree = T.s >= 2 && T.s <= 6;
        if (tree) { var v = GetTV(i); if (!v.root.activeSelf) v.root.SetActive(true); if (v.ls != T.s) { v.ls = T.s; v.pop = 1f; } LayoutTree(i, v); }
        else { TV v; if (tv.TryGetValue(i, out v)) { if (v.root.activeSelf) v.root.SetActive(false); v.ls = -1; v.lastStage = -1; } }
        if (T.s == 6) AddFire(i); else RemoveFire(i);
        Transform[] f;
        if (T.s == 8)
        {
            if (!fences.TryGetValue(i, out f))
            {
                var st = U.Cube(fenceRoot, new Vector3(i % N + .5f, .3f, i / N + .5f), new Vector3(.95f, .6f, .95f), U.Hex(0xbab4a8));
                var iv = U.Ball(fenceRoot, new Vector3(i % N + .5f, .62f, i / N + .5f), .4f, U.Hex(0x4f9a4a));
                f = new[] { st.transform, iv.transform }; fences[i] = f;
            }
            f[0].gameObject.SetActive(true); f[1].gameObject.SetActive(true);
        }
        else if (fences.TryGetValue(i, out f)) { f[0].gameObject.SetActive(false); f[1].gameObject.SetActive(false); }
    }

    /// <summary>Tempat hinggap burung: puncak menara/atap, pucuk pohon, tanah, atau sekitar pagar.</summary>
    public Vector3 RandomPerch(Vector3 from)
    {
        for (int a = 0; a < 14; a++)
        {
            float r = Random.value; Vector3 p;
            if (r < .28f && castlePerch.Length > 0) p = castlePerch[Random.Range(0, castlePerch.Length)];
            else if (r < .38f && Decor.Perches.Count > 0) p = Decor.Perches[Random.Range(0, Decor.Perches.Count)];
            else if (r < .66f)
            {
                p = Vector3.zero; bool ok = false;
                for (int k = 0; k < 40 && !ok; k++)
                {
                    int i = Random.Range(0, N * N); TV v;
                    if ((S.tl[i].s == 4 || S.tl[i].s == 5) && tv.TryGetValue(i, out v) && v.root.activeSelf) { p = v.root.transform.position + Vector3.up * (TreeKit.Top(v.species) * v.root.transform.localScale.y * .93f); ok = true; }
                }
                if (!ok) continue;
            }
            else
            {
                int i = Random.Range(0, N * N); int st = S.tl[i].s; if (st == 7 || st == 6 || (st >= 2 && st <= 5)) continue;
                p = new Vector3(i % N + .5f, st == 8 ? .65f : .05f, i / N + .5f);
            }
            if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(from.x, from.z)) > 6f) return p;
        }
        return new Vector3(Random.Range(4f, 26f), .05f, Random.Range(2f, 12f));
    }

    // =============== zona / kastil ===============
    void BuildScenery() { WorldGen.Scenery(null, false); }

    void BuildLake() { WorldGen.Lake(null); BuildProbe(); }

    void BuildProbe()
    {
        float cx = N / 2f + 8f, cz = -14f;
        var pg = new GameObject("ReflectionProbe"); pg.transform.position = new Vector3(cx, 4f, cz);
        probe = pg.AddComponent<ReflectionProbe>(); probe.mode = ReflectionProbeMode.Realtime; probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces; probe.resolution = 64; probe.size = new Vector3(160f, 40f, 160f); probe.boxProjection = false;
    }

    void BuildZone() { castlePerch = WorldGen.Zone(null, dynRoot, windowMats, lanterns, lanternMats, flags, false); }

    void BuildWallViews()
    {
        var cells = new List<Vector2>(); foreach (var wl in S.wl) cells.Add(new Vector2(wl.x, wl.z)); BuildWallObjects(cells);
    }

    void BuildWallObjects(List<Vector2> cells)
    {
        foreach (var w in walls) if (w.root) { if (Application.isPlaying) Destroy(w.root.gameObject); else DestroyImmediate(w.root.gameObject); }
        walls.Clear();
        foreach (var c in cells)
        {
            var v = new WV(); var g = new GameObject("wall"); g.transform.SetParent(wallRoot, false); g.transform.position = new Vector3(c.x, 0, c.y); v.root = g.transform;
            bool ax = Mathf.Abs(c.y - (ZZ + .5f)) < .1f || Mathf.Abs(c.y - (ZZ + 7.5f)) < .1f; v.alongX = ax;
            v.body = Kit.Part(g.transform, Kit.BoxC(ax ? 1.02f : .8f, 1f, ax ? .8f : 1.02f), Kit.Stone, new Vector3(0, .55f, 0)).transform;
            for (int k = 0; k < 3; k++) v.m[k] = Kit.Part(g.transform, Kit.BoxC(ax ? .22f : .8f, .28f, ax ? .8f : .22f), Kit.Stone, new Vector3(ax ? (k - 1) * .36f : 0, 1.25f, ax ? 0 : (k - 1) * .36f)).transform;
            walls.Add(v);
        }
    }

    void OnWall(int j)
    {
        if (walls.Count != S.wl.Count) BuildWallViews();
        if (j < 0 || j >= walls.Count) return;
        var w = S.wl[j]; var v = walls[j]; float h = w.st == 0 ? 1.1f : w.st == 1 ? .7f : .15f;
        v.body.localScale = new Vector3(1, h, 1); v.body.localPosition = new Vector3(0, h / 2f, 0);
        foreach (var m in v.m) { m.gameObject.SetActive(w.st == 0); m.localPosition = new Vector3(m.localPosition.x, h + .14f, m.localPosition.z); }
        v.body.GetComponent<Renderer>().sharedMaterial = w.st == 0 ? Kit.Stone : w.st == 1 ? Kit.StoneDark : Kit.Rubble;
    }

    /// <summary>Memakai dunia permanen hasil Bake (jika ada) sebagai pengganti hutan latar, ornamen, danau, dan kastil zona yang dibangun lewat skrip.</summary>
    public void AdoptBaked(BakedWorld b)
    {
        if (b.windows != null) windowMats.AddRange(b.windows);
        if (b.lanterns != null) lanterns.AddRange(b.lanterns);
        if (b.lanternMats != null) lanternMats.AddRange(b.lanternMats);
        if (b.flags != null) flags.AddRange(b.flags);
        castlePerch = b.castlePerch != null ? b.castlePerch : new Vector3[0];
        Decor.Perches.Clear(); if (b.perches != null) Decor.Perches.AddRange(b.perches);
        Decor.Adopt(b.hill, b.far);
        if (b.tileRenderer) { var tm = new Material(b.tileRenderer.sharedMaterial); tm.mainTexture = gt; b.tileRenderer.sharedMaterial = tm; tileR = b.tileRenderer; }
        if (b.groundRenderer) { bigM = new Material(b.groundRenderer.sharedMaterial); b.groundRenderer.sharedMaterial = bigM; bigR = b.groundRenderer; }
        if (b.farmer && b.farmerChibi != null)
        {
            Farmer = b.farmer; farmerC = b.farmerChibi; torso = farmerC.torso; mouth = farmerC.mouth; beard = farmerC.beard; flower = farmerC.flower; glasses = farmerC.glasses;
            FarmerTarget = Farmer.position;
        }
        if (b.npcGroups != null) for (int i = 0; i < 4 && i < b.npcGroups.Length; i++) { npcG[i] = b.npcGroups[i]; npcC[i] = b.npcChibis[i]; NpcPos[i] = b.npcPos[i]; }
        if (b.sheep != null) foreach (var r in b.sheep) sheep.Add(new SheepV { root = r.root, head = r.head, legs = r.legs, tgt = r.root.position, wait = Random.value * 3f });
        if (b.clouds != null) cloudsHigh.AddRange(b.clouds);
        if (b.cloudShadows != null) cloudShadow.AddRange(b.cloudShadows);
        if (b.walls != null) foreach (var wr in b.walls) walls.Add(new WV { root = wr.root, body = wr.body, m = wr.m, alongX = wr.alongX });
        foreach (var bt in b.GetComponentsInChildren<BakedTree>(true)) { var r = bt.GetComponent<MeshRenderer>(); if (r) r.sharedMaterials = TreeKit.Mats(bt.species, bt.tint, 5, -1, bt.low); }
    }

    /// <summary>Dipanggil menu Bake di mode Edit: membuat tanah, NPC dan rumahnya, petani, domba, awan, dan tembok sebagai objek Scene, lalu mencatat referensinya.</summary>
    public void BakeInto(BakedWorld bw, Transform root, List<Material> windows)
    {
        Transform Sub(string n) { var t = new GameObject(n).transform; t.SetParent(root, false); return t; }
        dynRoot = Sub("Awan"); npcRoot = Sub("NPC"); animalRoot = Sub("Domba"); wallRoot = Sub("Tembok"); var gr = Sub("Tanah"); var actors = Sub("Petani");
        BuildGroundData(); BakeFillTiles(); BuildGroundObjects(gr);
        BuildNpcs(); foreach (var g in npcG) g.gameObject.SetActive(true);
        BuildFarmer(); DressSeason(0); Farmer.SetParent(actors, true);
        BuildAnimals(); BuildClouds();
        var cells = Sim.WallCells(); BuildWallObjects(cells);
        bw.farmer = Farmer; bw.farmerChibi = farmerC; bw.npcGroups = (Transform[])npcG.Clone(); bw.npcChibis = (Chibi[])npcC.Clone(); bw.npcPos = (Vector3[])NpcPos.Clone();
        bw.sheep = sheep.ConvertAll(s => new SheepRef { root = s.root, head = s.head, legs = s.legs }).ToArray();
        bw.clouds = cloudsHigh.ToArray(); bw.cloudShadows = cloudShadow.ToArray();
        bw.walls = walls.ConvertAll(x => new WallRef { root = x.root, body = x.body, m = x.m, alongX = x.alongX }).ToArray();
        bw.tileRenderer = tileR; bw.groundRenderer = bigR; windows.AddRange(windowMats);
    }

    // =============== NPC ===============
    void BuildNpcs()
    {
        Color[] skin = { U.Hex(0xe0b58f), U.Hex(0xc99870), U.Hex(0xe8c4a0), U.Hex(0xb88a62) };
        Color[] hair = { U.Hex(0x4a3320), U.Hex(0x2a2a2a), U.Hex(0x8a5a2a), U.Hex(0x1e1e1e) };
        int[] hatK = { 7, 4, 5, 6 };
        Color[] hatC = { U.Hex(0x6b4a2b), U.Hex(0x2a6a5a), U.Hex(0x9a9aa2), U.Hex(0x6a2a6a) };
        Color[] hatC2 = { U.Hex(0x5a3a22), U.Hex(0x2a6a5a), U.Hex(0xc0392b), U.Hex(0xf0e8d0) };
        Color wood = new Color(.43f, .3f, .18f);
        for (int i = 0; i < 4; i++)
        {
            var g = new GameObject("npc" + i).transform; g.SetParent(npcRoot, false); npcG[i] = g;
            g.position = new Vector3(IX + .9f + i * 1.5f, 0, IZ + 3.9f); NpcPos[i] = g.position; Color n = NPCC[i];
            Castle.House(g, new Vector3(0, 0, 1.2f), 1.25f, 1.0f, .9f, .6f, windowMats, i % 2 == 0, .3f);
            Kit.Part(g, Kit.BoxC(1f, .5f, .36f), U.Mat(wood, .08f), new Vector3(0, .25f, .55f));
            Kit.Part(g, Kit.BoxC(1.12f, .06f, .62f), U.Mat(n, .08f), new Vector3(0, 1.15f, .6f));
            foreach (float x in new[] { -.52f, .52f }) Kit.Part(g, Kit.FrC(6, .03f, .03f, 1.1f), U.Mat(wood, .08f), new Vector3(x, 0, .85f));
            var st = new Chibi.Style { skin = skin[i], hair = hair[i], top = Color.Lerp(n, Color.white, .15f), sleeve = n, bloom = n * .8f, legs = new Color(.3f, .22f, .16f), boot = U.Hex(0x3a2a1a), collar = U.Hex(0xf1e9d0), hatC = hatC[i], hatC2 = hatC2[i], hat = hatK[i] };
            var ch = Chibi.Build(g, st, false); ch.root.localScale = Vector3.one * .85f; npcC[i] = ch;
            if (i == 0) { Kit.Part(ch.handR, Kit.FrC(6, .015f, .015f, .5f), U.Mat(wood, .08f), new Vector3(0, -.2f, 0)); Kit.Sc(Kit.Part(ch.handR, Kit.BoxC(1, 1, 1), U.Mat(new Color(.5f, .5f, .52f), .3f), new Vector3(0, .3f, 0)), .16f, .09f, .09f); }
            if (i == 1) { Kit.Part(ch.handR, Kit.FrC(10, .08f, .065f, .12f), U.Mat(new Color(.55f, .58f, .6f), .3f), new Vector3(0, -.12f, 0)); Kit.Part(ch.handR, Kit.FrC(5, .015f, .01f, .12f), U.Mat(new Color(.55f, .58f, .6f), .3f), new Vector3(.05f, -.05f, .02f), new Vector3(0, 0, -50)); }
            if (i == 2) { Kit.Part(ch.handR, Kit.FrC(6, .012f, .012f, 1.3f), U.Mat(wood, .08f), new Vector3(0, -.4f, 0)); Kit.Part(ch.handR, Kit.FrC(5, .04f, 0f, .15f, false), U.Mat(new Color(.8f, .8f, .82f), .4f), new Vector3(0, .9f, 0)); Kit.Part(ch.armL, Kit.FrC(14, .17f, .17f, .035f, false), U.Mat(new Color(.55f, .23f, .17f), .08f), new Vector3(-.1f, -.45f, .12f), new Vector3(90, 0, 0)); }
            if (i == 3) { Kit.Sc(Kit.Part(ch.handR, Kit.Cache("ls1", () => Kit.LowSphere(1)), U.Mat(new Color(.72f, .6f, .38f), .08f), new Vector3(0, -.12f, 0)), .12f, .13f, .12f); }
            g.gameObject.SetActive(false);
        }
    }
    public int NpcAt(float x, float z)
    {
        if (!S.npc) return -1;
        for (int i = 0; i < 4; i++) if (Vector2.Distance(new Vector2(NpcPos[i].x, NpcPos[i].z), new Vector2(x, z)) < .9f) return i;
        return -1;
    }
    public static string NpcName(int i) { return NPCN[i]; }

    // =============== petani ===============
    void BuildFarmer()
    {
        var F = new GameObject("Farmer").transform; Farmer = F;
        var st = new Chibi.Style { skin = U.Hex(0xe0b58f), hair = U.Hex(0x8a6a3a), top = U.Hex(0xe8dcb5), sleeve = U.Hex(0x7a5a38), bloom = U.Hex(0x7a5a38), legs = U.Hex(0xc9b88a), boot = U.Hex(0x4a3320), collar = U.Hex(0xf1e9d0), hatC = U.Hex(0x5a8a48), hatC2 = U.Hex(0x3a5a2e), hat = 0 };
        farmerC = Chibi.Build(F, st, true); torso = farmerC.torso; mouth = farmerC.mouth; beard = farmerC.beard; flower = farmerC.flower; glasses = farmerC.glasses;
        Kit.Part(farmerC.handR, Kit.FrC(6, .015f, .015f, .85f), U.Mat(new Color(.43f, .3f, .18f), .08f), new Vector3(0, -.3f, 0));
        Kit.Sc(Kit.Part(farmerC.handR, Kit.BoxC(1, 1, 1), U.Mat(new Color(.6f, .6f, .62f), .3f), new Vector3(0, .56f, .08f)), .16f, .03f, .1f);
        F.localScale = Vector3.one * .85f; F.position = new Vector3(IX + 3, 0, IZ + 3); FarmerTarget = F.position;
    }
    static readonly Color[] TOPC = { U.Hex(0xe8dcb5), U.Hex(0x4f9a4a), U.Hex(0xff8a3d), U.Hex(0x8a8f96) };
    static readonly Color[] SLVC = { U.Hex(0x7a5a38), U.Hex(0x7fbf6a), U.Hex(0xffb36a), U.Hex(0x555a62) };
    static readonly Color[] BLMC = { U.Hex(0x7a5a38), U.Hex(0xc9b88a), U.Hex(0x4a9fb0), U.Hex(0xaecbe0) };
    static readonly Color[] LEGC = { U.Hex(0xc9b88a), U.Hex(0xc9b88a), U.Hex(0xe0b58f), U.Hex(0xaecbe0) };
    void Dress() { DressSeason(S.se); }
    void DressSeason(int s)
    {
        dressed = s; farmerC.SetCloth(TOPC[s], SLVC[s], BLMC[s], LEGC[s]); farmerC.SetHat(s);
        farmerC.bloomT.localScale = new Vector3(1, s == 2 ? .7f : 1f, 1); farmerC.bloomT.localPosition = new Vector3(0, s == 2 ? .43f : .33f, 0);
        farmerC.torso.localScale = new Vector3(s == 3 ? 1.35f : 1f, 1f, s == 3 ? 1.35f : 1f);
        beard.gameObject.SetActive(s == 3); flower.gameObject.SetActive(s == 1); glasses.gameObject.SetActive(s == 2);
    }

    // =============== hewan / awan ===============
    void BuildAnimals()
    {
        for (int n = 0; n < 8; n++)
        {
            var sv = new SheepV(); var g = new GameObject("sheep").transform; g.SetParent(animalRoot, false); sv.root = g;
            Transform hd; Transform[] lg; SheepKit.Build(g, n % 4 == 3, out hd, out lg); sv.head = hd; sv.legs = lg;
            g.localScale = Vector3.one * (.9f + Random.value * .25f);
            g.position = new Vector3(14 + Random.value * 14, 0, 2 + Random.value * 14); sv.tgt = g.position; sv.wait = Random.value * 3; sheep.Add(sv);
        }
    }

    void BuildClouds()
    {
        var wm = U.Mat(Color.white, 0f);
        for (int c = 0; c < 8; c++)
        {
            var g = new GameObject("cloud").transform; g.SetParent(dynRoot, false); g.position = new Vector3(Random.value * N, 55f + Random.value * 10f, Random.value * N - 10f);
            for (int j = 0; j < 4; j++) { var s = U.Ball(g, new Vector3(j * 6f - 9f, Random.value * 2f, Random.value * 4f - 2f), new Vector3(10f + Random.value * 6f, 4f, 7f), Color.white); s.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off; }
            cloudsHigh.Add(g);
        }
        for (int c = 0; c < 5; c++)
        {
            var g = new GameObject("cloudShadow").transform; g.SetParent(dynRoot, false); g.position = new Vector3(Random.value * N, 22f, Random.value * N);
            for (int j = 0; j < 3; j++) { var s = U.Ball(g, new Vector3(j * 2f - 2f, Random.value * .4f, Random.value - .5f), new Vector3(3.6f, 2f, 3f), Color.white); var r = s.GetComponent<Renderer>(); r.shadowCastingMode = ShadowCastingMode.ShadowsOnly; }
            cloudShadow.Add(g);
        }
    }

    void BuildParticles()
    {
        var g = new GameObject("FX"); ps = g.AddComponent<ParticleSystem>();
        var main = ps.main; main.playOnAwake = false; main.maxParticles = 3000; main.simulationSpace = ParticleSystemSimulationSpace.World; main.startLifetime = 1f; main.startSpeed = 0f;
        var em = ps.emission; em.enabled = false; var sh = ps.shape; sh.enabled = false;
        var r = g.GetComponent<ParticleSystemRenderer>(); var m = new Material(Shader.Find("Sprites/Default")); m.mainTexture = U.Radial(32, Color.white, 1f, 0f, 1.4f); r.material = m; r.renderMode = ParticleSystemRenderMode.Billboard;
        ps.Play();
    }

    void Emit(string k, float x, float z)
    {
        var ep = new ParticleSystem.EmitParams(); ep.position = new Vector3(x + Random.Range(-.3f, .3f), .3f, z + Random.Range(-.3f, .3f));
        switch (k)
        {
            case "sparkle": ep.velocity = new Vector3(Random.Range(-.3f, .3f), 1f, Random.Range(-.3f, .3f)); ep.startColor = new Color(1, .95f, .63f); ep.startSize = .22f; ep.startLifetime = .9f; break;
            case "dust": ep.velocity = new Vector3(Random.Range(-.4f, .4f), .6f, Random.Range(-.4f, .4f)); ep.startColor = new Color(.78f, .72f, .6f, .8f); ep.startSize = .16f; ep.startLifetime = .5f; break;
            case "rain": ep.position = new Vector3(x, 2.2f, z); ep.velocity = new Vector3(0, -5f, 0); ep.startColor = new Color(.5f, .75f, 1f); ep.startSize = .08f; ep.startLifetime = .4f; break;
            case "ember": ep.velocity = new Vector3(Random.Range(-.5f, .5f), 1.3f, Random.Range(-.5f, .5f)); ep.startColor = new Color(1, .7f, .28f); ep.startSize = .1f; ep.startLifetime = 1.1f; break;
            case "smoke": ep.velocity = new Vector3(Random.Range(-.2f, .2f), 1f, Random.Range(-.2f, .2f)); ep.startColor = new Color(.3f, .3f, .3f, .5f); ep.startSize = .5f; ep.startLifetime = 2f; ep.position = new Vector3(x, 1f, z); break;
            case "leaf": ep.position = new Vector3(x, 8f, z); ep.velocity = new Vector3(Random.Range(-.3f, .3f), -1f, Random.Range(-.3f, .3f)); ep.startColor = S.se == 3 ? Color.white : S.se == 1 ? new Color(1, .7f, .8f) : S.se == 2 ? new Color(.85f, .5f, .17f) : new Color(.85f, .8f, .6f); ep.startSize = .14f; ep.startLifetime = 8f; break;
            case "fly": ep.position = new Vector3(x, .5f + Random.value * 1.5f, z); ep.velocity = new Vector3(Random.Range(-.1f, .1f), .05f, Random.Range(-.1f, .1f)); ep.startColor = new Color(1, .94f, .63f); ep.startSize = .16f; ep.startLifetime = 4f; break;
        }
        ps.Emit(ep, 1);
    }

    void OnSeason()
    {
        int se = S.se;
        for (int i = 0; i < N * N; i++) px[i] = TileColor(i);
        texDirty = true;
        TreeKit.Recolor(se, wetOn);
        Decor.Recolor(GRASS[se], SKYC[se]);
        bigM.color = GRASS[se];
        if (S.wl.Count > 0 && walls.Count == 0) BuildWallViews();
    }
    void OnSpawn(Intr o)
    {
        var g = new GameObject("intruder").transform; g.SetParent(dynRoot, false); o.go = g.gameObject;
        Color[] top = { U.Hex(0x8a3b2b), U.Hex(0x4f6a3a), U.Hex(0x3a3440), U.Hex(0x4a5d7a) };
        var st = new Chibi.Style { skin = U.Hex(0xd9a982), hair = U.Hex(0x3a2a1a), top = top[o.ty], sleeve = top[o.ty] * .7f, bloom = top[o.ty] * .6f, legs = U.Hex(0x3a2c26), boot = U.Hex(0x241a14), collar = U.Hex(0x6b5a48), hatC = HAT[o.ty], hatC2 = U.Hex(0x3a2a1a), hat = o.ty == 0 ? 8 : o.ty == 1 ? 7 : o.ty == 2 ? 4 : 3 };
        var ch = Chibi.Build(g, st, false); ch.root.localScale = Vector3.one * .8f; ic[o] = ch;
        Color wood = new Color(.43f, .3f, .18f);
        if (o.ty == 0) { Kit.Part(ch.handR, Kit.FrC(6, .02f, .02f, .55f), U.Mat(wood, .08f), new Vector3(0, -.2f, 0)); Kit.Part(ch.handR, Kit.FrC(6, .08f, 0f, .24f, false), U.Mat(new Color(1f, .48f, .1f), .1f), new Vector3(0, .35f, 0)); }
        else if (o.ty == 1) { Kit.Part(ch.handR, Kit.FrC(6, .02f, .02f, .7f), U.Mat(wood, .08f), new Vector3(0, -.3f, 0)); Kit.Sc(Kit.Part(ch.handR, Kit.BoxC(1, 1, 1), U.Mat(new Color(.7f, .7f, .72f), .4f), new Vector3(0, .3f, .06f)), .04f, .16f, .2f); }
        else if (o.ty == 2) { Kit.Sc(Kit.Part(ch.handR, Kit.Cache("ls1", () => Kit.LowSphere(1)), U.Mat(new Color(.72f, .6f, .38f), .08f), new Vector3(0, -.2f, 0)), .2f, .22f, .2f); }
        else { Kit.Part(ch.armL, Kit.FrC(14, .2f, .2f, .04f, false), U.Mat(new Color(.55f, .23f, .17f), .08f), new Vector3(-.1f, -.45f, .14f), new Vector3(90, 0, 0)); Kit.Part(ch.handR, Kit.FrC(6, .02f, .02f, .8f), U.Mat(wood, .08f), new Vector3(0, -.3f, 0)); Kit.Sc(Kit.Part(ch.handR, Kit.BoxC(1, 1, 1), U.Mat(new Color(.7f, .7f, .72f), .4f), new Vector3(0, .35f, 0)), .22f, .12f, .04f); }
        var ring = new GameObject("ring"); ring.transform.SetParent(g, false); ring.transform.localPosition = new Vector3(0, .05f, 0);
        var lr = ring.AddComponent<LineRenderer>(); lr.useWorldSpace = false; lr.loop = true; lr.positionCount = 24; lr.widthMultiplier = .05f; lr.sharedMaterial = RingMat();
        for (int i = 0; i < 24; i++) { float a = i / 24f * 6.283f; lr.SetPosition(i, new Vector3(Mathf.Cos(a) * .5f, 0, Mathf.Sin(a) * .5f)); }
        ring.SetActive(false);
    }

    // =============== per-frame ===============
    void Update()
    {
        if (S == null || !ready) return;
        float dt = Time.deltaTime, time = Time.time;
        if (dressed != S.se) Dress();
        if (S.wl.Count > 0 && walls.Count != S.wl.Count) { BuildWallViews(); for (int j = 0; j < S.wl.Count; j++) OnWall(j); }
        // tile yang berubah
        if (dirty.Count > 0) { foreach (var i in dirty) Refresh(i); dirty.Clear(); }
        {
            bool wetNow = S.wet > 0;
            if (wetNow != wetOn) { wetOn = wetNow; TreeKit.Recolor(S.se, wetOn); }
            foreach (var kv in tv)
            {
                var v = kv.Value; if (!v.root.activeSelf) continue;
                if (v.pop > 0f) { v.pop = Mathf.Max(0f, v.pop * Mathf.Pow(.93f, dt * 60f) - .0005f); LayoutTree(kv.Key, v); }
                else if (time >= v.nextT) { v.nextT = time + .3f + (kv.Key % 7) * .03f; var T = S.tl[kv.Key]; if (T.s >= 2 && T.s < 5) LayoutTree(kv.Key, v); }
            }
            UpdateFire(time);
        }
        if (texDirty) { gt.SetPixels(px); gt.Apply(false); texDirty = false; }
        foreach (var kv in fences) { if (!kv.Value[1].gameObject.activeSelf) continue; var T = S.tl[kv.Key]; float ga = Mathf.Clamp01((S.clock - T.bt) / 40f); kv.Value[1].localScale = new Vector3(.2f + .35f * ga, .12f + .18f * ga, .2f + .35f * ga) * 1.0f; }
        // api: asap & bara
        foreach (int i in S.burning) { if (Random.value < .03f) Emit("ember", i % N + .5f, i / N + .5f); if (Random.value < .02f) Emit("smoke", i % N + .5f, i / N + .5f); }
        // NPC
        for (int i = 0; i < 4; i++) { if (npcG[i].gameObject.activeSelf != S.npc) npcG[i].gameObject.SetActive(S.npc); if (S.npc && npcC[i] != null) npcC[i].Animate(time * 1.5f + i, .12f); }
        // petani
        Vector3 d = FarmerTarget - Farmer.position; d.y = 0; float dd = d.magnitude; bool mv = dd > .12f;
        if (mv) { float st = Mathf.Min(dd, 5f * dt); Farmer.position += d / dd * st; Farmer.rotation = Quaternion.Euler(0, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 0); walk += dt * 10f; }
        Farmer.position = new Vector3(Farmer.position.x, mv ? Mathf.Abs(Mathf.Sin(walk)) * .07f : 0, Farmer.position.z);
        farmerC.Animate(walk, mv ? 1f : 0f);
        bool fire = S.burning.Count > 0; mouth.localScale = new Vector3(fire ? .04f : .12f, fire ? .08f : .02f, .02f);
        // intruder
        foreach (var o in S.ints)
        {
            if (!o.go) continue; var t = o.go.transform; t.position = new Vector3(o.x, o.mode == 1 ? 0 : Mathf.Abs(Mathf.Sin(time * 10 + o.x)) * .06f, o.z);
            if (o.tk >= 0 && o.mode != 2) { var tp = S.TargetPos(o); t.rotation = Quaternion.Euler(0, Mathf.Atan2(tp.x - o.x, tp.y - o.z) * Mathf.Rad2Deg, 0); }
            Chibi chb; if (ic.TryGetValue(o, out chb)) chb.Animate(time * 10f + o.x, o.mode == 1 ? 0f : 1f);
            var ring = t.Find("ring"); bool show = o.mode == 1 || o.h > 0; if (ring) { ring.gameObject.SetActive(show); if (show) { var lr = ring.GetComponent<LineRenderer>(); Color c = o.h > 0 ? Color.white : HAT[o.ty]; lr.startColor = lr.endColor = c; ring.localScale = Vector3.one * (o.h > 0 ? .4f + .8f * o.h / S.HoldTime : 1f + .15f * Mathf.Sin(time * 8f)); } }
        }
        UpdateAnimals(dt, time); UpdateEnv(dt, time);
        cursor.gameObject.SetActive(CursorOn); cursor.transform.position = CursorPos + Vector3.up * .05f; cursor.transform.localScale = Vector3.one * (S.brush == 1 ? 1.3f : S.brush == 2 ? 1.2f : .5f);
    }

    void UpdateAnimals(float dt, float time)
    {
        foreach (var sv in sheep)
        {
            if (sv.wait > 0) { sv.wait -= dt; sv.head.localRotation = Quaternion.Euler(30f + Mathf.Sin(time * 3f) * 6f, 0, 0); foreach (var l in sv.legs) l.localRotation = Quaternion.identity; }
            else
            {
                sv.head.localRotation = Quaternion.identity; Vector3 dv = sv.tgt - sv.root.position; dv.y = 0; float d = dv.magnitude;
                if (d < .2f) { sv.wait = 2 + Random.value * 5; sv.tgt = new Vector3(Random.value * N, 0, Random.value * N); if (sv.tgt.x < ZX + 10 && sv.tgt.z > ZZ - 2) sv.tgt = new Vector3(N - 3, 0, 3); }
                else { sv.root.position += dv / d * .7f * dt; sv.root.rotation = Quaternion.Euler(0, Mathf.Atan2(dv.x, dv.z) * Mathf.Rad2Deg, 0); sv.root.position = new Vector3(sv.root.position.x, Mathf.Abs(Mathf.Sin(time * 8)) * .03f, sv.root.position.z); for (int k = 0; k < 4; k++) sv.legs[k].localRotation = Quaternion.Euler(Mathf.Sin(time * 8f + (k % 3 == 0 ? 3.14f : 0f)) * 28f, 0, 0); }
            }
        }
        foreach (var c in cloudsHigh) { c.position += Vector3.right * .6f * dt; if (c.position.x > N + 60f) c.position = new Vector3(-60f, c.position.y, c.position.z); }
        foreach (var c in cloudShadow) { c.position += Vector3.right * .35f * dt; if (c.position.x > N + 12f) c.position = new Vector3(-12f, c.position.y, c.position.z); }
        for (int i = 0; i < flags.Count; i++) flags[i].localRotation = Quaternion.Euler(0, Mathf.Sin(time * 3f + i) * 20f, 0);
    }

    /// <summary>Langit/kabut/ambient hanya diperbarui 2x per detik: mengubah skybox tiap frame memaksa Unity menghitung ulang refleksi dan terasa patah-patah.</summary>
    void UpdateEnv(float dt, float time)
    {
        float t = S.clock; float hh = Mathf.Cos(t / 90f - .6f), sa = t / 90f - .6f;
        Dn = Mathf.Clamp(.5f + hh * .6f, .2f, 1f); Night = Mathf.Max(0, 1.2f - Dn * 1.4f);
        Vector3 sv = new Vector3(Mathf.Sin(sa) * .9f, hh * .95f + .05f, -.4f).normalized; sunDir = sv;
        envT -= dt;
        if (envT <= 0f)
        {
            envT = .5f;
            Vector3 lv = hh >= 0 ? sv : -sv; if (lv.y < .08f) lv.y = .08f;
            sun.transform.rotation = Quaternion.LookRotation(-lv);
            sun.intensity = hh >= 0 ? .35f + 1.0f * Mathf.Min(1f, hh * 1.7f) : .15f;
            sun.color = hh >= 0 ? Color.Lerp(U.Hex(0xff9a6a), Color.white, Mathf.Clamp01(hh * 2.4f)) : U.Hex(0x93aaff);
            Color sky = Color.Lerp(SKYC[S.se] * .15f, SKYC[S.se], Dn); sky.a = 1f;
            RenderSettings.ambientSkyColor = sky * .75f; RenderSettings.ambientEquatorColor = sky * .55f; RenderSettings.ambientGroundColor = sky * .3f;
            RenderSettings.fogColor = sky; RenderSettings.fogStartDistance = 60f; RenderSettings.fogEndDistance = 260f;
            if (skyM) { skyM.SetFloat("_AtmosphereThickness", Mathf.Lerp(.6f, 1.3f, Dn)); skyM.SetFloat("_Exposure", .3f + 1f * Dn); skyM.SetColor("_SkyTint", Color.Lerp(SKYC[S.se], U.Hex(0x808080), .4f)); skyM.SetFloat("_SunSize", hh > -.05f ? .06f : 0f); }
            foreach (var m in windowMats) U.SetGlow(m, U.Hex(0xffb040), Night * 2f);
            for (int i = 0; i < lanterns.Count; i++) { bool on = Night > .02f; if (lanterns[i].enabled != on) lanterns[i].enabled = on; lanterns[i].intensity = Night * 2.2f; U.SetGlow(lanternMats[i], U.Hex(0xffb040), .3f + Night * 2.5f); }
        }
        if (S.se != 0 && Random.value < dt * 8f) Emit("leaf", cam.transform.position.x + Random.Range(-14f, 14f), cam.transform.position.z + Random.Range(-14f, 14f) - 8f);
        if (Night > .3f && Random.value < dt * 2f) Emit("fly", Random.value * N, Random.value * N);
        if (Random.value < dt * 1.5f) Emit("smoke", IX + 1.8f, IZ + .8f);
        if (probe)
        {
            bool want = Gfx.Reflections; if (probe.enabled != want) probe.enabled = want;
            probeT -= dt; if (want && probeT <= 0) { probeT = 8f; probe.RenderProbe(); }
        }
    }

    public bool FarmerHit(Vector2 screen, float radius)
    {
        Vector3 p = cam.WorldToScreenPoint(Farmer.position + Vector3.up * 1.2f);
        if (p.z < 0) return false;
        return Vector2.Distance(new Vector2(p.x, p.y), screen) < radius;
    }
    public Vector2 SunScreen(out bool visible)
    {
        Vector3 p = cam.WorldToScreenPoint(cam.transform.position + sunDir * 100f); visible = p.z > 0 && sunDir.y > -.05f; return new Vector2(p.x, p.y);
    }
    public Camera Cam { get { return cam; } }
}
