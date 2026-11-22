#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Mengubah dunia statis yang biasanya dibuat lewat skrip saat Play menjadi objek Scene + asset permanen
/// (mesh, material, tekstur, prefab) sehingga tidak hilang setelah Play dihentikan dan bisa diedit di Scene view.
/// Menu: Tools > Seasons of the Grove > Buat Dunia Permanen (Bake).
/// </summary>
public static class WorldBaker
{
    const string Dir = "Assets/SeasonsOfTheGrove_Baked";
    const string RootName = "SeasonsWorld_Baked";

    [MenuItem("Tools/Seasons of the Grove/Buat Dunia Permanen (Bake)")]
    public static void Bake()
    {
        if (Application.isPlaying) { EditorUtility.DisplayDialog("Seasons of the Grove", "Hentikan Play dulu, lalu jalankan menu ini di mode Edit.", "OK"); return; }
        RemoveInternal();
        StaticReset.All();
        GameObject root = null;
        try
        {
            EditorUtility.DisplayProgressBar("Seasons of the Grove", "Membangun dunia (bisa beberapa detik)...", .15f);
            root = new GameObject(RootName); var bw = root.AddComponent<BakedWorld>();
            var dyn = new GameObject("Dyn").transform; dyn.SetParent(root.transform, false);
            var win = new List<Material>(); var lan = new List<Light>(); var lm = new List<Material>(); var fl = new List<Transform>();
            WorldGen.InitKits();
            WorldGen.Scenery(root.transform, true);
            WorldGen.Lake(root.transform);
            bw.castlePerch = WorldGen.Zone(root.transform, dyn, win, lan, lm, fl, true);
            var tmp = new GameObject("_bake_tmp"); var wcomp = tmp.AddComponent<World>(); wcomp.BakeInto(bw, root.transform, win); Object.DestroyImmediate(tmp);
            bw.windows = win.ToArray(); bw.lanterns = lan.ToArray(); bw.lanternMats = lm.ToArray(); bw.flags = fl.ToArray();
            bw.perches = Decor.Perches.ToArray(); bw.hill = Decor.HillMat; bw.far = Decor.FarMat;

            EditorUtility.DisplayProgressBar("Seasons of the Grove", "Menyimpan mesh, material, dan tekstur sebagai asset...", .55f);
            Persist(root);
            foreach (var nm in new[] { "Scenery", "Decor", "Lake", "Zone" })
            {
                var t = root.transform.Find(nm); if (!t) continue;
                foreach (var r in t.GetComponentsInChildren<Renderer>(true)) { if (r.transform.IsChildOf(dyn)) continue; GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic); }
            }
            EditorUtility.DisplayProgressBar("Seasons of the Grove", "Membuat prefab...", .9f);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, Dir + "/SeasonsWorld_Baked.prefab", InteractionMode.AutomatedAction);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(root.scene);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Grove] Bake gagal: " + e);
            if (root) Object.DestroyImmediate(root);
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Seasons of the Grove", "Bake gagal. Lihat Console untuk detailnya.", "OK");
            return;
        }
        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("Seasons of the Grove", "Dunia permanen dibuat di Scene (objek '" + RootName + "') dan sebagai prefab di " + Dir + ".\nSimpan Scene dengan Ctrl+S. Saat Play, game memakai dunia ini otomatis.", "OK");
    }

    [MenuItem("Tools/Seasons of the Grove/Hapus Dunia Permanen")]
    public static void Remove()
    {
        if (Application.isPlaying) return;
        RemoveInternal();
        EditorUtility.DisplayDialog("Seasons of the Grove", "Dunia permanen dihapus. Game akan membangun dunia lewat skrip lagi saat Play.", "OK");
    }

    static void RemoveInternal()
    {
        var old = GameObject.Find(RootName); if (old) Object.DestroyImmediate(old);
        if (AssetDatabase.IsValidFolder(Dir)) AssetDatabase.DeleteAsset(Dir);
    }

    static void Folder(string parent, string name) { if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name); }

    static void Persist(GameObject root)
    {
        Folder("Assets", "SeasonsOfTheGrove_Baked"); Folder(Dir, "Meshes"); Folder(Dir, "Materials"); Folder(Dir, "Textures");
        int n = 0; var seen = new HashSet<Object>();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var m = mf.sharedMesh; if (!m || AssetDatabase.Contains(m) || !seen.Add(m)) continue;
                m.name = "mesh_" + (n++); AssetDatabase.CreateAsset(m, Dir + "/Meshes/" + m.name + ".asset");
            }
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                foreach (var mt in r.sharedMaterials)
                {
                    if (!mt || AssetDatabase.Contains(mt) || !seen.Add(mt)) continue;
                    var tex = mt.mainTexture as Texture2D;
                    if (tex && !AssetDatabase.Contains(tex) && seen.Add(tex)) { tex.name = "tex_" + (n++); AssetDatabase.CreateAsset(tex, Dir + "/Textures/" + tex.name + ".asset"); }
                    mt.name = "mat_" + (n++); AssetDatabase.CreateAsset(mt, Dir + "/Materials/" + mt.name + ".mat");
                }
        }
        finally { AssetDatabase.StopAssetEditing(); }
    }
}
#endif
