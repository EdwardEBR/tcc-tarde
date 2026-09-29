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

        InteractableObject interativelDetectado = null;

        // Dispara o raio a partir da câmera dentro da distância permitida
        if (Physics.Raycast(ray, out hit, distanciaInteracao, camadaInterativel))
        {
            interativelDetectado = hit.collider.GetComponent<InteractableObject>();
        }

        // Se o objeto focado mudou
        if (objetoAtual != interativelDetectado)
        {
            // Desliga o contorno do objeto anterior (apenas se ele não estiver travado/concluído)
            if (objetoAtual != null)
            {
                objetoAtual.SetHighlight(false);
            }

            // Atualiza para o novo objeto
            objetoAtual = interativelDetectado;

            // Liga o contorno do novo objeto
            if (objetoAtual != null)
            {
                objetoAtual.SetHighlight(true);
            }
        }

        // Se estiver olhando para um objeto válido e clicar com o botão esquerdo
        if (objetoAtual != null)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                objetoAtual.Interact();
                // Removido o desligamento do highlight para continuar visível durante os testes
            }
        }
    }
}