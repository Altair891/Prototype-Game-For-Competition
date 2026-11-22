using System.Collections.Generic;
using UnityEngine;

/// <summary>Pembangun bagian dunia yang statis (hutan latar, ornamen, danau, kastil zona). Dipakai saat Play maupun oleh menu Bake di Editor.</summary>
public static class WorldGen
{
    const int N = Sim.N, ZX = Sim.ZX, ZZ = Sim.ZZ, IX = ZX + 1, IZ = ZZ + 1;

    public static void InitKits() { TreeKit.Init(World.LEAF, World.SEAT); }

    public static void Scenery(Transform parent, bool bake)
    {
        TreeKit.Bake = bake; Decor.Bake = bake; Decor.Plan();
        var root = new GameObject("Scenery").transform; root.SetParent(parent, false);
        for (int j = 0; j < 230; j++)
        {
            float a = Random.value * 6.283f, r = N * .72f + Random.value * 34f, x = N / 2f + Mathf.Cos(a) * r, z = N / 2f + Mathf.Sin(a) * r;
            if (x > -2 && x < N + 2 && z > -2 && z < N + 2) continue;
            if (Mathf.Abs(x - (N / 2f + 8f)) < 29f && z < 0 && z > -27f) continue;
            if (Decor.Blocked(x, z)) continue;
            int sp = TreeKit.Pick(x * .5f, z * .5f, Random.value);
            TreeKit.Spawn(root, new Vector3(x, Decor.GroundY(x, z), z), Random.value * 360f, sp, TreeKit.Tint(sp, Random.value), .9f + Random.value * .9f, 5, false, true);
        }
        if (!bake) StaticBatchingUtility.Combine(root.gameObject);
        var d = new GameObject("Decor").transform; d.SetParent(parent, false); Decor.Build(d);
        TreeKit.Bake = false; Decor.Bake = false;
    }

    public static void Lake(Transform parent)
    {
        var root = new GameObject("Lake").transform; root.SetParent(parent, false); float cx = N / 2f + 8f, cz = -14f;
        U.Prim(PrimitiveType.Cylinder, root, new Vector3(cx, -.032f, cz), new Vector3(57f, .01f, 25.2f), U.Hex(0xc9b98a), .02f, false);
        var w = U.Prim(PrimitiveType.Cylinder, root, new Vector3(cx, -.012f, cz), new Vector3(52f, .01f, 22f), U.Hex(0x1d5f7a), .97f, false);
        w.name = "Water";
        for (int i = 0; i < 90; i++)
        {
            float a = Random.value * 6.283f, k = 1.02f + Random.value * .12f, x = cx + Mathf.Cos(a) * 26f * k, z = cz + Mathf.Sin(a) * 11f * k, h = .7f + Random.value * .8f;
            var rd = U.Cyl(root, new Vector3(x, h / 2f - .02f, z), .02f, h, U.Hex(0x5f8a3a)); rd.transform.rotation = Quaternion.Euler((Random.value - .5f) * 12f, 0, (Random.value - .5f) * 12f);
            if (Random.value < .4f) U.Cyl(root, new Vector3(x, h - .02f, z), .05f, .18f, U.Hex(0x6b4a2b));
        }
        for (int i = 0; i < 16; i++)
        {
            float a = Random.value * 6.283f, k = Random.value * .8f, x = cx + Mathf.Cos(a) * 26f * k, z = cz + Mathf.Sin(a) * 11f * k;
            U.Prim(PrimitiveType.Cylinder, root, new Vector3(x, -.004f, z), new Vector3(.56f, .005f, .56f), U.Hex(0x4f9a4a), .1f, false);
            if (Random.value < .4f) U.Ball(root, new Vector3(x, .04f, z), .14f, U.Hex(0xff9fc0));
        }
    }

    public static Vector3[] Zone(Transform parent, Transform dyn, List<Material> win, List<Light> lan, List<Material> lm, List<Transform> flags, bool bake)
    {
        var g = new GameObject("Zone").transform; g.SetParent(parent, false);
        Castle.Build(g, dyn, win, lan, lm, flags, ZX, ZZ, IX, IZ);
        if (!bake) StaticBatchingUtility.Combine(g.gameObject);
        return new[] { new Vector3(ZX + .5f, 2.95f, ZZ + .5f), new Vector3(ZX + 7.5f, 3.2f, ZZ + .5f), new Vector3(ZX + 7.5f, 2.9f, ZZ + 7.5f), new Vector3(ZX + 7.5f, 3.3f, ZZ + 4f), new Vector3(IX + 1.2f, 1.9f, IZ + 1.2f) };
    }
}
