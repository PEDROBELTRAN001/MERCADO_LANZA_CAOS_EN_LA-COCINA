using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleInput : MonoBehaviour
{
    public static SimpleInput Instance;
    public Vector2 MoveInput { get; private set; }
    public bool InteractPressed { get; private set; }
    public bool SprintHeld { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        // Movimiento
        float x = 0f;
        float y = 0f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
        MoveInput = new Vector2(x, y);

        // Interactuar (solo en el frame que se presiona)
        InteractPressed = Keyboard.current.eKey.wasPressedThisFrame;

        // Correr (Shift)
        SprintHeld = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
    }
}
