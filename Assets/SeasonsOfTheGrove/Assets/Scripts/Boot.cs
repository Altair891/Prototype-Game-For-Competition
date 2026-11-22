using UnityEngine;

/// <summary>Otomatis membangun seluruh game saat Play, jadi scene boleh kosong.</summary>
public static class Boot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Go()
    {
        if (Object.FindAnyObjectByType<Sim>() != null) return;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 1;
        var g = new GameObject("SeasonsOfTheGrove");
        var sim = g.AddComponent<Sim>();
        var sfx = g.AddComponent<Sfx>();
        var world = g.AddComponent<World>();
        var rig = g.AddComponent<CamRig>();
        var ui = g.AddComponent<GameUI>();
        var play = g.AddComponent<Play>();
        sim.paused = true;
        // World.Start membuat/menyiapkan kamera; hubungkan setelah satu frame.
        var cam = Camera.main;
        if (cam == null)
        {
            var cg = new GameObject("Main Camera"); cg.tag = "MainCamera"; cam = cg.AddComponent<Camera>(); cg.AddComponent<AudioListener>();
        }
        rig.cam = cam;
        ui.Setup(sim, world, rig, sfx);
        play.S = sim; play.W = world; play.C = rig; play.UI = ui; play.A = sfx;
    }
}
