using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Tingkat kualitas grafis yang bekerja di Built-in maupun URP (URP lewat salinan asset agar asset asli tidak berubah).</summary>
public static class Gfx
{
    public static int Level = 1;          // 0 rendah, 1 sedang, 2 tinggi
    public static bool Reflections = true;
    static RenderPipelineAsset orig, copy;

    public static void Apply()
    {
        int[] sd = { 38, 62, 95 }, cas = { 2, 3, 4 }, res = { 1024, 2048, 4096 }, msaa = { 1, 2, 4 }; float[] rs = { .85f, 1f, 1f };
        var rp = GraphicsSettings.currentRenderPipeline;
        if (rp != null)
        {
            if (copy == null) { orig = rp; copy = Object.Instantiate(rp); }
            if (QualitySettings.renderPipeline != copy) QualitySettings.renderPipeline = copy;
            Set(copy, "shadowDistance", sd[Level]); Set(copy, "shadowCascadeCount", cas[Level]); Set(copy, "mainLightShadowmapResolution", res[Level]);
            Set(copy, "msaaSampleCount", msaa[Level]); Set(copy, "renderScale", rs[Level]);
        }
        else
        {
            QualitySettings.shadowDistance = sd[Level]; QualitySettings.shadowCascades = cas[Level] == 3 ? 2 : cas[Level]; QualitySettings.antiAliasing = msaa[Level] == 1 ? 0 : msaa[Level];
        }
    }

    static void Set(object o, string name, object v)
    {
        try { var p = o.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance); if (p != null && p.CanWrite) p.SetValue(o, System.Convert.ChangeType(v, p.PropertyType)); } catch { }
    }

    public static void Restore() { if (orig != null) QualitySettings.renderPipeline = orig; }
}
