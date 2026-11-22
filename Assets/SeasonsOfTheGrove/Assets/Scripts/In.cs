using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public enum K { W, A, S, D, Up, Down, Left, Right, Q, E, F, H, Esc, N1, N2, N3, N4 }

/// <summary>Pembungkus input: berjalan di Input Manager lama maupun Input System baru.</summary>
public static class In
{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    public static Vector2 MousePos { get { var m = Mouse.current; return m != null ? m.position.ReadValue() : Vector2.zero; } }
    public static bool Btn(int b)
    {
        var m = Mouse.current; if (m == null) return false;
        return b == 0 ? m.leftButton.isPressed : b == 1 ? m.rightButton.isPressed : m.middleButton.isPressed;
    }
    public static bool BtnDown(int b)
    {
        var m = Mouse.current; if (m == null) return false;
        return b == 0 ? m.leftButton.wasPressedThisFrame : b == 1 ? m.rightButton.wasPressedThisFrame : m.middleButton.wasPressedThisFrame;
    }
    public static float Scroll
    {
        get
        {
            var m = Mouse.current; if (m == null) return 0;
            float y = m.scroll.ReadValue().y;
            if (Mathf.Abs(y) >= 10f) y /= 120f;
            return Mathf.Clamp(y, -3f, 3f);
        }
    }
    static Key Map(K k)
    {
        switch (k)
        {
            case K.W: return Key.W; case K.A: return Key.A; case K.S: return Key.S; case K.D: return Key.D;
            case K.Up: return Key.UpArrow; case K.Down: return Key.DownArrow; case K.Left: return Key.LeftArrow; case K.Right: return Key.RightArrow;
            case K.Q: return Key.Q; case K.E: return Key.E; case K.F: return Key.F; case K.H: return Key.H; case K.Esc: return Key.Escape;
            case K.N1: return Key.Digit1; case K.N2: return Key.Digit2; case K.N3: return Key.Digit3; default: return Key.Digit4;
        }
    }
    public static bool Held(K k) { var kb = Keyboard.current; return kb != null && kb[Map(k)].isPressed; }
    public static bool Pressed(K k) { var kb = Keyboard.current; return kb != null && kb[Map(k)].wasPressedThisFrame; }
#else
    public static Vector2 MousePos { get { return Input.mousePosition; } }
    public static bool Btn(int b) { return Input.GetMouseButton(b); }
    public static bool BtnDown(int b) { return Input.GetMouseButtonDown(b); }
    public static float Scroll { get { return Input.mouseScrollDelta.y; } }
    static KeyCode Map(K k)
    {
        switch (k)
        {
            case K.W: return KeyCode.W; case K.A: return KeyCode.A; case K.S: return KeyCode.S; case K.D: return KeyCode.D;
            case K.Up: return KeyCode.UpArrow; case K.Down: return KeyCode.DownArrow; case K.Left: return KeyCode.LeftArrow; case K.Right: return KeyCode.RightArrow;
            case K.Q: return KeyCode.Q; case K.E: return KeyCode.E; case K.F: return KeyCode.F; case K.H: return KeyCode.H; case K.Esc: return KeyCode.Escape;
            case K.N1: return KeyCode.Alpha1; case K.N2: return KeyCode.Alpha2; case K.N3: return KeyCode.Alpha3; default: return KeyCode.Alpha4;
        }
    }
    public static bool Held(K k) { return Input.GetKey(Map(k)); }
    public static bool Pressed(K k) { return Input.GetKeyDown(Map(k)); }
#endif
}
