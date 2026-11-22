using System.Collections.Generic;
using UnityEngine;

/// <summary>Model burung kecil bergaya gambar 2 (chickadee): topi hitam, pipi putih, punggung abu, perut krem, sayap berbulu, ekor panjang.</summary>
public static class BirdKit
{
    static Mesh body, wingR, wingL; static int[] bodyMap, wingMap;
    static readonly Material[][] bodyMats = new Material[3][], wingMats = new Material[3][];
    static bool built;
    public static void Reset() { built = false; body = null; wingR = null; wingL = null; bodyMap = null; wingMap = null; }
    // varian: 0 chickadee, 1 burung pipit cokelat, 2 pipit gelap
    static readonly Color[] GREY = { new Color(.55f, .55f, .57f), new Color(.55f, .4f, .27f), new Color(.3f, .3f, .33f) };
    static readonly Color[] BUFF = { new Color(.86f, .68f, .48f), new Color(.78f, .62f, .45f), new Color(.62f, .5f, .38f) };
    static readonly Color[] CAP = { new Color(.07f, .07f, .08f), new Color(.38f, .24f, .16f), new Color(.05f, .05f, .06f) };

    static void Add(List<Kit.PartSpec> l, Mesh m, Vector3 p, Vector3 s, int g, Vector3 e) { l.Add(new Kit.PartSpec { m = m, x = Matrix4x4.TRS(p, Quaternion.Euler(e), s), g = g }); }

    public static void Ensure()
    {
        if (built) return; built = true;
        var sp = Kit.Sphere(1, true); var bx = Kit.Cache("cube1", () => Kit.Box(1, 1, 1, 1f)); var cone = Kit.Cache("bkc", () => Kit.Frustum(5, .014f, 0f, .045f, 1f, false)); var leg = Kit.Cache("bkl", () => Kit.Frustum(4, .005f, .005f, .06f, 1f, false));
        var l = new List<Kit.PartSpec>();
        Add(l, sp, Vector3.zero, new Vector3(.07f, .065f, .14f), 0, Vector3.zero);                       // badan (punggung abu)
        Add(l, sp, new Vector3(0, .025f, .125f), new Vector3(.055f, .05f, .055f), 0, Vector3.zero);      // kepala
        Add(l, sp, new Vector3(0, -.028f, .015f), new Vector3(.063f, .05f, .12f), 1, Vector3.zero);     // perut krem
        Add(l, sp, new Vector3(0, .048f, .125f), new Vector3(.058f, .04f, .058f), 2, Vector3.zero);     // topi hitam
        Add(l, sp, new Vector3(0, -.012f, .175f), new Vector3(.03f, .025f, .03f), 2, Vector3.zero);     // dagu hitam
        for (int s = -1; s <= 1; s += 2)
        {
            Add(l, sp, new Vector3(s * .036f, .018f, .15f), new Vector3(.028f, .03f, .035f), 3, Vector3.zero);   // pipi putih
            Add(l, sp, new Vector3(s * .04f, .038f, .158f), Vector3.one * .01f, 4, Vector3.zero);               // mata
            Add(l, leg, new Vector3(s * .02f, -.09f, .01f), Vector3.one, 4, Vector3.zero);                       // kaki
        }
        Add(l, cone, new Vector3(0, .025f, .188f), Vector3.one, 4, new Vector3(90, 0, 0));                  // paruh
        for (int k = -1; k <= 1; k++) Add(l, bx, new Vector3(k * .018f, 0, -.21f), new Vector3(.03f, .006f, .16f), 0, new Vector3(0, k * 9f, 0)); // ekor
        body = Kit.Merge(l, 5, out bodyMap);
        wingR = BuildWing(1f, out wingMap); int[] m2; wingL = BuildWing(-1f, out m2);
        for (int v = 0; v < 3; v++)
        {
            Color[] pal = { GREY[v], BUFF[v], CAP[v], new Color(.95f, .95f, .93f), new Color(.12f, .1f, .1f) };
            bodyMats[v] = new Material[bodyMap.Length]; for (int k = 0; k < bodyMap.Length; k++) bodyMats[v][k] = U.Mat(pal[bodyMap[k]], .08f);
            wingMats[v] = new Material[wingMap.Length]; for (int k = 0; k < wingMap.Length; k++) wingMats[v][k] = U.Mat(k == 0 ? GREY[v] * .92f : new Color(.2f, .19f, .2f), .08f);
        }
    }

    static Mesh BuildWing(float s, out int[] map)
    {
        var l = new List<Kit.PartSpec>(); var bx = Kit.Cache("cube1", () => Kit.Box(1, 1, 1, 1f));
        Add(l, bx, new Vector3(s * .1f, 0, .02f), new Vector3(.17f, .012f, .05f), 0, Vector3.zero);
        Add(l, bx, new Vector3(s * .12f, .004f, -.025f), new Vector3(.13f, .01f, .05f), 0, Vector3.zero);
        for (int k = 0; k < 6; k++)
        {
            float len = .22f - k * .016f, th = 5f + k * 4.5f, rad = th * Mathf.Deg2Rad;
            Add(l, bx, new Vector3(s * (.05f + len / 2f * Mathf.Cos(rad)), 0, -.022f * k - len / 2f * Mathf.Sin(rad)), new Vector3(len, .005f, .03f), k >= 4 ? 4 : 0, new Vector3(0, s * th, 0));
        }
        return Kit.Merge(l, 5, out map);
    }

    public static Material[] BodyMats(int v) { Ensure(); return bodyMats[v]; }
    public static Material[] WingMats(int v) { Ensure(); return wingMats[v]; }
    public static Mesh Body { get { Ensure(); return body; } }
    public static Mesh WingR { get { Ensure(); return wingR; } }
    public static Mesh WingL { get { Ensure(); return wingL; } }
}

/// <summary>
/// Perilaku burung: kawanan kecil duduk di pohon/menara/atap/tanah, lalu lepas landas menuju tujuan tertentu.
/// Kadang mereka pergi jauh dan menghilang (di luar peta), lalu kembali beberapa saat kemudian sebagai hiasan. Tidak berputar-putar di atas pemain.
/// </summary>
public class Aviary : MonoBehaviour
{
    const float FOOT = .2f;
    World W;
    class Bird
    {
        public Transform root, body, wR, wL; public Vector3 pos, vel, spot; public float ox, oy, oz, phase, fold = 1f, timer, seed, hop, peck; public bool gliding;
    }
    class Flock
    {
        public List<Bird> b = new List<Bird>(); public int state; public float timer, heading, speed, alt, turn, takeT, seed; public Vector3 pos, dest; public bool destIsPerch; public int variant;
    }
    readonly List<Flock> flocks = new List<Flock>();

    public void Init(World w)
    {
        W = w; BirdKit.Ensure(); int[] sizes = { 3, 2, 3 };
        for (int i = 0; i < sizes.Length; i++)
        {
            var f = new Flock { variant = i % 3, seed = Random.value * 10f };
            for (int k = 0; k < sizes[i]; k++) f.b.Add(MakeBird(f.variant, k));
            flocks.Add(f);
            if (i == 2) { f.state = 4; f.timer = Random.Range(10f, 30f); foreach (var b in f.b) b.root.gameObject.SetActive(false); }
            else SetPerch(f, W.RandomPerch(new Vector3(15, 0, 15)));
        }
    }

    Bird MakeBird(int variant, int idx)
    {
        var b = new Bird(); var g = new GameObject("bird").transform; g.SetParent(transform, false); g.localScale = Vector3.one * 2f; b.root = g; b.seed = Random.value * 10f;
        var bd = new GameObject("body"); bd.transform.SetParent(g, false); bd.AddComponent<MeshFilter>().sharedMesh = BirdKit.Body; var mr = bd.AddComponent<MeshRenderer>(); mr.sharedMaterials = BirdKit.BodyMats(variant); b.body = bd.transform;
        b.wR = Wing(g, BirdKit.WingR, variant, 1f); b.wL = Wing(g, BirdKit.WingL, variant, -1f);
        float side = idx % 2 == 0 ? 1f : -1f; b.ox = side * (1.1f + idx * .5f); b.oz = -.9f * (idx + 1); b.oy = Random.Range(-.3f, .5f);
        return b;
    }
    static Transform Wing(Transform parent, Mesh m, int variant, float s)
    {
        var p = new GameObject("wing").transform; p.SetParent(parent, false); p.localPosition = new Vector3(s * .05f, .02f, 0);
        var g = new GameObject("m"); g.transform.SetParent(p, false); g.AddComponent<MeshFilter>().sharedMesh = m; g.AddComponent<MeshRenderer>().sharedMaterials = BirdKit.WingMats(variant); return p;
    }

    static Vector3 FarPoint(float a, float b)
    {
        float ang = Random.value * 6.2832f; return new Vector3(15 + Mathf.Cos(ang) * Random.Range(a, b), Random.Range(13f, 20f), 15 + Mathf.Sin(ang) * Random.Range(a, b));
    }

    void SetPerch(Flock f, Vector3 p)
    {
        f.state = 0; f.timer = Random.Range(10f, 28f); f.pos = p + Vector3.up * FOOT; f.speed = 0;
        foreach (var b in f.b)
        {
            Vector2 r = Random.insideUnitCircle * (p.y < .3f ? 1.1f : .3f); b.spot = p + new Vector3(r.x, FOOT, r.y); b.pos = b.spot; b.vel = Vector3.zero; b.fold = 1f; b.gliding = false;
            b.root.gameObject.SetActive(true); b.root.position = b.pos; b.root.rotation = Quaternion.Euler(0, Random.value * 360f, 0); b.timer = Random.Range(1f, 4f);
        }
    }

    void Depart(Flock f)
    {
        bool far = Random.value < .42f;
        if (far) { f.dest = FarPoint(95f, 120f); f.destIsPerch = false; f.alt = Random.Range(11f, 16f); }
        else { Vector3 p = W.RandomPerch(f.pos); f.dest = p; f.destIsPerch = true; f.alt = Mathf.Max(p.y + Random.Range(3.5f, 6f), 6.5f); }
        f.state = 1; f.takeT = 0; f.speed = 2.5f; f.heading = Mathf.Atan2(f.dest.x - f.pos.x, f.dest.z - f.pos.z);
        Vector3 c = Vector3.zero; foreach (var b in f.b) c += b.pos; f.pos = c / f.b.Count;
    }

    float Steer(Flock f, float dt, float turnRate)
    {
        float want = Mathf.Atan2(f.dest.x - f.pos.x, f.dest.z - f.pos.z) + Mathf.Sin(Time.time * .45f + f.seed) * .22f;
        float diff = Mathf.DeltaAngle(f.heading * Mathf.Rad2Deg, want * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        float dh = Mathf.Clamp(diff, -turnRate * dt, turnRate * dt); f.heading += dh; f.turn = Mathf.Lerp(f.turn, dh / Mathf.Max(dt, .001f), dt * 4f);
        f.pos += new Vector3(Mathf.Sin(f.heading), 0, Mathf.Cos(f.heading)) * f.speed * dt;
        return Vector2.Distance(new Vector2(f.pos.x, f.pos.z), new Vector2(f.dest.x, f.dest.z));
    }

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, .05f);
        foreach (var f in flocks) { Step(f, dt); Apply(f, dt); }
    }

    void Step(Flock f, float dt)
    {
        switch (f.state)
        {
            case 0: f.timer -= dt; if (f.timer <= 0) Depart(f); break;
            case 1:
                f.takeT += dt; f.speed = Mathf.MoveTowards(f.speed, 6f, 5f * dt); Steer(f, dt, .8f); f.pos.y = Mathf.MoveTowards(f.pos.y, f.alt, 4.2f * dt);
                if ((f.takeT > 1.6f && f.pos.y >= f.alt - .6f) || f.takeT > 3.5f) f.state = 2; break;
            case 2:
                f.speed = Mathf.MoveTowards(f.speed, 7f, 3f * dt); float d = Steer(f, dt, 1.3f);
                f.pos.y = Mathf.Lerp(f.pos.y, f.alt + Mathf.Sin(Time.time * .6f + f.seed) * .8f, dt * .8f);
                if (f.destIsPerch && d < 16f) f.state = 3;
                else if (!f.destIsPerch && d < 10f) { f.state = 4; f.timer = Random.Range(25f, 70f); foreach (var b in f.b) b.root.gameObject.SetActive(false); }
                break;
            case 3:
                float dl = Vector2.Distance(new Vector2(f.pos.x, f.pos.z), new Vector2(f.dest.x, f.dest.z));
                f.speed = Mathf.Lerp(1.6f, 6.5f, Mathf.Clamp01(dl / 12f)); Steer(f, dt, 2f);
                float ty = f.dest.y + FOOT + (f.alt - f.dest.y) * Mathf.Pow(Mathf.Clamp01(dl / 16f), 1.5f); f.pos.y = Mathf.Lerp(f.pos.y, ty, dt * 3f);
                if (dl < .6f && Mathf.Abs(f.pos.y - (f.dest.y + FOOT)) < .5f) SetPerch(f, f.dest);
                break;
            case 4:
                f.timer -= dt;
                if (f.timer <= 0)
                {
                    Vector3 p = W.RandomPerch(new Vector3(15, 0, 15)); f.dest = p; f.destIsPerch = true; f.pos = FarPoint(90f, 105f); f.alt = Mathf.Max(p.y + 4f, 8f); f.pos.y = f.alt;
                    f.heading = Mathf.Atan2(p.x - f.pos.x, p.z - f.pos.z); f.speed = 7f; f.state = 2;
                    foreach (var b in f.b) { b.pos = f.pos; b.vel = Vector3.zero; b.fold = 0f; b.root.position = b.pos; b.root.gameObject.SetActive(true); }
                }
                break;
        }
    }

    void Apply(Flock f, float dt)
    {
        if (f.state == 4) return;
        float t = Time.time; Vector3 fwd = new Vector3(Mathf.Sin(f.heading), 0, Mathf.Cos(f.heading)), right = new Vector3(Mathf.Cos(f.heading), 0, -Mathf.Sin(f.heading));
        float dist = f.state == 3 ? Vector2.Distance(new Vector2(f.pos.x, f.pos.z), new Vector2(f.dest.x, f.dest.z)) : 99f;
        for (int i = 0; i < f.b.Count; i++)
        {
            var b = f.b[i];
            if (f.state == 0) { Perched(b, i, t, dt); continue; }
            Vector3 tp = f.pos + right * b.ox + fwd * b.oz + Vector3.up * b.oy;
            if (f.state == 3 && dist < 4f) tp = Vector3.Lerp(b.spot, tp, Mathf.Clamp01(dist / 4f));
            Vector3 old = b.pos; b.pos = Vector3.Lerp(b.pos, tp, Mathf.Min(1f, dt * 3.5f));
            b.vel = Vector3.Lerp(b.vel, (b.pos - old) / Mathf.Max(dt, .001f), Mathf.Min(1f, dt * 6f));
            float flap; bool flapping = Flap(f, b, dt, out flap);
            b.fold = Mathf.MoveTowards(b.fold, 0f, dt * 5f);
            b.root.position = b.pos + Vector3.up * (flapping ? Mathf.Sin(b.phase) * .04f : 0f);
            if (b.vel.sqrMagnitude > .3f)
            {
                var look = Quaternion.LookRotation(b.vel.normalized) * Quaternion.Euler(0, 0, Mathf.Clamp(-f.turn * .35f, -.6f, .6f) * Mathf.Rad2Deg);
                b.root.rotation = Quaternion.Slerp(b.root.rotation, look, Mathf.Min(1f, dt * 8f));
            }
            b.body.localRotation = Quaternion.identity; Wings(b, flap);
        }
    }

    bool Flap(Flock f, Bird b, float dt, out float flap)
    {
        if (f.state == 2)
        {
            b.timer -= dt;
            if (b.gliding) { if (b.timer <= 0) { b.gliding = false; b.timer = Random.Range(1f, 1.8f); } }
            else if (b.timer <= 0) { b.gliding = true; b.timer = Random.Range(.5f, 1.1f); }
        }
        else b.gliding = false;
        if (b.gliding) { flap = 10f; return false; }
        b.phase += (f.state == 2 ? 20f : 27f) * dt; flap = Mathf.Sin(b.phase) * 50f; return true;
    }

    void Perched(Bird b, int i, float t, float dt)
    {
        b.pos = b.spot; b.fold = Mathf.MoveTowards(b.fold, 1f, dt * 4f); b.timer -= dt;
        if (b.timer <= 0) { b.timer = Random.Range(2f, 6f); if (Random.value < .5f) b.hop = .4f; else b.peck = .35f; }
        float y = 0, pitch = 0;
        if (b.hop > 0) { b.hop -= dt; y = Mathf.Sin((1f - b.hop / .4f) * Mathf.PI) * .25f; }
        if (b.peck > 0) { b.peck -= dt; pitch = Mathf.Sin((1f - b.peck / .35f) * Mathf.PI) * 30f; }
        b.root.position = b.pos + Vector3.up * y; b.body.localRotation = Quaternion.Euler(pitch, Mathf.Sin(t * .8f + b.seed) * 25f, 0); Wings(b, 0);
    }

    static void Wings(Bird b, float flap)
    {
        float k = 1f - b.fold;
        b.wR.localRotation = Quaternion.Euler(0, b.fold * 80f, flap * k); b.wL.localRotation = Quaternion.Euler(0, -b.fold * 80f, -flap * k);
    }
}
