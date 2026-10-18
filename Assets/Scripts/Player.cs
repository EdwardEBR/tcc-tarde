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
    private ItemSeguravel itemDestacadoAtual = null; // Item segurável que o jogador está a olhar

    [Header("Sistema de Inventário/Mão (Segurar Itens)")]
    public Transform maoDoJogador;          // Ponto de fixação do item, acompanhado pela câmera
    public Vector3 posicaoMaoNaCamera = new Vector3(0.3f, -0.25f, 0.6f);
    private ItemSeguravel itemNaMao = null; // Guarda o item que o player está a segurar no momento

    private CharacterController controller;
    private Vector3 velocidadeVertical;
    private float rotacaoCamera = 0f;

    private bool mouseLivre = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        ConfigurarPontoDaMao();
        TravarMouse();
    }

    void ConfigurarPontoDaMao()
    {
        if (cameraPlayer == null) return;

        if (maoDoJogador == null)
        {
            GameObject pontoDaMao = new GameObject("MaoDoJogador");
            maoDoJogador = pontoDaMao.transform;
        }

        // Mantém o suporte fora da hierarquia escalada do Player/câmera para evitar deformação.
        maoDoJogador.SetParent(null, false);
        maoDoJogador.localScale = Vector3.one;
        AtualizarPontoDaMao();
    }

    void LateUpdate()
    {
        AtualizarPontoDaMao();
    }

    void AtualizarPontoDaMao()
    {
        if (cameraPlayer == null || maoDoJogador == null) return;

        // Recalcula a posição relativa à câmera sem herdar a escala dos pais dela.
        maoDoJogador.position = cameraPlayer.position
            + cameraPlayer.right * posicaoMaoNaCamera.x
            + cameraPlayer.up * posicaoMaoNaCamera.y
            + cameraPlayer.forward * posicaoMaoNaCamera.z;
        maoDoJogador.rotation = cameraPlayer.rotation;
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

        if (itemDestacadoAtual != null)
        {
            itemDestacadoAtual.SetHighlight(false);
            itemDestacadoAtual = null;
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

        // Rotação horizontal do corpo do jogador
        transform.Rotate(
            Vector3.up * mouse.x * sensibilidade
        );

        // Rotação vertical da câmara
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
        ItemSeguravel itemSeguravelDetectado = null;

        if (Physics.Raycast(ray, out hit, distanciaInteracao, camadaInterativel))
        {
            interativelDetectado = hit.collider.GetComponent<InteractableObject>();

            // Só permite destacar e apanhar itens seguráveis quando a mão está livre.
            if (itemNaMao == null)
                itemSeguravelDetectado = hit.collider.GetComponent<ItemSeguravel>();
        }

        // Atualiza o destaque das interações normais.
        if (objetoAtual != interativelDetectado)
        {
            if (objetoAtual != null) objetoAtual.SetHighlight(false);
            objetoAtual = interativelDetectado;
            if (objetoAtual != null) objetoAtual.SetHighlight(true);
        }

        // Atualiza o destaque dos itens que podem ser apanhados.
        if (itemDestacadoAtual != itemSeguravelDetectado)
        {
            if (itemDestacadoAtual != null) itemDestacadoAtual.SetHighlight(false);
            itemDestacadoAtual = itemSeguravelDetectado;
            if (itemDestacadoAtual != null) itemDestacadoAtual.SetHighlight(true);
        }

        // Executa as ações do clique.
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Devolve o item ao slot da cafeteira.
            if (itemNaMao != null && hit.collider != null && hit.collider.CompareTag("SlotCafeteira"))
            {
                Transform slotDestino = hit.collider.transform;
                itemNaMao.SoltarItem(slotDestino);
                itemNaMao = null;
                Debug.Log("Xícara devolvida à origem!");
                return;
            }

            // Apanha um item segurável, como a xícara sobre a TV.
            if (itemNaMao == null && itemSeguravelDetectado != null)
            {
                itemDestacadoAtual.SetHighlight(false);
                itemDestacadoAtual = null;
                itemNaMao = itemSeguravelDetectado;
                itemNaMao.PegarItem(maoDoJogador);
                return;
            }

            // Mantém as interações normais, como portas e tarefas.
            if (objetoAtual != null)
            {
                objetoAtual.Interact();
            }
        }
    }
}
