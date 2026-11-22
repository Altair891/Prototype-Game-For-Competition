using UnityEngine;

/// <summary>
/// Mengosongkan semua cache statis (material, mesh, tekstur) setiap kali Play dimulai.
/// Tanpa ini, jika "Reload Domain" dimatikan di Enter Play Mode Options, Play kedua memakai material yang sudah dihancurkan
/// dan muncul MissingReferenceException / NullReferenceException.
/// </summary>
public static class StaticReset
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void OnLoad() { All(); }

    public static void All()
    {
        U.Reset(); Kit.Reset(); TreeKit.Reset(); BirdKit.Reset(); SheepKit.Reset(); Decor.Reset();
    }
}
