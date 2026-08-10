using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade = 3f;
    public float gravidade = -20f;

    [Header("Camera")]
    public Transform cameraPlayer;
    public float sensibilidade = 0.15f;

    private CharacterController controller;
    private Vector3 velocidadeVertical;
    private float rotacaoCamera = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        Movimento();
        Camera();
    }

    void Movimento()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed)
                input.y += 1;

            if (Keyboard.current.sKey.isPressed)
                input.y -= 1;

            if (Keyboard.current.dKey.isPressed)
                input.x += 1;

            if (Keyboard.current.aKey.isPressed)
                input.x -= 1;
        }

        Vector3 movimento =
            transform.right * input.x +
            transform.forward * input.y;

        movimento.Normalize();

        controller.Move(movimento * velocidade * Time.deltaTime);

        // Gravidade
        if (controller.isGrounded)
        {
            velocidadeVertical.y = -2f;
        }
        else
        {
            velocidadeVertical.y += gravidade * Time.deltaTime;
        }

        controller.Move(velocidadeVertical * Time.deltaTime);
    }

    void Camera()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouse = Mouse.current.delta.ReadValue();

        // Olhar para os lados
        transform.Rotate(
            Vector3.up * mouse.x * sensibilidade
        );

        // Olhar para cima/baixo
        rotacaoCamera -= mouse.y * sensibilidade;

        rotacaoCamera = Mathf.Clamp(
            rotacaoCamera,
            -80f,
            80f
        );

        cameraPlayer.localRotation =
            Quaternion.Euler(rotacaoCamera, 0f, 0f);
    }
}