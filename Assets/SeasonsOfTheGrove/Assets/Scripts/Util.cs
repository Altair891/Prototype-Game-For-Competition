using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Helper statis: material, primitif, mesh kerucut, tekstur kecil.</summary>
public static class U
{
    static Shader sh;
    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
    static readonly Dictionary<int, Mesh> cones = new Dictionary<int, Mesh>();

    public static void Reset() { sh = null; cache.Clear(); cones.Clear(); }

    public static Shader Sh()
    {
        if (sh) return sh;
        bool srp = GraphicsSettings.currentRenderPipeline != null;
        sh = Shader.Find(srp ? "Universal Render Pipeline/Lit" : "Standard");
        if (!sh) sh = Shader.Find("Standard");
        if (!sh) sh = Shader.Find("Sprites/Default");
        return sh;
    }

    public static Color Hex(int rgb)
    {
        return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
    }

    static Material Make(Color c, float smooth)
    {
        var m = new Material(Sh());
        m.color = c;
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        m.enableInstancing = true;
        return m;
    }

    /// <summary>Material bersama (di-cache per warna) supaya batching/instancing efektif.</summary>
    public static Material Mat(Color c, float smooth = 0.12f)
    {
        Color32 k = c;
        string key = k.r + "," + k.g + "," + k.b + "," + (int)(smooth * 100);
        Material m;
        if (!cache.TryGetValue(key, out m) || m == null) { m = Make(c, smooth); cache[key] = m; }
        return m;
    }

    /// <summary>Material unik dengan emisi (untuk jendela/lentera yang menyala malam hari).</summary>
    public static Material Glow(Color c)
    {
        var m = Make(c, .2f);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        m.SetColor("_EmissionColor", Color.black);
        return m;
    }

    public static Material TexMat(Texture2D t, Color tint, float smooth = .05f)
    {
        var m = Make(tint, smooth); m.mainTexture = t; return m;
    }

    public static void SetGlow(Material m, Color c, float k)
    {
        if (m) m.SetColor("_EmissionColor", c * k);
    }

    public static GameObject Prim(PrimitiveType t, Transform p, Vector3 pos, Vector3 sc, Color c, float smooth = 0.12f, bool shadow = true)
    {
        var g = GameObject.CreatePrimitive(t);
        var col = g.GetComponent<Collider>();
        if (col) { if (Application.isPlaying) Object.Destroy(col); else Object.DestroyImmediate(col); }
        g.transform.SetParent(p, false);
        g.transform.localPosition = pos;
        g.transform.localScale = sc;
        var r = g.GetComponent<Renderer>();
        r.sharedMaterial = Mat(c, smooth);
        r.shadowCastingMode = shadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
        return g;
    }

    public static GameObject Cube(Transform p, Vector3 pos, Vector3 size, Color c) { return Prim(PrimitiveType.Cube, p, pos, size, c); }
    public static GameObject Ball(Transform p, Vector3 pos, Vector3 size, Color c) { return Prim(PrimitiveType.Sphere, p, pos, size, c); }
    public static GameObject Ball(Transform p, Vector3 pos, float diameter, Color c) { return Prim(PrimitiveType.Sphere, p, pos, Vector3.one * diameter, c); }
    /// <summary>Silinder dengan radius r dan tinggi h (pusat di pos).</summary>
    public static GameObject Cyl(Transform p, Vector3 pos, float r, float h, Color c) { return Prim(PrimitiveType.Cylinder, p, pos, new Vector3(r * 2, h / 2, r * 2), c); }

    /// <summary>Kerucut dengan alas di pos, radius r, tinggi h.</summary>
    public static GameObject Cone(Transform p, Vector3 pos, float r, float h, Color c, int sides = 8)
    {
        var g = new GameObject("cone");
        g.AddComponent<MeshFilter>().sharedMesh = ConeMesh(sides);
        var mr = g.AddComponent<MeshRenderer>();
        mr.sharedMaterial = Mat(c);
        g.transform.SetParent(p, false);
        g.transform.localPosition = pos;
        g.transform.localScale = new Vector3(r * 2, h, r * 2);
        return g;
    }

    public static Mesh ConeMesh(int sides)
    {
        Mesh m;
        if (cones.TryGetValue(sides, out m) && m != null) return m;
        var v = new List<Vector3>();
        var t = new List<int>();
        var apex = new Vector3(0, 1, 0);
        for (int i = 0; i < sides; i++)
        {
            float a0 = i * 2 * Mathf.PI / sides, a1 = (i + 1) * 2 * Mathf.PI / sides;
            var p0 = new Vector3(Mathf.Cos(a0) * .5f, 0, Mathf.Sin(a0) * .5f);
            var p1 = new Vector3(Mathf.Cos(a1) * .5f, 0, Mathf.Sin(a1) * .5f);
            int b = v.Count;
            v.Add(apex); v.Add(p1); v.Add(p0);
            t.Add(b); t.Add(b + 1); t.Add(b + 2);
            int c = v.Count;
            v.Add(Vector3.zero); v.Add(p0); v.Add(p1);
            t.Add(c); t.Add(c + 1); t.Add(c + 2);
        }
        m = new Mesh();
        m.SetVertices(v);
        m.SetTriangles(t, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        cones[sides] = m;
        return m;
    }

    public static Texture2D Solid(Color c)
    {
        var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        t.SetPixel(0, 0, c); t.Apply();
        t.hideFlags = HideFlags.HideAndDontSave;
        return t;
    }

    /// <summary>Gradien radial (alpha memudar ke tepi). inner/outer = alpha di tengah/tepi.</summary>
    public static Texture2D Radial(int n, Color col, float inner, float outer, float power = 1f)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + .5f) / n * 2 - 1, dy = (y + .5f) / n * 2 - 1;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float a = Mathf.Lerp(inner, outer, Mathf.Pow(d, power));
                t.SetPixel(x, y, new Color(col.r, col.g, col.b, a));
            }
        t.Apply();
        t.hideFlags = HideFlags.HideAndDontSave;
        return t;
    }
}
