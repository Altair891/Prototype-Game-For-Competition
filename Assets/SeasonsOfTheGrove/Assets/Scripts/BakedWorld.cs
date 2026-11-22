using UnityEngine;

[System.Serializable] public class WallRef { public Transform root, body; public Transform[] m; public bool alongX; }
[System.Serializable] public class SheepRef { public Transform root, head; public Transform[] legs; }

/// <summary>Penanda pohon di dunia permanen: saat Play, materialnya dipasang ulang agar warna daunnya mengikuti musim.</summary>
public class BakedTree : MonoBehaviour { public int species, tint; public bool low; }

/// <summary>
/// Dunia permanen (hasil menu Tools > Seasons of the Grove > Buat Dunia Permanen). Berisi referensi yang dibutuhkan game saat Play.
/// Semua mesh/material tersimpan sebagai asset sehingga objeknya tetap ada di Scene setelah Play dihentikan.
/// </summary>
public class BakedWorld : MonoBehaviour
{
    public Material[] windows, lanternMats; public Light[] lanterns; public Transform[] flags;
    public Vector3[] castlePerch, perches; public Material hill, far;
    // aktor dan benda bergerak yang ikut permanen
    public Transform farmer; public Chibi farmerChibi;
    public Transform[] npcGroups; public Chibi[] npcChibis; public Vector3[] npcPos;
    public SheepRef[] sheep; public Transform[] clouds, cloudShadows; public WallRef[] walls;
    public Renderer tileRenderer, groundRenderer;

    // Kembalikan warna dasar asset agar tidak berubah permanen oleh pergantian musim saat Play.
    void OnDisable()
    {
        if (hill) hill.color = new Color(.5f, .64f, .35f);
        if (far) far.color = new Color(.55f, .65f, .5f);
        if (windows != null) foreach (var m in windows) if (m) m.SetColor("_EmissionColor", Color.black);
        if (lanternMats != null) foreach (var m in lanternMats) if (m) m.SetColor("_EmissionColor", Color.black);
    }
}
