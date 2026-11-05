using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PlayerFPS : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade = 3f;
    public float gravidade = -20f;

    [Header("Camera")]
    public Transform cameraPlayer;
    public float sensibilidade = 0.15f;
    [Tooltip("Tempo de suavização do movimento do olhar. Valores menores respondem mais rápido.")]
    [Min(0f)] public float suavidadeOlhar = 0.08f;
    [Tooltip("Campo de visão da câmera enquanto o botão do meio do mouse estiver pressionado.")]
    [Range(1f, 179f)] public float campoVisaoZoom = 30f;
    [Tooltip("Velocidade de transição do zoom.")]
    public float velocidadeZoom = 10f;
    [Tooltip("Quanto baixar a câmera ao sentar.")]
    public float baixarCameraAoSentar = 0.65f;
    [Tooltip("Limite horizontal de olhar, para cada lado, enquanto sentado.")]
    [Range(10f, 120f)] public float limiteGiroHorizontalSentado = 90f;
    [Tooltip("Limite vertical de olhar, para cima e para baixo, enquanto sentado.")]
    [Range(10f, 80f)] public float limiteGiroVerticalSentado = 80f;
    private Camera cameraComponente;
    private float campoVisaoOriginal;
    private Vector3 posicaoLocalCameraOriginal;
    private Quaternion rotacaoLocalCameraOriginal;

    [Header("Interação")]
    public float distanciaInteracao = 3f;
    public LayerMask camadaInterativel;
    private InteractableObject objetoAtual;
    private CafeteiraInteractable cafeteiraAtual;
    private TelefoneCarregadorInteractable carregadorAtual;
    private CadeiraSentavelInteractable cadeiraAtual;
    private ItemSeguravel itemDestacadoAtual = null; // Item segurável que o jogador está a olhar
    private PortaItemSlot slotDestacadoAtual = null; // Slot azul que o jogador está a olhar
    private MecanicaColagem mecanicaColagemAtual;
    private Outline outlineEstanteLivro;

    [Header("Sons de passos")]
    public AudioClip somPasso1;
    public AudioClip somPasso2;
    public float intervaloPassos = 1.5f;
    [Tooltip("Pausa adicional após cada clipe antes de tocar o próximo passo.")]
    public float pausaEntrePassos = 0.6f;
    private AudioSource audioPassos;
    private float proximoPasso;
    private bool alternarPasso;
    private AudioSource somGeladeira;
    private AudioSource somAmbiente;

    [Header("Sistema de Inventário/Mão (Segurar Itens)")]
    public Transform maoDoJogador;          // Ponto de fixação do item, acompanhado pela câmera
    public Vector3 posicaoMaoNaCamera = new Vector3(0.3f, -0.25f, 0.6f);
    private ItemSeguravel itemNaMao = null; // Guarda o item que o player está a segurar no momento

    private CharacterController controller;
    private Rigidbody corpoRigido;
    private bool corpoRigidoUsavaGravidade;
    private bool corpoRigidoEraKinematic;
    private Vector3 velocidadeVertical;
    private float rotacaoCamera = 0f;
    private float rotacaoHorizontalAlvo;
    private float rotacaoHorizontalBaseSentado;
    private float velocidadeRotacaoHorizontal;
    private float velocidadeRotacaoVertical;

    private bool mouseLivre = false;
    private bool modoConversaAtivo = false;
    private bool descartarDeltaMouse;
    private bool sentandoNaCadeira;
    private bool sentadoNaCadeira;
    private Vector3 posicaoNormalAntesDeSentar;
    private Quaternion rotacaoNormalAntesDeSentar;

    public float MeiaAlturaMundialDoPersonagem => controller != null
        ? controller.bounds.extents.y
        : Mathf.Max(0.5f, transform.lossyScale.y);

    void Start()
    {
        controller = GetComponent<CharacterController>();
        corpoRigido = GetComponent<Rigidbody>();
        if (corpoRigido != null)
        {
            corpoRigidoUsavaGravidade = corpoRigido.useGravity;
            corpoRigidoEraKinematic = corpoRigido.isKinematic;
        }
        DesativarSombrasDoJogador();
        if (cameraPlayer != null)
        {
            posicaoLocalCameraOriginal = cameraPlayer.localPosition;
            rotacaoLocalCameraOriginal = cameraPlayer.localRotation;
            cameraComponente = cameraPlayer.GetComponent<Camera>();
            if (cameraComponente != null) campoVisaoOriginal = cameraComponente.fieldOfView;
            rotacaoCamera = cameraPlayer.localEulerAngles.x;
            if (rotacaoCamera > 180f) rotacaoCamera -= 360f;
        }
        rotacaoHorizontalAlvo = transform.eulerAngles.y;
        // Fontes independentes: os passos não podem substituir a faixa ambiente.
        audioPassos = gameObject.AddComponent<AudioSource>();
        audioPassos.spatialBlend = 0f;
        audioPassos.playOnAwake = false;
        if (somPasso1 == null) somPasso1 = Resources.Load<AudioClip>("Sons/AudioClip/Snd-Passo1");
        if (somPasso2 == null) somPasso2 = Resources.Load<AudioClip>("Sons/AudioClip/Snd-Passo2");
        ConfigurarSomAmbiente();
        ConfigurarSomGeladeira();
        ConfigurarPontoDaMao();
        TravarMouse();
    }

    private void DesativarSombrasDoJogador()
    {
        // Evita que o cilindro/corpo do jogador projete sombra nas luzes da cena.
        Renderer[] renderizadores = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderizador in renderizadores)
            renderizador.shadowCastingMode = ShadowCastingMode.Off;
    }

    private void ConfigurarSomAmbiente()
    {
        somAmbiente = gameObject.AddComponent<AudioSource>();
        somAmbiente.playOnAwake = false;
        somAmbiente.spatialBlend = 0f;
        somAmbiente.loop = true;
        somAmbiente.volume = 0.5f;
        somAmbiente.clip = SomJogo.Carregar("Snd-Ambiente");
        if (somAmbiente.clip != null) somAmbiente.Play();
        else Debug.LogWarning("PlayerFPS: não foi possível carregar Snd-Ambiente em Resources/Sons/AudioClip.", this);
    }

    private void ConfigurarSomGeladeira()
    {
        GameObject porta = GameObject.Find("PortaGeladeira");
        if (porta == null) return;
        somGeladeira = SomJogo.CriarFonte(porta.transform);
        somGeladeira.clip = SomJogo.Carregar("Snd-Geladeira");
        somGeladeira.loop = true;
        somGeladeira.minDistance = 1f;
        somGeladeira.maxDistance = 8f;
        if (somGeladeira.clip != null) somGeladeira.Play();
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
            if (!modoConversaAtivo) AlternarCursor();
        }

        if (sentandoNaCadeira) return;

        // A gravidade e o chão continuam a funcionar mesmo se o mouse estiver livre, evitando cair pelo mapa
        if (!sentadoNaCadeira) AplicarGravidade();

        // Se o mouse estiver livre (mexendo no Inspetor), não anda nem mexe a câmara
        if (mouseLivre) return;

        if (!sentadoNaCadeira) Movimento();
        Camera();
        if (!modoConversaAtivo) GerenciarInteracao();
    }

    private void OnApplicationFocus(bool ganhouFoco)
    {
        if (!ganhouFoco || mouseLivre || modoConversaAtivo) return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        descartarDeltaMouse = true;
    }

    void TravarMouse()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        mouseLivre = false;
        descartarDeltaMouse = true;
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

        if (cafeteiraAtual != null)
        {
            cafeteiraAtual.SetHighlight(false);
            cafeteiraAtual = null;
        }

        if (itemDestacadoAtual != null)
        {
            itemDestacadoAtual.SetHighlight(false);
            itemDestacadoAtual = null;
        }

        if (slotDestacadoAtual != null)
        {
            slotDestacadoAtual.SetHighlight(false);
            slotDestacadoAtual = null;
        }

        if (carregadorAtual != null)
        {
            carregadorAtual.SetHighlight(false);
            carregadorAtual = null;
        }

        if (cadeiraAtual != null)
        {
            cadeiraAtual.SetHighlight(false);
            cadeiraAtual = null;
        }

        if (mecanicaColagemAtual != null)
        {
            mecanicaColagemAtual.SetHighlightArvore(false);
            mecanicaColagemAtual = null;
        }
    }

    public void DefinirModoConversa(bool ativo)
    {
        modoConversaAtivo = ativo;
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
        if (sentadoNaCadeira) return;

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

        bool andando = input.sqrMagnitude > 0.01f && controller.isGrounded;
        if (!andando) { proximoPasso = 0f; return; }
        if (Time.time >= proximoPasso)
        {
            if (audioPassos.isPlaying)
            {
                proximoPasso = Time.time + 0.1f;
                return;
            }

            AudioClip passo = alternarPasso ? somPasso2 : somPasso1;
            if (passo != null)
            {
                // Aguarda o clipe atual terminar e acrescenta uma pausa maior,
                // mantendo uma cadência de caminhada mais lenta.
                audioPassos.clip = passo;
                audioPassos.Play();
                float duracaoMaisLonga = Mathf.Max(
                    passo.length,
                    somPasso1 != null ? somPasso1.length : 0f,
                    somPasso2 != null ? somPasso2.length : 0f);
                proximoPasso = Time.time + Mathf.Max(intervaloPassos, duracaoMaisLonga + pausaEntrePassos);
            }
            else
            {
                proximoPasso = Time.time + intervaloPassos;
            }
            alternarPasso = !alternarPasso;
        }
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
        if (Mouse.current == null || cameraPlayer == null) return;

        AtualizarZoom();

        // Depois de voltar o foco para o jogo, o Input System pode entregar
        // um delta acumulado que deixa a mira em um estado inesperado.
        if (descartarDeltaMouse)
        {
            descartarDeltaMouse = false;
            return;
        }

        Vector2 mouse = Mouse.current.delta.ReadValue();
        if (!ValorFinito(mouse.x) || !ValorFinito(mouse.y))
            return;

        // Um delta extremo pode ocorrer quando o cursor recupera o foco; evita
        // que esse valor contamine a rotação da câmera.
        mouse = Vector2.ClampMagnitude(mouse, 100f);

        rotacaoHorizontalAlvo += mouse.x * sensibilidade;
        if (sentadoNaCadeira)
        {
            float deslocamentoHorizontal = Mathf.DeltaAngle(rotacaoHorizontalBaseSentado, rotacaoHorizontalAlvo);
            rotacaoHorizontalAlvo = rotacaoHorizontalBaseSentado
                + Mathf.Clamp(deslocamentoHorizontal, -limiteGiroHorizontalSentado, limiteGiroHorizontalSentado);
        }

        // Rotação vertical da câmara
        float novaRotacaoCamera = rotacaoCamera - mouse.y * sensibilidade;
        if (!ValorFinito(novaRotacaoCamera)) return;
        float limiteVertical = sentadoNaCadeira ? limiteGiroVerticalSentado : 80f;
        rotacaoCamera = Mathf.Clamp(novaRotacaoCamera, -limiteVertical, limiteVertical);

        float suavidade = Mathf.Max(0f, suavidadeOlhar);
        float rotacaoHorizontalSuave = suavidade == 0f
            ? rotacaoHorizontalAlvo
            : Mathf.SmoothDampAngle(transform.eulerAngles.y, rotacaoHorizontalAlvo,
                ref velocidadeRotacaoHorizontal, suavidade);
        if (sentadoNaCadeira)
        {
            float deslocamentoSuave = Mathf.DeltaAngle(rotacaoHorizontalBaseSentado, rotacaoHorizontalSuave);
            rotacaoHorizontalSuave = rotacaoHorizontalBaseSentado
                + Mathf.Clamp(deslocamentoSuave, -limiteGiroHorizontalSentado, limiteGiroHorizontalSentado);
        }
        float rotacaoVerticalSuave = suavidade == 0f
            ? rotacaoCamera
            : Mathf.SmoothDampAngle(cameraPlayer.localEulerAngles.x > 180f
                    ? cameraPlayer.localEulerAngles.x - 360f
                    : cameraPlayer.localEulerAngles.x,
                rotacaoCamera, ref velocidadeRotacaoVertical, suavidade);

        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, rotacaoHorizontalSuave, transform.eulerAngles.z);
        Quaternion rotacao = Quaternion.Euler(rotacaoVerticalSuave, 0f, 0f);
        if (ValorFinito(rotacao.x) && ValorFinito(rotacao.y)
            && ValorFinito(rotacao.z) && ValorFinito(rotacao.w))
            cameraPlayer.localRotation = rotacao;
    }

    private static bool ValorFinito(float valor)
    {
        return !float.IsNaN(valor) && !float.IsInfinity(valor);
    }

    public void SentarNaCadeira(Vector3 posicaoFinal, Quaternion rotacaoFinal)
    {
        if (sentadoNaCadeira || sentandoNaCadeira) return;
        posicaoNormalAntesDeSentar = transform.position;
        rotacaoNormalAntesDeSentar = transform.rotation;
        if (audioPassos != null) audioPassos.Stop();
        StartCoroutine(AproximarEChegarNaCadeira(posicaoFinal, rotacaoFinal));
    }

    private IEnumerator AproximarEChegarNaCadeira(Vector3 posicaoFinal, Quaternion rotacaoFinal)
    {
        sentandoNaCadeira = true;
        if (controller == null) controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        if (corpoRigido == null) corpoRigido = GetComponent<Rigidbody>();
        if (corpoRigido != null)
        {
            corpoRigido.linearVelocity = Vector3.zero;
            corpoRigido.angularVelocity = Vector3.zero;
            corpoRigido.useGravity = false;
            corpoRigido.isKinematic = true;
        }

        Vector3 posicaoInicial = transform.position;
        Quaternion rotacaoInicial = transform.rotation;
        Vector3 posicaoCameraInicial = cameraPlayer != null
            ? cameraPlayer.localPosition
            : Vector3.zero;
        Vector3 posicaoCameraFinal = posicaoCameraInicial;
        Quaternion rotacaoCameraInicial = cameraPlayer != null
            ? cameraPlayer.localRotation
            : Quaternion.identity;
        float rotacaoCameraFinalGraus = Mathf.Clamp(
            rotacaoCamera, -limiteGiroVerticalSentado, limiteGiroVerticalSentado);
        Quaternion rotacaoCameraFinal = Quaternion.Euler(rotacaoCameraFinalGraus, 0f, 0f);
        if (cameraPlayer != null)
            posicaoCameraFinal.y = posicaoLocalCameraOriginal.y - Mathf.Abs(baixarCameraAoSentar);

        const float duracao = 0.8f;
        float tempo = 0f;

        while (tempo < duracao)
        {
            tempo += Time.unscaledDeltaTime;
            float progresso = Mathf.Clamp01(tempo / duracao);
            float suavizado = progresso * progresso * (3f - 2f * progresso);
            transform.position = Vector3.Lerp(posicaoInicial, posicaoFinal, suavizado);
            transform.rotation = Quaternion.Slerp(rotacaoInicial, rotacaoFinal, suavizado);
            if (cameraPlayer != null)
            {
                cameraPlayer.localPosition = Vector3.Lerp(posicaoCameraInicial, posicaoCameraFinal, suavizado);
                cameraPlayer.localRotation = Quaternion.Slerp(rotacaoCameraInicial, rotacaoCameraFinal, suavizado);
            }
            yield return null;
        }

        transform.SetPositionAndRotation(posicaoFinal, rotacaoFinal);
        rotacaoHorizontalBaseSentado = rotacaoFinal.eulerAngles.y;
        rotacaoHorizontalAlvo = rotacaoHorizontalBaseSentado;
        velocidadeRotacaoHorizontal = 0f;
        velocidadeRotacaoVertical = 0f;
        if (cameraPlayer != null)
        {
            rotacaoCamera = rotacaoCameraFinalGraus;
            cameraPlayer.localPosition = posicaoCameraFinal;
            cameraPlayer.localRotation = rotacaoCameraFinal;
        }

        // O CharacterController e o Rigidbody ficam desligados enquanto sentado,
        // para gravidade e colisões não deslocarem o jogador da cadeira.
        velocidadeVertical = Vector3.zero;
        sentadoNaCadeira = true;
        sentandoNaCadeira = false;

        TaskManager gerenciadorTarefas = FindAnyObjectByType<TaskManager>();
        if (gerenciadorTarefas != null)
            gerenciadorTarefas.IniciarDialogoAposSentar();
    }

    public void TeleportarPara(Vector3 posicao)
    {
        bool estavaSentado = sentadoNaCadeira || sentandoNaCadeira;
        Vector3 destino = posicao;
        Quaternion rotacaoDestino = transform.rotation;
        if (estavaSentado)
        {
            destino.y = posicaoNormalAntesDeSentar.y;
            rotacaoDestino = rotacaoNormalAntesDeSentar;
        }

        if (controller == null) controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        if (corpoRigido == null) corpoRigido = GetComponent<Rigidbody>();
        if (corpoRigido != null) corpoRigido.isKinematic = true;

        transform.SetPositionAndRotation(destino, rotacaoDestino);
        if (corpoRigido != null)
        {
            corpoRigido.position = destino;
            corpoRigido.rotation = rotacaoDestino;
            corpoRigido.useGravity = corpoRigidoUsavaGravidade;
            corpoRigido.isKinematic = corpoRigidoEraKinematic;
            corpoRigido.linearVelocity = Vector3.zero;
            corpoRigido.angularVelocity = Vector3.zero;
        }

        if (cameraPlayer != null)
        {
            cameraPlayer.localPosition = posicaoLocalCameraOriginal;
            cameraPlayer.localRotation = rotacaoLocalCameraOriginal;
            rotacaoCamera = cameraPlayer.localEulerAngles.x;
            if (rotacaoCamera > 180f) rotacaoCamera -= 360f;
        }

        velocidadeVertical = Vector3.zero;
        rotacaoHorizontalAlvo = rotacaoDestino.eulerAngles.y;
        velocidadeRotacaoHorizontal = 0f;
        velocidadeRotacaoVertical = 0f;
        sentadoNaCadeira = false;
        sentandoNaCadeira = false;
        modoConversaAtivo = false;
        Physics.SyncTransforms();
        if (controller != null) controller.enabled = true;
        descartarDeltaMouse = true;
    }

    void AtualizarZoom()
    {
        if (cameraComponente == null || campoVisaoOriginal <= 0f) return;

        float campoVisaoAlvo = Mouse.current.middleButton.isPressed
            ? Mathf.Clamp(campoVisaoZoom, 1f, campoVisaoOriginal)
            : campoVisaoOriginal;
        cameraComponente.fieldOfView = Mathf.Lerp(
            cameraComponente.fieldOfView,
            campoVisaoAlvo,
            velocidadeZoom * Time.deltaTime);
    }

    void GerenciarInteracao()
    {
        if (cameraPlayer == null) return;

        Ray ray = new Ray(cameraPlayer.position, cameraPlayer.forward);
        RaycastHit hit;

        InteractableObject interativelDetectado = null;
        CafeteiraInteractable cafeteiraDetectada = null;
        TelefoneCarregadorInteractable carregadorDetectado = null;
        CadeiraSentavelInteractable cadeiraDetectada = null;
        MecanicaColagem mecanicaColagemDetectada = null;
        ItemSeguravel itemSeguravelDetectado = null;
        PortaItemSlot slotDetectado = null;

        // A representação da xícara usa colliders trigger para não empurrar o jogador.
        // Colide com triggers para que ela continue clicável mesmo quando o jogador olha para ela.
        if (Physics.Raycast(ray, out hit, distanciaInteracao, camadaInterativel, QueryTriggerInteraction.Collide))
        {
            cafeteiraDetectada = hit.collider.GetComponentInParent<CafeteiraInteractable>();
            carregadorDetectado = hit.collider.GetComponentInParent<TelefoneCarregadorInteractable>();
            cadeiraDetectada = hit.collider.GetComponentInParent<CadeiraSentavelInteractable>();
            MecanicaColagem mecanicaColagem = hit.collider.GetComponentInParent<MecanicaColagem>();
            if (mecanicaColagem != null && mecanicaColagem.EhAlvoDaArvore(hit.collider.transform))
                mecanicaColagemDetectada = mecanicaColagem;
            // Se o collider do telefone estiver à frente do carregador, ainda trata o
            // clique como interação com o carregador mesmo quando são objetos separados.
            if (carregadorDetectado == null)
            {
                ItemSeguravel itemMirado = hit.collider.GetComponentInParent<ItemSeguravel>();
                if (itemMirado != null)
                {
                    TelefoneCarregadorInteractable[] carregadores = FindObjectsByType<TelefoneCarregadorInteractable>(FindObjectsSortMode.None);
                    foreach (TelefoneCarregadorInteractable carregador in carregadores)
                    {
                        if (carregador.telefone == itemMirado)
                        {
                            carregadorDetectado = carregador;
                            break;
                        }
                    }
                }
            }
            if (cafeteiraDetectada == null)
            {
                interativelDetectado = hit.collider.GetComponentInParent<InteractableObject>();
            }

            PortaItemSlot slot = hit.collider.GetComponentInParent<PortaItemSlot>();
            if (slot == null)
            {
                slot = PortaItemSlot.EncontrarSlotDaRepresentacao(hit.collider.transform);
            }

            // Com a mão vazia, permite pegar itens soltos ou retirar o item de um slot ocupado.
            if (itemNaMao == null)
            {
                itemSeguravelDetectado = hit.collider.GetComponentInParent<ItemSeguravel>();

                // O telefone só pode ser retirado pelo carregador depois das tarefas.
                TelefoneCarregadorInteractable carregadorDoItem = itemSeguravelDetectado != null
                    ? itemSeguravelDetectado.GetComponentInParent<TelefoneCarregadorInteractable>()
                    : null;
                if (carregadorDoItem != null && !carregadorDoItem.PodePegarTelefone)
                    itemSeguravelDetectado = null;

                // Se a xícara estiver no local alternativo da mecânica de troca,
                // ainda retire-a pelo slot para transferir o café junto com ela.
                if (slot == null && itemSeguravelDetectado != null)
                {
                    PortaItemSlot slotDeOrigem = itemSeguravelDetectado.slotDeOrigem;
                    // CanecaDerrubada mantém o vínculo com o PortaItem, mas é um item
                    // solto. Não tratar o clique nela como retirada do item antigo do slot.
                    if (slotDeOrigem != null
                        && (!slotDeOrigem.slotOcupado || slotDeOrigem.ItemGuardado == itemSeguravelDetectado))
                    {
                        slot = slotDeOrigem;
                    }
                }

                if (itemSeguravelDetectado != null && !itemSeguravelDetectado.podeSerPego)
                {
                    itemSeguravelDetectado = null;
                }

                // CanecaOri é a representação visual e também tem ItemSeguravel na cena.
                // O item que deve ir para a mão é o filho real mantido pelo slot.
                if (slot != null && slot.objetoAoOcupar != null
                    && itemSeguravelDetectado == slot.objetoAoOcupar.GetComponent<ItemSeguravel>())
                {
                    itemSeguravelDetectado = null;
                }

                if (slot != null && slot.PodeRetirarItem)
                {
                    slotDetectado = slot;
                }
            }
            else
            {
                // Um slot da colagem só destaca/recebe sua foto correspondente.
                if (slot != null && mecanicaColagem != null
                    && mecanicaColagem.EhPortaDaColagem(slot)
                    && mecanicaColagem.PodeColocarFoto(itemNaMao, slot))
                {
                    slotDetectado = slot;
                }
                // Mantém o comportamento dos outros slots do jogo.
                else if (slot != null && !slot.slotOcupado
                    && (mecanicaColagem == null || !mecanicaColagem.EhPortaDaColagem(slot)))
                {
                    slotDetectado = slot;
                }
            }
        }

        // Algumas fotos têm colliders muito finos. Se o raycast físico não as
        // encontrou, usa os limites visuais da foto, sem permitir atravessar paredes.
        if (itemNaMao == null && itemSeguravelDetectado == null)
        {
            MecanicaColagem[] mecanicasColagem = FindObjectsByType<MecanicaColagem>(FindObjectsSortMode.None);
            foreach (MecanicaColagem mecanica in mecanicasColagem)
            {
                ItemSeguravel fotoMirada = mecanica.EncontrarFotoNaMira(ray, distanciaInteracao, out float distanciaFoto);
                if (fotoMirada == null) continue;

                bool caminhoLivre = true;
                RaycastHit[] obstaculos = Physics.RaycastAll(ray, distanciaFoto, ~0, QueryTriggerInteraction.Ignore);
                foreach (RaycastHit obstaculo in obstaculos)
                {
                    if (obstaculo.collider.transform.IsChildOf(transform)) continue;
                    ItemSeguravel itemNoCaminho = obstaculo.collider.GetComponentInParent<ItemSeguravel>();
                    if (itemNoCaminho == fotoMirada) continue;
                    if (obstaculo.distance < distanciaFoto - 0.02f)
                    {
                        caminhoLivre = false;
                        break;
                    }
                }

                if (caminhoLivre)
                {
                    itemSeguravelDetectado = fotoMirada;
                    break;
                }
            }
        }

        AtualizarOutlineEstanteLivro(ray);

        // Atualiza o destaque das interações normais.
        if (objetoAtual != interativelDetectado)
        {
            if (objetoAtual != null) objetoAtual.SetHighlight(false);
            objetoAtual = interativelDetectado;
            if (objetoAtual != null) objetoAtual.SetHighlight(true);
        }

        if (cafeteiraAtual != cafeteiraDetectada)
        {
            if (cafeteiraAtual != null) cafeteiraAtual.SetHighlight(false);
            cafeteiraAtual = cafeteiraDetectada;
        }
        // Reaplica o highlight para ele poder voltar quando a animação marcar o café como pronto.
        if (cafeteiraAtual != null) cafeteiraAtual.SetHighlight(true);

        if (carregadorAtual != carregadorDetectado)
        {
            if (carregadorAtual != null) carregadorAtual.SetHighlight(false);
            carregadorAtual = carregadorDetectado;
        }
        if (carregadorAtual != null) carregadorAtual.SetHighlight(true, itemNaMao);

        if (cadeiraAtual != cadeiraDetectada)
        {
            if (cadeiraAtual != null) cadeiraAtual.SetHighlight(false);
            cadeiraAtual = cadeiraDetectada;
        }
        if (cadeiraAtual != null) cadeiraAtual.SetHighlight(true);

        if (mecanicaColagemAtual != mecanicaColagemDetectada)
        {
            if (mecanicaColagemAtual != null) mecanicaColagemAtual.SetHighlightArvore(false);
            mecanicaColagemAtual = mecanicaColagemDetectada;
            if (mecanicaColagemAtual != null) mecanicaColagemAtual.SetHighlightArvore(true);
        }

        // Atualiza o destaque dos itens que podem ser apanhados.
        if (itemDestacadoAtual != itemSeguravelDetectado)
        {
            if (itemDestacadoAtual != null) itemDestacadoAtual.SetHighlight(false);
            itemDestacadoAtual = itemSeguravelDetectado;
            if (itemDestacadoAtual != null) itemDestacadoAtual.SetHighlight(true);
        }

        // Destaca o slot disponível para colocar ou ocupado para retirar o item.
        if (slotDestacadoAtual != slotDetectado)
        {
            if (slotDestacadoAtual != null) slotDestacadoAtual.SetHighlight(false);
            slotDestacadoAtual = slotDetectado;
            if (slotDestacadoAtual != null) slotDestacadoAtual.SetHighlight(true);
        }

        // Executa as ações do clique.
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (itemNaMao != null && EhLivroPT(itemNaMao)
                && TentarColocarLivroNaEstante(ray))
            {
                return;
            }

            if (itemNaMao == null && mecanicaColagemDetectada != null
                && mecanicaColagemDetectada.TentarMontarColagem())
            {
                return;
            }

            // O carregador funciona como ponto de retirada e devolução do telefone.
            if (carregadorAtual != null)
            {
                if (itemNaMao == null && carregadorAtual.TentarPegar(maoDoJogador))
                {
                    itemNaMao = carregadorAtual.telefone;
                    return;
                }
                if (itemNaMao != null && carregadorAtual.TentarDevolver(itemNaMao))
                {
                    itemNaMao = null;
                    return;
                }
            }

            if (cadeiraAtual != null)
            {
                cadeiraAtual.Interact(this);
                return;
            }

            // Retira o item real do slot; a representação visual do cenário é desativada pelo slot.
            if (itemNaMao == null && slotDetectado != null && slotDetectado.slotOcupado)
            {
                ItemSeguravel itemRetirado = slotDetectado.RetirarItem();
                if (itemRetirado != null)
                {
                    itemNaMao = itemRetirado;
                    itemNaMao.PegarItem(maoDoJogador);
                    AoPegarLivroPT();

                    if (slotDestacadoAtual != null)
                    {
                        slotDestacadoAtual.SetHighlight(false);
                        slotDestacadoAtual = null;
                    }

                    return;
                }
            }

            // 1. Devolve o item ao PortaItemSlot (quadrado azul) se tiver o item correto na mão.
            if (itemNaMao != null && slotDetectado != null && !slotDetectado.slotOcupado)
            {
                bool veioDesteSlot = itemNaMao.slotDeOrigem == slotDetectado;
                bool nomeAceito = string.Equals(slotDetectado.nomeDoItemAceito, itemNaMao.nomeDoItem, System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(slotDetectado.nomeDoItemAceito, itemNaMao.gameObject.name, System.StringComparison.OrdinalIgnoreCase);

                MecanicaColagem mecanicaDoSlot = slotDetectado.GetComponentInParent<MecanicaColagem>();
                bool fotoDaColagem = mecanicaDoSlot != null
                    && mecanicaDoSlot.PodeColocarFoto(itemNaMao, slotDetectado);

                // O slot de origem sempre pode receber seu próprio item, mesmo que outra mecânica
                // tenha mudado o nome exibido (por exemplo, para "Xícara com Café").
                if (veioDesteSlot || nomeAceito || fotoDaColagem)
                {
                    if (!slotDetectado.OcuparSlot(itemNaMao)) return;
                    itemNaMao = null;

                    if (slotDestacadoAtual != null)
                    {
                        slotDestacadoAtual.SetHighlight(false);
                        slotDestacadoAtual = null;
                    }

                    Debug.Log("Item devolvido ao slot de origem com sucesso!");
                    return;
                }
            }

            // 2. Apanha um item segurável, como a xícara sobre a TV.
            if (itemNaMao == null && itemSeguravelDetectado != null)
            {
                if (itemDestacadoAtual != null)
                {
                    itemDestacadoAtual.SetHighlight(false);
                    itemDestacadoAtual = null;
                }

                itemNaMao = itemSeguravelDetectado;
                itemNaMao.PegarItem(maoDoJogador);
                AoPegarLivroPT();
                return;
            }

            // 3. Aciona a lógica própria da cafeteira quando ela estiver no foco.
            if (cafeteiraAtual != null)
            {
                cafeteiraAtual.Interact();
                return;
            }

            // 4. Mantém as interações normais, como portas e tarefas.
            if (objetoAtual != null)
            {
                objetoAtual.Interact();
                return;
            }

            // Com a xícara cheia na mão, clicar em uma área sem interação consome o café.
            if (itemNaMao != null)
            {
                itemNaMao.BeberCafe();
            }
        }
    }

    private bool TentarColocarLivroNaEstante(Ray ray)
    {
        if (!Physics.Raycast(ray, out RaycastHit hit, distanciaInteracao, ~0, QueryTriggerInteraction.Ignore))
            return false;

        if (!EhEstante(hit.collider.transform)) return false;

        Transform livroNaEstante = EncontrarObjetoNaCena("LIVROPTESTANTE");
        if (livroNaEstante == null || livroNaEstante.gameObject.activeSelf)
            return false;

        ItemSeguravel livroDaMao = itemNaMao;
        livroNaEstante.gameObject.SetActive(true);
        livroDaMao.SoltarItem();
        livroDaMao.gameObject.SetActive(false);
        itemNaMao = null;
        if (outlineEstanteLivro != null)
        {
            outlineEstanteLivro.enabled = false;
            outlineEstanteLivro = null;
        }

        if (itemDestacadoAtual == livroDaMao)
            itemDestacadoAtual = null;

        Debug.Log("LIVROPT colocado na estante.");
        return true;
    }

    private void AoPegarLivroPT()
    {
        if (!EhLivroPT(itemNaMao)) return;

        StartCoroutine(ConcluirPegarLivroPT());
    }

    private IEnumerator ConcluirPegarLivroPT()
    {
        TaskManager taskManager = FindAnyObjectByType<TaskManager>();
        if (taskManager != null)
            taskManager.ReproduzirTelaPretaFecharRapido();
        else
            Debug.LogWarning("PlayerFPS: não encontrou TaskManager para tocar TelaPretaFecharRapido.", this);

        yield return new WaitForSecondsRealtime(1.5f);

        // Reutiliza a posição da cozinha, mantendo a altura normal atual do Player.
        // O teleporte após a conversa preserva essa altura quando ele está sentado.
        TeleportarPara(new Vector3(-7.66099977f, transform.position.y, -32.2929993f));

        CadeiraSentavelInteractable cadeira = FindAnyObjectByType<CadeiraSentavelInteractable>();
        if (cadeira != null) cadeira.RestaurarColliders();

        Transform posJantar = EncontrarObjetoNaCena("PósJantar");
        Transform louca = posJantar != null
            ? EncontrarDescendente(posJantar, "Louça")
            : null;
        if (louca != null)
            louca.gameObject.SetActive(false);
        else
            Debug.LogWarning("PlayerFPS: não encontrou Louça dentro de PósJantar para desativar.", this);
    }

    private void AtualizarOutlineEstanteLivro(Ray ray)
    {
        Outline novoOutline = null;
        if (EhLivroPT(itemNaMao)
            && Physics.Raycast(ray, out RaycastHit hit, distanciaInteracao, ~0, QueryTriggerInteraction.Ignore)
            && EhEstante(hit.collider.transform))
        {
            Transform raizVisual = EncontrarRaizVisualEstante(hit.collider.transform);
            if (raizVisual != null)
            {
                if (raizVisual.GetComponentsInChildren<Renderer>(true).Length == 0)
                {
                    Renderer renderizadorPai = raizVisual.GetComponentInParent<Renderer>();
                    if (renderizadorPai != null) raizVisual = renderizadorPai.transform;
                }

                novoOutline = raizVisual.GetComponent<Outline>();
                if (novoOutline == null)
                    novoOutline = raizVisual.gameObject.AddComponent<Outline>();

                novoOutline.OutlineMode = Outline.Mode.OutlineAndSilhouette;
                novoOutline.OutlineColor = new Color(1f, 0.28f, 0f, 1f);
                novoOutline.OutlineWidth = 22f;
                novoOutline.enabled = true;
            }
        }

        if (outlineEstanteLivro != novoOutline)
        {
            if (outlineEstanteLivro != null)
                outlineEstanteLivro.enabled = false;
            outlineEstanteLivro = novoOutline;
        }
    }

    private static bool EhEstante(Transform alvo)
    {
        while (alvo != null)
        {
            if (string.Equals(alvo.name, "Bookcase_0", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(alvo.name, "Prateleira", System.StringComparison.OrdinalIgnoreCase))
                return true;
            alvo = alvo.parent;
        }
        return false;
    }

    private static Transform EncontrarRaizVisualEstante(Transform alvo)
    {
        Transform prateleira = null;
        while (alvo != null)
        {
            if (string.Equals(alvo.name, "Bookcase_0", System.StringComparison.OrdinalIgnoreCase))
                return alvo;
            if (string.Equals(alvo.name, "Prateleira", System.StringComparison.OrdinalIgnoreCase))
                prateleira = alvo;
            alvo = alvo.parent;
        }
        return prateleira;
    }

    private static bool EhLivroPT(ItemSeguravel item)
    {
        return item != null && (
            string.Equals(item.nomeDoItem, "LIVROPT", System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.gameObject.name, "LIVROPT", System.StringComparison.OrdinalIgnoreCase));
    }

    private static Transform EncontrarObjetoNaCena(string nome)
    {
        GameObject[] raizes = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject raiz in raizes)
        {
            if (string.Equals(raiz.name, nome, System.StringComparison.OrdinalIgnoreCase))
                return raiz.transform;

            Transform encontrado = EncontrarDescendente(raiz.transform, nome);
            if (encontrado != null) return encontrado;
        }
        return null;
    }

    private static Transform EncontrarDescendente(Transform raiz, string nome)
    {
        foreach (Transform filho in raiz)
        {
            if (string.Equals(filho.name, nome, System.StringComparison.OrdinalIgnoreCase))
                return filho;

            Transform encontrado = EncontrarDescendente(filho, nome);
            if (encontrado != null) return encontrado;
        }
        return null;
    }
}
