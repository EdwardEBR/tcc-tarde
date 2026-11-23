using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;

public class PlayerFPS : MonoBehaviour
{
    private const float VelocidadePadraoPassos = 3f;
    [Header("Movimento")]
    public float velocidade = 3f;
    public float gravidade = -20f;

    [Header("Camera")]
    public Transform cameraPlayer;
    public float sensibilidade = 0.15f;
    [Tooltip("Velocidade de rotação da câmera pelo analógico direito, em graus por segundo.")]
    [Min(1f)] public float sensibilidadeControle = 140f;
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
    private Outline outlinePiaPratoAtual;
    private GameObject indicadorEstanteLivro;
    private TextMeshProUGUI textoIndicadorEstanteLivro;
    private Transform livroNaEstanteVisual;

    [Header("Sons de passos")]
    public AudioClip somPasso1;
    public AudioClip somPasso2;
    public float intervaloPassos = 1.5f;
    [Tooltip("Pausa adicional após cada clipe antes de tocar o próximo passo.")]
    public float pausaEntrePassos = 0.6f;
    private AudioSource audioPassos;
    private float proximoPasso;
    private bool alternarPasso;
    private AudioSource audioAmbienciaJantar;
    private AudioClip clipAmbienciaJantar;
    private Coroutine rotinaFadeAmbienciaJantar;
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
    private bool cameraLivreAtiva;
    private string bufferCheatCamera = string.Empty;
    private Keyboard tecladoCheatCamera;
    private Vector3 posicaoLocalCameraAntesDoModoLivre;
    private Quaternion rotacaoLocalCameraAntesDoModoLivre;
    private float rotacaoVerticalCameraLivre;
    private float velocidadeAtualCameraLivre = VelocidadeCameraLivre;
    private MiraCircularUI miraCameraLivre;
    private bool miraEstavaAtiva;
    private Renderer[] renderizadoresCorpoCameraLivre;
    private bool[] estadosRenderizadoresCorpoCameraLivre;
    private const string ComandoCameraLivre = "livre";
    private const float VelocidadeCameraLivre = 8f;
    private bool sentandoNaCadeira;
    private bool sentadoNaCadeira;
    private Vector3 posicaoNormalAntesDeSentar;
    private Quaternion rotacaoNormalAntesDeSentar;

    public float MeiaAlturaMundialDoPersonagem => controller != null
        ? controller.bounds.extents.y
        : Mathf.Max(0.5f, transform.lossyScale.y);
    public bool CameraLivreAtiva => cameraLivreAtiva;
    public bool InteracaoEmAndamento =>
        modoConversaAtivo || sentadoNaCadeira || sentandoNaCadeira || itemNaMao != null
        || mouseLivre || cameraLivreAtiva;

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
        if (EstaNoNivel2())
        {
            clipAmbienciaJantar = Resources.Load<AudioClip>("Sons/AudioClip/DinnerAmbience");
            if (clipAmbienciaJantar == null)
                Debug.LogWarning("PlayerFPS: não foi possível carregar DinnerAmbience em Resources/Sons/AudioClip.", this);
            else
            {
                audioAmbienciaJantar = gameObject.AddComponent<AudioSource>();
                audioAmbienciaJantar.playOnAwake = false;
                audioAmbienciaJantar.loop = true;
                audioAmbienciaJantar.spatialBlend = 0f;
                audioAmbienciaJantar.volume = 1f;
            }
        }
        ConfigurarSomAmbiente();
        ConfigurarSomGeladeira();
        ConfigurarPontoDaMao();
        ConfigurarIndicadorEstanteLivro();
        DesativarOutlineDaEstanteNivel2();
        TravarMouse();
        if (GetComponent<PauseMenuManager>() == null)
            gameObject.AddComponent<PauseMenuManager>();
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
        somAmbiente.volume = 1f;
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
        somGeladeira.volume = 0.4f;
        somGeladeira.minDistance = 1f;
        somGeladeira.maxDistance = 16f;
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
        if (PauseMenuManager.JogoPausado) return;

        AtualizarTecladoCheatCamera();

        if (cameraLivreAtiva)
        {
            if (indicadorEstanteLivro != null) indicadorEstanteLivro.SetActive(false);
            AtualizarCameraLivre();
            return;
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

    private void AtualizarTecladoCheatCamera()
    {
        Keyboard tecladoAtual = Keyboard.current;
        if (tecladoAtual == tecladoCheatCamera) return;

        if (tecladoCheatCamera != null)
            tecladoCheatCamera.onTextInput -= ReceberTextoCheatCamera;

        tecladoCheatCamera = tecladoAtual;
        if (tecladoCheatCamera != null)
            tecladoCheatCamera.onTextInput += ReceberTextoCheatCamera;
    }

    private void OnDisable()
    {
        if (tecladoCheatCamera != null)
            tecladoCheatCamera.onTextInput -= ReceberTextoCheatCamera;
        tecladoCheatCamera = null;
    }

    private void ReceberTextoCheatCamera(char caractere)
    {
        if (!char.IsLetter(caractere) || char.ToLowerInvariant(caractere) != caractere)
        {
            bufferCheatCamera = string.Empty;
            return;
        }

        bufferCheatCamera += caractere;
        if (!ComandoCameraLivre.StartsWith(bufferCheatCamera, System.StringComparison.Ordinal))
            bufferCheatCamera = caractere == 'l' ? "l" : string.Empty;

        if (bufferCheatCamera != ComandoCameraLivre) return;

        bufferCheatCamera = string.Empty;
        AlternarCameraLivre();
    }

    private void AlternarCameraLivre()
    {
        if (cameraPlayer == null) return;

        cameraLivreAtiva = !cameraLivreAtiva;
        foreach (ConversaTelefonicaUI conversa in FindObjectsByType<ConversaTelefonicaUI>())
        {
            if (conversa != null) conversa.DefinirCameraLivreAtiva(cameraLivreAtiva);
        }

        if (cameraLivreAtiva)
        {
            posicaoLocalCameraAntesDoModoLivre = cameraPlayer.localPosition;
            rotacaoLocalCameraAntesDoModoLivre = cameraPlayer.localRotation;
            float anguloVertical = cameraPlayer.eulerAngles.x;
            rotacaoVerticalCameraLivre = anguloVertical > 180f ? anguloVertical - 360f : anguloVertical;
            miraCameraLivre = FindAnyObjectByType<MiraCircularUI>();
            if (miraCameraLivre != null)
            {
                miraEstavaAtiva = miraCameraLivre.enabled;
                miraCameraLivre.enabled = false;
            }
            renderizadoresCorpoCameraLivre = GetComponents<Renderer>();
            estadosRenderizadoresCorpoCameraLivre = new bool[renderizadoresCorpoCameraLivre.Length];
            for (int i = 0; i < renderizadoresCorpoCameraLivre.Length; i++)
            {
                estadosRenderizadoresCorpoCameraLivre[i] = renderizadoresCorpoCameraLivre[i].enabled;
                renderizadoresCorpoCameraLivre[i].enabled = false;
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            descartarDeltaMouse = true;
            Debug.Log("Cheat 'livre': câmera livre ativada. Digite 'livre' novamente para voltar.", this);
            return;
        }

        cameraPlayer.localPosition = posicaoLocalCameraAntesDoModoLivre;
        cameraPlayer.localRotation = rotacaoLocalCameraAntesDoModoLivre;
        if (miraCameraLivre != null)
        {
            miraCameraLivre.enabled = miraEstavaAtiva;
            miraCameraLivre = null;
        }
        if (renderizadoresCorpoCameraLivre != null)
        {
            for (int i = 0; i < renderizadoresCorpoCameraLivre.Length; i++)
            {
                if (renderizadoresCorpoCameraLivre[i] != null)
                    renderizadoresCorpoCameraLivre[i].enabled = estadosRenderizadoresCorpoCameraLivre[i];
            }
            renderizadoresCorpoCameraLivre = null;
            estadosRenderizadoresCorpoCameraLivre = null;
        }
        TravarMouse();
        Debug.Log("Cheat 'livre': câmera normal restaurada.", this);
    }

    private void AtualizarCameraLivre()
    {
        if (cameraPlayer == null) return;

        Keyboard teclado = Keyboard.current;
        Mouse mouse = Mouse.current;
        Gamepad controle = Gamepad.current;
        Joystick joystick = Joystick.current;
        if (teclado == null && controle == null && joystick == null && mouse == null) return;

        Vector2 deltaMouse = descartarDeltaMouse || mouse == null ? Vector2.zero : mouse.delta.ReadValue();
        descartarDeltaMouse = false;
        if (!ValorFinito(deltaMouse.x) || !ValorFinito(deltaMouse.y))
            deltaMouse = Vector2.zero;
        else
            deltaMouse = Vector2.ClampMagnitude(deltaMouse, 100f);

        float scroll = mouse != null ? mouse.scroll.ReadValue().y : 0f;
        if (!float.IsNaN(scroll) && !float.IsInfinity(scroll) && Mathf.Abs(scroll) > 0.01f)
            velocidadeAtualCameraLivre = Mathf.Clamp(
                velocidadeAtualCameraLivre + Mathf.Clamp(scroll / 120f, -3f, 3f), 1f, 40f);

        if (!ValorFinito(rotacaoHorizontalAlvo))
            rotacaoHorizontalAlvo = transform.eulerAngles.y;
        if (!ValorFinito(rotacaoVerticalCameraLivre))
        {
            float inclinacaoAtual = cameraPlayer.eulerAngles.x;
            rotacaoVerticalCameraLivre = inclinacaoAtual > 180f ? inclinacaoAtual - 360f : inclinacaoAtual;
        }

        float sensibilidadeSegura = ValorFinito(sensibilidade) ? sensibilidade : 0f;
        rotacaoHorizontalAlvo = Mathf.Repeat(rotacaoHorizontalAlvo + deltaMouse.x * sensibilidadeSegura, 360f);
        Vector2 movimento = ControleCompatibilidade.LerMovimento();
        Vector2 olhar = ControleCompatibilidade.LerOlharGenerico();
        rotacaoHorizontalAlvo = Mathf.Repeat(
            rotacaoHorizontalAlvo + olhar.x * sensibilidadeControle * Time.deltaTime, 360f);
        rotacaoVerticalCameraLivre = Mathf.Clamp(
            rotacaoVerticalCameraLivre - deltaMouse.y * sensibilidadeSegura
            - olhar.y * sensibilidadeControle * Time.deltaTime, -85f, 85f);
        cameraPlayer.rotation = Quaternion.Euler(rotacaoVerticalCameraLivre, rotacaoHorizontalAlvo, 0f);

        if (teclado != null && (controle != null || joystick != null))
        {
            if (teclado.wKey.isPressed) movimento.y += 1f;
            if (teclado.sKey.isPressed) movimento.y -= 1f;
            if (teclado.dKey.isPressed) movimento.x += 1f;
            if (teclado.aKey.isPressed) movimento.x -= 1f;
        }
        Vector3 direcao = cameraPlayer.forward * movimento.y + cameraPlayer.right * movimento.x;
        if (teclado != null && (teclado.eKey.isPressed || teclado.spaceKey.isPressed)) direcao += Vector3.up;
        if (teclado != null && (teclado.qKey.isPressed || teclado.leftCtrlKey.isPressed)) direcao -= Vector3.up;

        if (direcao.sqrMagnitude > 1f) direcao.Normalize();
        float multiplicador = (teclado != null && teclado.leftShiftKey.isPressed)
            || (controle != null && controle.leftStickButton.isPressed)
            || ControleCompatibilidade.BotaoJoystickSegurado(8) ? 3f : 1f;
        cameraPlayer.position += direcao * velocidadeAtualCameraLivre * multiplicador * Time.deltaTime;
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
        if (indicadorEstanteLivro != null) indicadorEstanteLivro.SetActive(false);

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

    public void RestaurarCursorAposPausa()
    {
        if (modoConversaAtivo && !cameraLivreAtiva)
            LiberarMouse();
        else
            TravarMouse();
    }

    public void TravarCursorAposFecharMenu()
    {
        TravarMouse();
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

        bool controleInputSystem = Gamepad.current != null || Joystick.current != null;
        Vector2 input = ControleCompatibilidade.LerMovimento();

        if (Keyboard.current != null && controleInputSystem)
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

        if (movimento.sqrMagnitude > 1f)
            movimento.Normalize();

        bool correndo = (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
            || (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed)
            || ControleCompatibilidade.BotaoJoystickSegurado(8);
        float velocidadeMovimento = velocidade * (correndo ? 1.5f : 1f);
        controller.Move(movimento * velocidadeMovimento * Time.deltaTime);

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
                // Diminui o tom e espaça os passos na mesma proporção da velocidade do jogador.
                float fatorVelocidadePassos = Mathf.Clamp(
                    velocidadeMovimento * Mathf.Clamp01(input.magnitude) / VelocidadePadraoPassos,
                    0.5f, 1.5f);
                audioPassos.pitch = fatorVelocidadePassos;
                audioPassos.volume = 1f;
                audioPassos.clip = passo;
                audioPassos.Play();
                float duracaoMaisLonga = Mathf.Max(
                    passo.length,
                    somPasso1 != null ? somPasso1.length : 0f,
                    somPasso2 != null ? somPasso2.length : 0f);
                float intervaloBase = Mathf.Max(intervaloPassos, duracaoMaisLonga + pausaEntrePassos);
                proximoPasso = Time.time + intervaloBase / fatorVelocidadePassos;
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
        if (cameraPlayer == null) return;

        AtualizarZoom();

        // Depois de voltar o foco para o jogo, o Input System pode entregar
        // um delta acumulado que deixa a mira em um estado inesperado.
        if (descartarDeltaMouse)
        {
            descartarDeltaMouse = false;
            return;
        }

        Vector2 mouse = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        if (!ValorFinito(mouse.x) || !ValorFinito(mouse.y))
            return;

        // Um delta extremo pode ocorrer quando o cursor recupera o foco; evita
        // que esse valor contamine a rotação da câmera.
        mouse = Vector2.ClampMagnitude(mouse, 100f);
        Vector2 olharControle = Gamepad.current != null
            ? Gamepad.current.rightStick.ReadValue() * sensibilidadeControle * Time.deltaTime
            : ControleCompatibilidade.LerOlharGenerico() * sensibilidadeControle * Time.deltaTime;

        rotacaoHorizontalAlvo += mouse.x * sensibilidade + olharControle.x;
        if (sentadoNaCadeira)
        {
            float deslocamentoHorizontal = Mathf.DeltaAngle(rotacaoHorizontalBaseSentado, rotacaoHorizontalAlvo);
            rotacaoHorizontalAlvo = rotacaoHorizontalBaseSentado
                + Mathf.Clamp(deslocamentoHorizontal, -limiteGiroHorizontalSentado, limiteGiroHorizontalSentado);
        }

        // Rotação vertical da câmara
        float novaRotacaoCamera = rotacaoCamera - mouse.y * sensibilidade - olharControle.y;
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
            tempo += Time.deltaTime;
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
        IniciarAmbienciaJantar();

        SequenciaCronologicaPessoas sequenciaPessoas = FindAnyObjectByType<SequenciaCronologicaPessoas>();
        if (sequenciaPessoas != null) sequenciaPessoas.Iniciar();

        TaskManager gerenciadorTarefas = FindAnyObjectByType<TaskManager>();
        if (gerenciadorTarefas != null)
            gerenciadorTarefas.IniciarDialogoAposSentar();
    }

    public void TeleportarPara(Vector3 posicao)
    {
        if (EhTeleporteParaCozinhaNivel2(posicao))
            PararAmbienciaJantarComFade();

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

        bool zoomAtivo = Mouse.current != null && Mouse.current.middleButton.isPressed;
        zoomAtivo |= Gamepad.current != null && Gamepad.current.leftTrigger.ReadValue() > 0.5f;
        float campoVisaoAlvo = zoomAtivo
            ? Mathf.Clamp(campoVisaoZoom, 1f, campoVisaoOriginal)
            : campoVisaoOriginal;
        cameraComponente.fieldOfView = Mathf.Lerp(
            cameraComponente.fieldOfView,
            campoVisaoAlvo,
            velocidadeZoom * Time.deltaTime);
    }

    void GerenciarInteracao()
    {
        if (string.Equals(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            "Nivel4",
            System.StringComparison.OrdinalIgnoreCase)) return;

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
        Outline outlinePiaPratoDetectada = null;

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
                    TelefoneCarregadorInteractable[] carregadores = FindObjectsByType<TelefoneCarregadorInteractable>();
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

        PlateStackKitchenInteractable pratoNaMaoMirandoPia = itemNaMao != null
            ? itemNaMao.GetComponent<PlateStackKitchenInteractable>()
            : null;
        if (pratoNaMaoMirandoPia != null
            && pratoNaMaoMirandoPia.EncontrarPiaMirada(ray, distanciaInteracao, out RaycastHit piaMirada))
        {
            outlinePiaPratoDetectada = pratoNaMaoMirandoPia.ObterOutlinePia(piaMirada.collider.transform);
        }

        // Algumas fotos têm colliders muito finos. Se o raycast físico não as
        // encontrou, usa os limites visuais da foto, sem permitir atravessar paredes.
        if (itemNaMao == null && itemSeguravelDetectado == null)
        {
            MecanicaColagem[] mecanicasColagem = FindObjectsByType<MecanicaColagem>();
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

        AtualizarIndicadorEstanteLivro(ray);

        if (outlinePiaPratoAtual != outlinePiaPratoDetectada)
        {
            if (outlinePiaPratoAtual != null) outlinePiaPratoAtual.enabled = false;
            outlinePiaPratoAtual = outlinePiaPratoDetectada;
        }
        if (outlinePiaPratoAtual != null) outlinePiaPratoAtual.enabled = true;

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
        if (AcaoDeInteracaoPressionada())
        {
            PlateStackKitchenInteractable pratoParaPia = itemNaMao != null
                ? itemNaMao.GetComponent<PlateStackKitchenInteractable>()
                : null;
            if (pratoParaPia != null
                && pratoParaPia.EncontrarPiaMirada(ray, distanciaInteracao, out RaycastHit hitPia))
            {
                pratoParaPia.ColocarNaPia(itemNaMao, hitPia.point);
                itemNaMao = null;
                PauseMenuManager.TentarAutosave();
                return;
            }

            if (itemNaMao != null && EhLivroPT(itemNaMao)
                && TentarColocarLivroNaEstante(ray))
            {
                PauseMenuManager.TentarAutosave();
                return;
            }

            if (itemNaMao == null && mecanicaColagemDetectada != null
                && mecanicaColagemDetectada.TentarMontarColagem())
            {
                PauseMenuManager.TentarAutosave();
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
                    PauseMenuManager.TentarAutosave();
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
                    PlateStackKitchenInteractable pratoRetirado = itemNaMao.GetComponent<PlateStackKitchenInteractable>();
                    if (pratoRetirado != null) pratoRetirado.AoPegar();
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
                    PauseMenuManager.TentarAutosave();
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
                PlateStackKitchenInteractable pratoPego = itemNaMao.GetComponent<PlateStackKitchenInteractable>();
                if (pratoPego != null) pratoPego.AoPegar();
                AoPegarLivroPT();
                return;
            }

            // 3. Aciona a lógica própria da cafeteira quando ela estiver no foco.
            if (cafeteiraAtual != null)
            {
                cafeteiraAtual.Interact();
                PauseMenuManager.TentarAutosave();
                return;
            }

            // 4. Mantém as interações normais, como portas e tarefas.
            if (objetoAtual != null)
            {
                objetoAtual.Interact();
                PauseMenuManager.TentarAutosave();
                return;
            }

            // Com a xícara cheia na mão, clicar em uma área sem interação consome o café.
            if (itemNaMao != null)
            {
                itemNaMao.BeberCafe();
            }
        }
    }

    private static bool AcaoDeInteracaoPressionada()
    {
        bool mouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool teclado = Keyboard.current != null
            && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);
        bool controle = ControleCompatibilidade.BotaoXboxAPressionado(Gamepad.current)
            || ControleCompatibilidade.BotaoXboxXPressionado(Gamepad.current)
            || ControleCompatibilidade.BotaoXboxYPressionado(Gamepad.current);
        bool joystick = ControleCompatibilidade.BotaoJoystickPressionado(0)
            || ControleCompatibilidade.BotaoJoystickPressionado(2)
            || ControleCompatibilidade.BotaoJoystickPressionado(3);
        return mouse || teclado || controle || joystick;
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
        if (indicadorEstanteLivro != null) indicadorEstanteLivro.SetActive(false);

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

        yield return new WaitForSeconds(TaskManager.DuracaoFechamentoTelaPretaRapida);

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

    private void IniciarAmbienciaJantar()
    {
        if (audioAmbienciaJantar == null || clipAmbienciaJantar == null) return;
        if (rotinaFadeAmbienciaJantar != null)
        {
            StopCoroutine(rotinaFadeAmbienciaJantar);
            rotinaFadeAmbienciaJantar = null;
        }

        audioAmbienciaJantar.clip = clipAmbienciaJantar;
        audioAmbienciaJantar.volume = 1f;
        if (!audioAmbienciaJantar.isPlaying)
            audioAmbienciaJantar.Play();
    }

    private void PararAmbienciaJantarComFade()
    {
        if (audioAmbienciaJantar == null || !audioAmbienciaJantar.isPlaying) return;
        if (rotinaFadeAmbienciaJantar != null)
            StopCoroutine(rotinaFadeAmbienciaJantar);
        rotinaFadeAmbienciaJantar = StartCoroutine(FadeSaidaAmbienciaJantar());
    }

    private IEnumerator FadeSaidaAmbienciaJantar()
    {
        float volumeInicial = audioAmbienciaJantar.volume;
        const float duracao = 0.75f;
        float tempo = 0f;
        while (tempo < duracao && audioAmbienciaJantar != null)
        {
            tempo += Time.unscaledDeltaTime;
            audioAmbienciaJantar.volume = Mathf.Lerp(volumeInicial, 0f, Mathf.Clamp01(tempo / duracao));
            yield return null;
        }

        if (audioAmbienciaJantar != null)
        {
            audioAmbienciaJantar.Stop();
            audioAmbienciaJantar.volume = 1f;
        }
        rotinaFadeAmbienciaJantar = null;
    }

    private static bool EstaNoNivel2()
    {
        return string.Equals(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            "Nivel2",
            System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool EhTeleporteParaCozinhaNivel2(Vector3 destino)
    {
        const float tolerancia = 0.75f;
        return EstaNoNivel2()
            && Mathf.Abs(destino.x + 7.661f) <= tolerancia
            && Mathf.Abs(destino.z + 32.293f) <= tolerancia;
    }

    private void ConfigurarIndicadorEstanteLivro()
    {
        if (!string.Equals(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            "Nivel2",
            System.StringComparison.OrdinalIgnoreCase)) return;

        livroNaEstanteVisual = EncontrarObjetoNaCena("LIVROPTESTANTE");

        GameObject canvasObjeto = new GameObject(
            "IndicadorInteracaoEstante", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObjeto.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler escalador = canvasObjeto.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        indicadorEstanteLivro = new GameObject(
            "AvisoGuardarLivro", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        indicadorEstanteLivro.transform.SetParent(canvasObjeto.transform, false);
        RectTransform painel = indicadorEstanteLivro.GetComponent<RectTransform>();
        painel.anchorMin = new Vector2(0.5f, 0f);
        painel.anchorMax = new Vector2(0.5f, 0f);
        painel.pivot = new Vector2(0.5f, 0f);
        painel.anchoredPosition = new Vector2(0f, 86f);
        painel.sizeDelta = new Vector2(520f, 58f);
        indicadorEstanteLivro.GetComponent<Image>().color = new Color(0.05f, 0.04f, 0.03f, 0.82f);

        GameObject textoObjeto = new GameObject(
            "TextoAvisoGuardarLivro", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textoObjeto.transform.SetParent(indicadorEstanteLivro.transform, false);
        RectTransform areaTexto = textoObjeto.GetComponent<RectTransform>();
        areaTexto.anchorMin = Vector2.zero;
        areaTexto.anchorMax = Vector2.one;
        areaTexto.offsetMin = new Vector2(12f, 4f);
        areaTexto.offsetMax = new Vector2(-12f, -4f);
        textoIndicadorEstanteLivro = textoObjeto.GetComponent<TextMeshProUGUI>();
        textoIndicadorEstanteLivro.font = TMP_Settings.defaultFontAsset;
        textoIndicadorEstanteLivro.text = "CLIQUE ESQUERDO  •  GUARDAR LIVRO";
        textoIndicadorEstanteLivro.fontSize = 24f;
        textoIndicadorEstanteLivro.fontStyle = FontStyles.Bold;
        textoIndicadorEstanteLivro.color = Color.white;
        textoIndicadorEstanteLivro.alignment = TextAlignmentOptions.Center;
        indicadorEstanteLivro.SetActive(false);
    }

    private static void DesativarOutlineDaEstanteNivel2()
    {
        if (!string.Equals(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            "Nivel2",
            System.StringComparison.OrdinalIgnoreCase)) return;

        GameObject estante = GameObject.Find("Bookcase_0");
        if (estante == null) return;

        Outline outline = estante.GetComponent<Outline>();
        if (outline != null) outline.enabled = false;
    }

    private void AtualizarIndicadorEstanteLivro(Ray ray)
    {
        bool olhandoParaEstante = string.Equals(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                "Nivel2",
                System.StringComparison.OrdinalIgnoreCase)
            && Physics.Raycast(ray, out RaycastHit hit, distanciaInteracao, ~0, QueryTriggerInteraction.Ignore)
            && EhEstante(hit.collider.transform);

        if (indicadorEstanteLivro == null) return;
        if (!olhandoParaEstante)
        {
            indicadorEstanteLivro.SetActive(false);
            return;
        }

        bool livroJaGuardado = livroNaEstanteVisual != null && livroNaEstanteVisual.gameObject.activeSelf;
        string instrucao = livroJaGuardado
            ? "LIVRO JÁ GUARDADO"
            : itemNaMao == null
                ? "PEGUE O LIVRO PARA INTERAGIR COM A ESTANTE"
                : EhLivroPT(itemNaMao)
                    ? "CLIQUE ESQUERDO  •  GUARDAR LIVRO"
                    : "ESTANTE  •  SÓ ACEITA O LIVRO AZUL";

        if (textoIndicadorEstanteLivro.text != instrucao)
            textoIndicadorEstanteLivro.text = instrucao;
        if (!indicadorEstanteLivro.activeSelf)
            indicadorEstanteLivro.SetActive(true);
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
