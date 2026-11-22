using UnityEngine;

/// <summary>Kamera bebas: geser (WASD/tengah), putar+miring (kanan), zoom ke kursor (roda), ikuti petani (F / klik petani).</summary>
public class CamRig : MonoBehaviour
{
    public Camera cam;
    public Vector3 T = new Vector3(Sim.N / 2f, 0, Sim.N / 2f), Ts;
    public float D = 34f, Ds = 34f, Yaw, Ys, P = .9f, Ps = .9f;
    public bool follow;
    public Transform followT;
    Vector2 prev;
    static readonly Plane ground = new Plane(Vector3.up, Vector3.zero);

    void Awake() { Ts = T; }

    public bool Ground(Vector2 sp, out Vector3 p)
    {
        Ray r = cam.ScreenPointToRay(sp);
        float d;
        if (ground.Raycast(r, out d)) { p = r.GetPoint(d); return true; }
        p = Vector3.zero; return false;
    }

    public void Focus() { follow = true; D = Mathf.Min(D, 13f); P = Mathf.Max(P, .7f); }

    void PanBy(float dx, float dy)
    {
        float k = Ds * .0016f, fx = -Mathf.Sin(Ys), fz = -Mathf.Cos(Ys);
        T.x += fx * dy * k - fz * dx * k;
        T.z += fz * dy * k + fx * dx * k;
        follow = false;
    }

    /// <param name="menu">mode menu: kamera mengorbit pelan di atas peta</param>
    /// <param name="inputOn">false jika kursor di atas UI / ada modal</param>
    public void Tick(float dt, bool menu, bool inputOn)
    {
        Vector2 mp = In.MousePos;
        if (menu)
        {
            Yaw += dt * .06f; P = .45f; D = 44f; T = new Vector3(Sim.N / 2f, 0, Sim.N / 2f); follow = false;
        }
        else if (inputOn)
        {
            float fx = -Mathf.Sin(Ys), fz = -Mathf.Cos(Ys), mx = 0, mz = 0, ps = Ds * .9f * dt;
            if (In.Held(K.W) || In.Held(K.Up)) { mx += fx; mz += fz; }
            if (In.Held(K.S) || In.Held(K.Down)) { mx -= fx; mz -= fz; }
            if (In.Held(K.D) || In.Held(K.Right)) { mx += fz; mz -= fx; }
            if (In.Held(K.A) || In.Held(K.Left)) { mx -= fz; mz += fx; }
            if (mx != 0 || mz != 0) { follow = false; T.x += mx * ps; T.z += mz * ps; }
            if (In.Held(K.Q)) Yaw -= dt * 1.4f;
            if (In.Held(K.E)) Yaw += dt * 1.4f;

            Vector2 dlt = mp - prev;
            if (In.Btn(1)) { Yaw -= dlt.x * .006f; P = Mathf.Clamp(P - dlt.y * .005f, .15f, 1.45f); }
            if (In.Btn(2)) PanBy(dlt.x, -dlt.y);

            float sc = In.Scroll;
            if (Mathf.Abs(sc) > .01f)
            {
                Vector3 g; bool ok = Ground(mp, out g);
                float o = D; D = Mathf.Clamp(D * Mathf.Pow(.88f, sc), 4f, 90f);
                float k = 1f - D / o;
                if (ok) { T.x += (g.x - T.x) * k; T.z += (g.z - T.z) * k; follow = false; }
            }
        }
        prev = mp;
        if (follow && followT) { T.x += (followT.position.x - T.x) * Mathf.Min(1f, dt * 5f); T.z += (followT.position.z - T.z) * Mathf.Min(1f, dt * 5f); }
        T.x = Mathf.Clamp(T.x, -25f, Sim.N + 25f); T.z = Mathf.Clamp(T.z, -25f, Sim.N + 25f);

        float s = Mathf.Min(1f, dt * 8f);
        Ts = Vector3.Lerp(Ts, T, s); Ds += (D - Ds) * s; Ys += (Yaw - Ys) * s; Ps += (P - Ps) * s;
        float cp = Mathf.Cos(Ps);
        Vector3 pos = Ts + new Vector3(Mathf.Sin(Ys) * cp, Mathf.Sin(Ps), Mathf.Cos(Ys) * cp) * Ds;
        pos.y = Mathf.Max(pos.y, .8f);
        cam.transform.position = pos;
        cam.transform.LookAt(Ts);
    }
}
