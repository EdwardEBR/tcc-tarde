using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public sealed class PauseMenuManager : MonoBehaviour
{
    private const string PrefixoSave = "Checkpoint_";
    private static readonly Color CorDestaqueSlimUI = new Color(1f, 0.6862745f, 0f, 1f);
    private static TMP_FontAsset fonteSlimUI;
    private static Texture2D cursorTransparente;

    public static bool JogoPausado { get; private set; }

    private PlayerFPS jogador;
    private GameObject canvasMenu;
    private GameObject canvasOpcoes;
    private GameObject painelPrincipal;
    private GameObject painelOpcoes;
    private GameObject painelConfirmacaoSaida;
    private TextMeshProUGUI textoStatus;
    private Slider sliderVolume;
    private Slider sliderSensibilidade;
    private float proximoStatus;
    private int framesForcarCursorBloqueado;
    private float timeScaleAntesDaPausa = 1f;
    private bool audioPausadoAntesDaPausa;

    private void Awake()
    {
        jogador = GetComponent<PlayerFPS>();
        CriarEventSystemSeNecessario();
        CriarMenu();
        RestaurarCheckpointSeDisponivel();
    }

    private void Update()
    {
        if (JogoPausado) ControleCompatibilidade.AtualizarNavegacaoUI();
        if (proximoStatus > 0f && Time.unscaledTime >= proximoStatus)
        {
            textoStatus.text = string.Empty;
            proximoStatus = 0f;
        }

        bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool start = (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            || ControleCompatibilidade.BotaoJoystickPressionado(7);
        bool cancel = JogoPausado && ControleCompatibilidade.BotaoXboxBPressionado(Gamepad.current);
        cancel |= JogoPausado && ControleCompatibilidade.BotaoJoystickPressionado(1);
        if (!escape && !start && !cancel) return;

        if (!JogoPausado)
            AbrirMenu();
        else if (canvasOpcoes != null && canvasOpcoes.activeSelf)
            MostrarMenuPrincipal();
        else if (painelConfirmacaoSaida != null && painelConfirmacaoSaida.activeSelf)
            CancelarSaidaSemSalvar();
        else
            RetomarJogo();
    }

    private void LateUpdate()
    {
        if (framesForcarCursorBloqueado <= 0) return;

        if (jogador != null) jogador.TravarCursorAposFecharMenu();
        OcultarCursorDoJogo();
        framesForcarCursorBloqueado--;
    }

    private void OnDestroy()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        if (JogoPausado)
            RestaurarTempoEAudio();
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    private void CriarEventSystemSeNecessario()
    {
        EventSystem sistema = FindAnyObjectByType<EventSystem>();
        if (sistema == null)
        {
            GameObject eventSystem = new GameObject("EventSystem (Pause Menu)", typeof(EventSystem));
            sistema = eventSystem.GetComponent<EventSystem>();
        }

        ControleCompatibilidade.GarantirModuloUI(sistema);
    }

    private void CriarMenu()
    {
        canvasMenu = new GameObject("Canvas_PauseMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasMenu.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasMenu.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject fundoEscuro = CriarImagemUI(canvasMenu.transform, "Fundo_Escurecido", new Color(0f, 0f, 0f, 0.62f));
        RectTransform fundoRect = fundoEscuro.GetComponent<RectTransform>();
        fundoRect.anchorMin = Vector2.zero;
        fundoRect.anchorMax = Vector2.one;
        fundoRect.offsetMin = Vector2.zero;
        fundoRect.offsetMax = Vector2.zero;

        painelPrincipal = CriarPainel(canvasMenu.transform, "Painel_Pausa", new Vector2(600f, 620f));
        painelPrincipal.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        painelPrincipal.GetComponent<Image>().raycastTarget = false;
        painelPrincipal.GetComponent<RectTransform>().anchoredPosition = new Vector2(-570f, 0f);
        TextMeshProUGUI tituloPausa = CriarTexto(painelPrincipal.transform, "Título", "PAUSADO", 42f, new Vector2(0f, 240f), new Vector2(500f, 70f));
        tituloPausa.color = Color.white;
        tituloPausa.fontStyle = FontStyles.Normal;
        CriarBotao(painelPrincipal.transform, "Continuar", new Vector2(0f, 130f), RetomarJogo);
        CriarBotao(painelPrincipal.transform, "Salvar jogo", new Vector2(0f, 40f), SalvarJogo);
        CriarBotao(painelPrincipal.transform, "Configurações", new Vector2(0f, -50f), MostrarOpcoes);
        CriarBotao(painelPrincipal.transform, "Sair para o menu", new Vector2(0f, -140f), SairParaOMenu);
        textoStatus = CriarTexto(painelPrincipal.transform, "Status", string.Empty, 20f, new Vector2(0f, -245f), new Vector2(500f, 38f));

        canvasOpcoes = new GameObject("Canv_Options", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvasDeOpcoes = canvasOpcoes.GetComponent<Canvas>();
        canvasDeOpcoes.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasDeOpcoes.sortingOrder = 201;
        CanvasScaler escaladorOpcoes = canvasOpcoes.GetComponent<CanvasScaler>();
        escaladorOpcoes.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escaladorOpcoes.referenceResolution = new Vector2(1920f, 1080f);
        escaladorOpcoes.matchWidthOrHeight = 0.5f;
        painelOpcoes = CriarPainel(canvasOpcoes.transform, "Painel_Opções", new Vector2(700f, 520f));
        CriarTexto(painelOpcoes.transform, "Título", "CONFIGURAÇÕES", 38f, new Vector2(0f, 190f), new Vector2(620f, 60f));
        CriarTexto(painelOpcoes.transform, "RótuloVolume", "Volume", 23f, new Vector2(-210f, 75f), new Vector2(180f, 42f));
        sliderVolume = CriarSlider(painelOpcoes.transform, "SliderVolume", new Vector2(100f, 75f), 380f, 0f, 1f, AudioListener.volume);
        sliderVolume.onValueChanged.AddListener(valor => AudioListener.volume = valor);

        CriarTexto(painelOpcoes.transform, "RótuloSensibilidade", "Sensibilidade do mouse", 23f, new Vector2(-170f, -25f), new Vector2(260f, 42f));
        float sensibilidadeAtual = jogador != null ? jogador.sensibilidade : 0.2f;
        sliderSensibilidade = CriarSlider(painelOpcoes.transform, "SliderSensibilidade", new Vector2(150f, -25f), 300f, 0.05f, 1f, sensibilidadeAtual);
        sliderSensibilidade.onValueChanged.AddListener(AtualizarSensibilidade);
        CriarBotao(painelOpcoes.transform, "Voltar", new Vector2(0f, -180f), MostrarMenuPrincipal);

        painelConfirmacaoSaida = CriarPainel(canvasMenu.transform, "Painel_ConfirmarSaidaSemSalvar", new Vector2(680f, 390f));
        CriarTexto(
            painelConfirmacaoSaida.transform,
            "AvisoProgresso",
            "Lembre-se de salvar seu progresso antes de sair.\nDeseja voltar ao menu principal?",
            26f,
            new Vector2(0f, 105f),
            new Vector2(600f, 115f));
        CriarBotao(painelConfirmacaoSaida.transform, "Continuar jogando", new Vector2(0f, 5f), CancelarSaidaSemSalvar);
        CriarBotao(painelConfirmacaoSaida.transform, "Voltar ao menu principal", new Vector2(0f, -85f), ConfirmarSaidaSemSalvar);
        painelConfirmacaoSaida.SetActive(false);

        canvasOpcoes.SetActive(false);
        canvasMenu.SetActive(false);
    }

    private static GameObject CriarPainel(Transform pai, string nome, Vector2 tamanho)
    {
        GameObject painel = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        painel.transform.SetParent(pai, false);
        RectTransform rect = painel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = tamanho;
        painel.GetComponent<Image>().color = new Color(0.035f, 0.035f, 0.035f, 0.94f);
        return painel;
    }

    private static TextMeshProUGUI CriarTexto(Transform pai, string nome, string conteudo, float tamanhoFonte, Vector2 posicao, Vector2 tamanho)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        objeto.transform.SetParent(pai, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = tamanho;
        TextMeshProUGUI texto = objeto.GetComponent<TextMeshProUGUI>();
        if (fonteSlimUI == null)
            fonteSlimUI = Resources.Load<TMP_FontAsset>("SlimUI/RUBIK-LIGHT-SDF");
        texto.font = fonteSlimUI != null ? fonteSlimUI : TMP_Settings.defaultFontAsset;
        texto.text = conteudo;
        texto.fontSize = tamanhoFonte;
        texto.fontStyle = FontStyles.Normal;
        texto.color = Color.white;
        texto.alignment = TextAlignmentOptions.Center;
        return texto;
    }

    private static Button CriarBotao(Transform pai, string rotulo, Vector2 posicao, Action acao)
    {
        GameObject prefabSlimUI = Resources.Load<GameObject>("SlimUI/Btn_MainMenu");
        GameObject objeto = prefabSlimUI != null
            ? Instantiate(prefabSlimUI)
            : new GameObject("Botão_" + rotulo, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        objeto.name = "Botão_" + rotulo;
        objeto.transform.SetParent(pai, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        // Usa o mesmo sprite do Btn_MainMenu, ampliado para os rótulos em português.
        rect.sizeDelta = new Vector2(520f, 82f);
        Image imagem = objeto.GetComponent<Image>();
        if (imagem != null) imagem.color = prefabSlimUI != null ? Color.white : CorDestaqueSlimUI;
        Button botao = objeto.GetComponent<Button>();
        botao.targetGraphic = imagem;
        botao.onClick.RemoveAllListeners();
        botao.onClick.AddListener(() => acao());

        if (prefabSlimUI == null)
        {
            ColorBlock cores = botao.colors;
            cores.normalColor = Color.white;
            cores.highlightedColor = new Color(1f, 0.86f, 0.55f, 1f);
            cores.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            botao.colors = cores;
            CriarTexto(objeto.transform, "Rótulo", rotulo, 24f, Vector2.zero, new Vector2(370f, 58f));
        }
        else
        {
            TMP_Text textoSlimUI = objeto.GetComponentInChildren<TMP_Text>(true);
            if (textoSlimUI != null) textoSlimUI.gameObject.SetActive(false);

            GameObject fundoBotao = new GameObject("Fundo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fundoBotao.transform.SetParent(objeto.transform, false);
            fundoBotao.transform.SetAsFirstSibling();
            RectTransform fundoBotaoRect = fundoBotao.GetComponent<RectTransform>();
            fundoBotaoRect.anchorMin = Vector2.zero;
            fundoBotaoRect.anchorMax = Vector2.one;
            fundoBotaoRect.offsetMin = new Vector2(7f, 6f);
            fundoBotaoRect.offsetMax = new Vector2(-7f, -6f);
            Image fundoBotaoImagem = fundoBotao.GetComponent<Image>();
            fundoBotaoImagem.color = new Color(0.035f, 0.035f, 0.035f, 0.84f);
            fundoBotaoImagem.raycastTarget = false;

            GameObject rotuloObjeto = new GameObject("Rótulo", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            rotuloObjeto.transform.SetParent(objeto.transform, false);
            RectTransform rotuloRect = rotuloObjeto.GetComponent<RectTransform>();
            rotuloRect.anchorMin = Vector2.zero;
            rotuloRect.anchorMax = Vector2.one;
            rotuloRect.offsetMin = new Vector2(14f, 4f);
            rotuloRect.offsetMax = new Vector2(-14f, -4f);
            TextMeshProUGUI rotuloTexto = rotuloObjeto.GetComponent<TextMeshProUGUI>();
            if (fonteSlimUI == null)
                fonteSlimUI = Resources.Load<TMP_FontAsset>("SlimUI/RUBIK-LIGHT-SDF");
            rotuloTexto.font = fonteSlimUI != null ? fonteSlimUI : TMP_Settings.defaultFontAsset;
            rotuloTexto.text = rotulo;
            rotuloTexto.fontSize = 32f;
            rotuloTexto.fontStyle = FontStyles.Normal;
            rotuloTexto.color = Color.white;
            rotuloTexto.alignment = TextAlignmentOptions.Center;
            rotuloTexto.raycastTarget = false;
        }
        return botao;
    }

    private static Slider CriarSlider(Transform pai, string nome, Vector2 posicao, float largura, float minimo, float maximo, float valorInicial)
    {
        GameObject raiz = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Slider));
        raiz.transform.SetParent(pai, false);
        RectTransform raizRect = raiz.GetComponent<RectTransform>();
        raizRect.anchorMin = raizRect.anchorMax = new Vector2(0.5f, 0.5f);
        raizRect.pivot = new Vector2(0.5f, 0.5f);
        raizRect.anchoredPosition = posicao;
        raizRect.sizeDelta = new Vector2(largura, 24f);
        raiz.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.2f, 1f);

        GameObject preenchimento = CriarImagemUI(raiz.transform, "Preenchimento", CorDestaqueSlimUI);
        RectTransform preenchimentoRect = preenchimento.GetComponent<RectTransform>();
        preenchimentoRect.anchorMin = Vector2.zero;
        preenchimentoRect.anchorMax = Vector2.one;
        preenchimentoRect.offsetMin = Vector2.zero;
        preenchimentoRect.offsetMax = Vector2.zero;

        GameObject alca = CriarImagemUI(raiz.transform, "Alça", Color.white);
        RectTransform alcaRect = alca.GetComponent<RectTransform>();
        alcaRect.sizeDelta = new Vector2(26f, 34f);

        Slider slider = raiz.GetComponent<Slider>();
        slider.minValue = minimo;
        slider.maxValue = maximo;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;
        slider.fillRect = preenchimentoRect;
        slider.handleRect = alcaRect;
        slider.targetGraphic = alca.GetComponent<Image>();
        slider.SetValueWithoutNotify(Mathf.Clamp(valorInicial, minimo, maximo));
        return slider;
    }

    private static GameObject CriarImagemUI(Transform pai, string nome, Color cor)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        objeto.transform.SetParent(pai, false);
        objeto.GetComponent<Image>().color = cor;
        return objeto;
    }

    private void AbrirMenu()
    {
        timeScaleAntesDaPausa = Time.timeScale;
        audioPausadoAntesDaPausa = AudioListener.pause;
        JogoPausado = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        painelPrincipal.SetActive(true);
        canvasOpcoes.SetActive(false);
        canvasMenu.SetActive(true);
        SelecionarPrimeiroElemento(painelPrincipal.transform);
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void MostrarOpcoes()
    {
        canvasMenu.SetActive(false);
        canvasOpcoes.SetActive(true);
        SelecionarPrimeiroElemento(painelOpcoes.transform);
    }

    private void MostrarMenuPrincipal()
    {
        canvasOpcoes.SetActive(false);
        canvasMenu.SetActive(true);
        SelecionarPrimeiroElemento(painelPrincipal.transform);
    }

    private void RetomarJogo()
    {
        RestaurarTempoEAudio();
        canvasMenu.SetActive(false);
        canvasOpcoes.SetActive(false);
        // Mantém o estado interno do PlayerFPS sincronizado com o cursor da UI.
        if (jogador != null) jogador.TravarCursorAposFecharMenu();
        OcultarCursorDoJogo();
        framesForcarCursorBloqueado = 30;
    }

    private void RestaurarTempoEAudio()
    {
        JogoPausado = false;
        Time.timeScale = timeScaleAntesDaPausa;
        AudioListener.pause = audioPausadoAntesDaPausa;
    }

    private static void OcultarCursorDoJogo()
    {
        if (cursorTransparente == null)
        {
            cursorTransparente = new Texture2D(1, 1, TextureFormat.RGBA32, false, true)
            {
                name = "CursorTransparenteGameplay",
                hideFlags = HideFlags.HideAndDontSave
            };
            cursorTransparente.SetPixel(0, 0, Color.clear);
            cursorTransparente.Apply(false, false);
        }

        Cursor.SetCursor(cursorTransparente, Vector2.zero, CursorMode.ForceSoftware);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SalvarJogo()
    {
        if (InteracaoEmAndamento())
        {
            MostrarStatus("Termine a conversa ou a interação antes de salvar.");
            return;
        }

        if (!GravarSaveAtual())
        {
            MostrarStatus("Não foi possível salvar o jogo.");
            return;
        }

        MostrarStatus("Jogo salvo nesta cena.");
    }

    private bool GravarSaveAtual(bool preservarSessaoParaContinuar = false)
    {
        if (jogador == null || InteracaoEmAndamento()) return false;

        try
        {
            SaveGameDatabase.Save(CriarDadosDeSaveAtual(), preservarSessaoParaContinuar);
            PlayerPrefs.Save();
            return true;
        }
        catch (System.Exception exception)
        {
            Debug.LogError("PauseMenuManager: falha ao salvar antes de sair para o menu: " + exception.Message, this);
            return false;
        }
    }

    private SaveGameDatabase.SaveData CriarDadosDeSaveAtual()
    {
        Vector3 posicao = jogador.transform.position;
        Vector3 rotacao = jogador.transform.eulerAngles;
        TaskManager gerenciadorTarefas = FindAnyObjectByType<TaskManager>();
        ConversaTelefonicaUI conversaSalva = null;
        foreach (ConversaTelefonicaUI conversa in FindObjectsByType<ConversaTelefonicaUI>())
        {
            if (conversa != null && conversa.ConversaAtiva)
            {
                conversaSalva = conversa;
                break;
            }
        }

        return new SaveGameDatabase.SaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            x = posicao.x, y = posicao.y, z = posicao.z,
            rotX = rotacao.x, rotY = rotacao.y, rotZ = rotacao.z,
            sensitivity = jogador.sensibilidade,
            volume = AudioListener.volume,
            completedTasks = gerenciadorTarefas != null
                ? gerenciadorTarefas.ObterTarefasConcluidas()
                : System.Array.Empty<string>(),
            conversationActive = conversaSalva != null,
            conversationObjectName = conversaSalva != null ? conversaSalva.gameObject.name : string.Empty,
            conversationNode = conversaSalva != null ? conversaSalva.IndiceNoAtual : 0
        };
    }

    private bool CapturarSessaoParaContinuar()
    {
        if (jogador == null) return false;

        try
        {
            SaveGameDatabase.DefinirSessaoParaContinuar(CriarDadosDeSaveAtual());
            return true;
        }
        catch (System.Exception exception)
        {
            Debug.LogError("PauseMenuManager: falha ao guardar a sessão para continuar: " + exception.Message, this);
            return false;
        }
    }

    private void MostrarStatus(string mensagem)
    {
        textoStatus.text = mensagem;
        proximoStatus = Time.unscaledTime + 3f;
    }

    private bool InteracaoEmAndamento()
    {
        if (jogador != null && jogador.InteracaoEmAndamento) return true;
        TaskManager gerenciadorTarefas = FindAnyObjectByType<TaskManager>();
        if (gerenciadorTarefas != null && gerenciadorTarefas.InteracaoEmAndamento) return true;
        foreach (ConversaTelefonicaUI conversa in FindObjectsByType<ConversaTelefonicaUI>())
            if (conversa != null && conversa.ConversaAtiva) return true;
        return false;
    }

    public static bool TentarAutosave()
    {
        PauseMenuManager menu = FindAnyObjectByType<PauseMenuManager>();
        if (menu == null || menu.InteracaoEmAndamento()) return false;
        return menu.GravarSaveAtual(preservarSessaoParaContinuar: true);
    }

    public static bool PrepararContinuacaoAposRetornoAoMenu()
    {
        PauseMenuManager menu = FindAnyObjectByType<PauseMenuManager>();
        return menu != null && menu.CapturarSessaoParaContinuar();
    }

    private void RestaurarCheckpointSeDisponivel()
    {
        SaveGameDatabase.SaveData save = SaveGameDatabase.Load();
        if (save == null) return;
        AudioListener.volume = save.volume;
        if (jogador != null)
            jogador.sensibilidade = save.sensitivity;

        if (jogador == null || !string.Equals(save.sceneName, SceneManager.GetActiveScene().name, System.StringComparison.Ordinal)) return;

        Vector3 posicao = new Vector3(save.x, save.y, save.z);
        Quaternion rotacao = Quaternion.Euler(save.rotX, save.rotY, save.rotZ);
        jogador.transform.rotation = rotacao;
        jogador.TeleportarPara(posicao);
    }

    private void AtualizarSensibilidade(float valor)
    {
        if (jogador != null) jogador.sensibilidade = valor;
        PlayerPrefs.SetFloat(PrefixoSave + "Sensibilidade", valor);
    }

    private void SairParaOMenu()
    {
        painelPrincipal.SetActive(false);
        painelConfirmacaoSaida.SetActive(true);
        SelecionarPrimeiroElemento(painelConfirmacaoSaida.transform);
    }

    private void CancelarSaidaSemSalvar()
    {
        painelConfirmacaoSaida.SetActive(false);
        painelPrincipal.SetActive(true);
        SelecionarPrimeiroElemento(painelPrincipal.transform);
    }

    private static void SelecionarPrimeiroElemento(Transform raiz)
    {
        EventSystem sistema = EventSystem.current;
        if (sistema == null || raiz == null) return;
        GameObject primeiroSelecionavel = null;

        foreach (Selectable elemento in raiz.GetComponentsInChildren<Selectable>())
        {
            if (elemento == null || !elemento.isActiveAndEnabled) continue;
            Navigation navegacao = elemento.navigation;
            if (navegacao.mode == Navigation.Mode.None)
            {
                navegacao.mode = Navigation.Mode.Automatic;
                elemento.navigation = navegacao;
            }
            if (primeiroSelecionavel == null && elemento.interactable)
                primeiroSelecionavel = elemento.gameObject;
        }

        sistema.firstSelectedGameObject = primeiroSelecionavel;
        sistema.SetSelectedGameObject(primeiroSelecionavel);
    }

    private void ConfirmarSaidaSemSalvar()
    {
        if (!PrepararContinuacaoAposRetornoAoMenu())
            Debug.LogWarning("PauseMenuManager: não foi possível guardar a sessão temporária para continuar.", this);
        RestaurarTempoEAudio();
        SceneManager.LoadScene("Demo1");
    }
}
