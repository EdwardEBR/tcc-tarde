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
    public LayerMask camadaInterativel; // Defina uma Layer para os objetos interativos
    private InteractableObject objetoAtual;

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
        GerenciarInteracao();
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

    void GerenciarInteracao()
    {
        if (cameraPlayer == null) return;

        Ray ray = new Ray(cameraPlayer.position, cameraPlayer.forward);
        RaycastHit hit;

        // Limpa o destaque do objeto anterior se o jogador desviou o olhar
        if (objetoAtual != null)
        {
            objetoAtual.Highlight(false);
            objetoAtual = null;
        }

        // Dispara o raio a partir da câmera
        if (Physics.Raycast(ray, out hit, distanciaInteracao, camadaInterativel))
        {
            InteractableObject interativel = hit.collider.GetComponent<InteractableObject>();
            
            if (interativel != null)
            {
                objetoAtual = interativel;
                objetoAtual.Highlight(true); // Deixa laranja

                // Se o jogador clicar com o botão esquerdo do mouse usando o Input System novo
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    objetoAtual.Interact();
                }
            }
        }
    }
}