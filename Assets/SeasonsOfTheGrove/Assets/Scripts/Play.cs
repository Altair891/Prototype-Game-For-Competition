using UnityEngine;

/// <summary>Menerjemahkan input mouse/keyboard menjadi aksi game dan menggerakkan kamera.</summary>
public class Play : MonoBehaviour
{
    public Sim S; public World W; public CamRig C; public GameUI UI; public Sfx A;
    Intr hold; bool brushing, captured;

    void Update()
    {
        if (S == null || W == null || C == null || !W.Ready) return;
        float dt = Time.deltaTime;
        if (UI.mode == Mode.Menu) { C.Tick(dt, true, false); W.CursorOn = false; return; }

        if (In.Pressed(K.Esc)) UI.OnEsc();
        Vector2 mp = In.MousePos;
        bool over = UI.Over(mp), modal = UI.Blocking;
        if (!modal)
        {
            if (In.Pressed(K.N1)) S.brush = 0; if (In.Pressed(K.N2)) S.brush = 1; if (In.Pressed(K.N3)) S.brush = 2; if (In.Pressed(K.N4)) S.brush = 3;
            if (In.Pressed(K.F)) C.Focus();
            if (In.Pressed(K.H)) UI.photo = !UI.photo;
        }
        C.followT = W.Farmer;
        C.Tick(dt, false, !over);

        Vector3 g = Vector3.zero; bool hasG = !modal && C.Ground(mp, out g);
        W.CursorOn = hasG && !over && !UI.photo; W.CursorPos = hasG ? g : Vector3.zero;

        if (modal || S.paused) { brushing = false; hold = null; captured = false; return; }

        if (In.BtnDown(0))
        {
            captured = over; brushing = false; hold = null;
            if (!over && hasG)
            {
                Intr best = null; float bd = .8f;
                foreach (var o in S.ints) { if (o.mode == 2) continue; float d = Vector2.Distance(new Vector2(o.x, o.z), new Vector2(g.x, g.z)); if (d < bd) { bd = d; best = o; } }
                if (best != null) { hold = best; best.h = 0; }
                else
                {
                    int n = W.NpcAt(g.x, g.z);
                    if (n >= 0) UI.shopNpc = n;
                    else if (!C.follow && W.FarmerHit(mp, 34f * Mathf.Max(.7f, Screen.height / 800f))) C.Focus();
                    else brushing = true;
                }
            }
        }
        if (In.Btn(0) && !captured && hasG)
        {
            if (hold != null)
            {
                if (hold.mode == 2 || Vector2.Distance(new Vector2(hold.x, hold.z), new Vector2(g.x, g.z)) > 1f) { hold.h = 0; hold = null; }
                else S.Hold(hold, dt);
            }
            else if (brushing)
            {
                S.Brush(g.x, g.z, dt); A.BrushTick(S.brush, dt);
                W.FarmerTarget = new Vector3(Mathf.Clamp(g.x, 0, Sim.N), 0, Mathf.Clamp(g.z + .9f, 0, Sim.N));
            }
        }
        else { if (hold != null) hold.h = 0; hold = null; brushing = false; if (!In.Btn(0)) captured = false; }
    }

}
