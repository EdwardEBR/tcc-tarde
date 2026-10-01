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

    [Header("Interação")]
    public float distanciaInteracao = 3f;
    public LayerMask camadaInterativel;
    private InteractableObject objetoAtual;

    private CharacterController controller;
    private Vector3 velocidadeVertical;
    private float rotacaoCamera = 0f;

    private bool mouseLivre = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        TravarMouse();
    }

    void Update()
    {
        // Alterna entre soltar e prender o mouse com ESC
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            AlternarCursor();
        }

        // A gravidade e o chão continuam a funcionar mesmo se o mouse estiver livre, evitando cair pelo mapa
        AplicarGravidade();

        // Se o mouse estiver livre (mexendo no Inspetor), não anda nem mexe a câmara
        if (mouseLivre) return;

        Movimento();
        Camera();
        GerenciarInteracao();
    }

    void TravarMouse()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        mouseLivre = false;
    }

    void LiberarMouse()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        mouseLivre = true;

        if (objetoAtual != null)
        {
            objetoAtual.SetHighlight(false);
            objetoAtual = null;
        }
    }

    void AlternarCursor()
    {
        if (mouseLivre)
        {
            TravarMouse();
        }
        else
        {
            LiberarMouse();
        }
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
    }

    void AplicarGravidade()
    {
        // Mantém o player grudado no chão consistentemente
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

        transform.Rotate(
            Vector3.up * mouse.x * sensibilidade
        );

        rotacaoCamera -= mouse.y * sensibilidade;
        rotacaoCamera = Mathf.Clamp(rotacaoCamera, -80f, 80f);

        cameraPlayer.localRotation = Quaternion.Euler(rotacaoCamera, 0f, 0f);
    }

    void GerenciarInteracao()
    {
        if (cameraPlayer == null) return;

        Ray ray = new Ray(cameraPlayer.position, cameraPlayer.forward);
        RaycastHit hit;

        InteractableObject interativelDetectado = null;

        if (Physics.Raycast(ray, out hit, distanciaInteracao, camadaInterativel))
        {
            interativelDetectado = hit.collider.GetComponent<InteractableObject>();
        }

        if (objetoAtual != interativelDetectado)
        {
            if (objetoAtual != null)
            {
                objetoAtual.SetHighlight(false);
            }

            objetoAtual = interativelDetectado;

            if (objetoAtual != null)
            {
                objetoAtual.SetHighlight(true);
            }
        }

        if (objetoAtual != null)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                objetoAtual.Interact();
            }
        }
    }
}