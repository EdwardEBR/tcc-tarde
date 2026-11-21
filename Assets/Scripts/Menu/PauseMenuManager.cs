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

    public static bool JogoPausado { get; private set; }

    private PlayerFPS jogador;
    private GameObject canvasMenu;
    private GameObject canvasOpcoes;
    private GameObject painelPrincipal;
    private GameObject painelOpcoes;
    private TextMeshProUGUI textoStatus;
    private Slider sliderVolume;
    private Slider sliderSensibilidade;
    private float proximoStatus;

    private void Awake()
    {
        jogador = GetComponent<PlayerFPS>();
        CriarEventSystemSeNecessario();
        CriarMenu();
        RestaurarCheckpointSeDisponivel();
    }

    private void Update()
    {
        if (proximoStatus > 0f && Time.unscaledTime >= proximoStatus)
        {
            textoStatus.text = string.Empty;
            proximoStatus = 0f;
        }

        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (!JogoPausado)
            AbrirMenu();
        else if (canvasOpcoes != null && canvasOpcoes.activeSelf)
            MostrarMenuPrincipal();
        else
            RetomarJogo();
    }

    private void OnDestroy()
    {
        if (JogoPausado)
        {
            JogoPausado = false;
            Time.timeScale = 1f;
        }
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    private void CriarEventSystemSeNecessario()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem (Pause Menu)", typeof(EventSystem));
        eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
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

        painelPrincipal = CriarPainel(canvasMenu.transform, "Painel_Pausa", new Vector2(600f, 620f));
        CriarTexto(painelPrincipal.transform, "Título", "PAUSADO", 42f, new Vector2(0f, 240f), new Vector2(500f, 70f));
        CriarBotao(painelPrincipal.transform, "Salvar jogo", new Vector2(0f, 130f), SalvarJogo);
        CriarBotao(painelPrincipal.transform, "Configurações", new Vector2(0f, 40f), MostrarOpcoes);
        CriarBotao(painelPrincipal.transform, "Resume", new Vector2(0f, -50f), RetomarJogo);
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
        painel.GetComponent<Image>().color = new Color(0.035f, 0.045f, 0.065f, 0.96f);
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
        texto.font = TMP_Settings.defaultFontAsset;
        texto.text = conteudo;
        texto.fontSize = tamanhoFonte;
        texto.fontStyle = FontStyles.Bold;
        texto.color = Color.white;
        texto.alignment = TextAlignmentOptions.Center;
        return texto;
    }

    private static Button CriarBotao(Transform pai, string rotulo, Vector2 posicao, Action acao)
    {
        GameObject objeto = new GameObject("Botão_" + rotulo, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        objeto.transform.SetParent(pai, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = new Vector2(390f, 64f);
        Image imagem = objeto.GetComponent<Image>();
        imagem.color = new Color(0.16f, 0.24f, 0.34f, 1f);
        Button botao = objeto.GetComponent<Button>();
        botao.targetGraphic = imagem;
        botao.onClick.AddListener(() => acao());

        ColorBlock cores = botao.colors;
        cores.highlightedColor = new Color(0.25f, 0.45f, 0.62f, 1f);
        cores.pressedColor = new Color(0.1f, 0.19f, 0.28f, 1f);
        botao.colors = cores;
        CriarTexto(objeto.transform, "Rótulo", rotulo, 24f, Vector2.zero, new Vector2(370f, 58f));
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

        GameObject preenchimento = CriarImagemUI(raiz.transform, "Preenchimento", new Color(0.2f, 0.62f, 0.9f, 1f));
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
        JogoPausado = true;
        Time.timeScale = 0f;
        painelPrincipal.SetActive(true);
        canvasOpcoes.SetActive(false);
        canvasMenu.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void MostrarOpcoes()
    {
        canvasMenu.SetActive(false);
        canvasOpcoes.SetActive(true);
    }

    private void MostrarMenuPrincipal()
    {
        canvasOpcoes.SetActive(false);
        canvasMenu.SetActive(true);
    }

    private void RetomarJogo()
    {
        JogoPausado = false;
        Time.timeScale = 1f;
        canvasMenu.SetActive(false);
        canvasOpcoes.SetActive(false);
        if (jogador != null) jogador.RestaurarCursorAposPausa();
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void SalvarJogo()
    {
        if (jogador == null) return;

        string cena = SceneManager.GetActiveScene().name;
        Vector3 posicao = jogador.transform.position;
        Vector3 rotacao = jogador.transform.eulerAngles;
        SaveGameDatabase.Save(new SaveGameDatabase.SaveData
        {
            sceneName = cena,
            x = posicao.x, y = posicao.y, z = posicao.z,
            rotX = rotacao.x, rotY = rotacao.y, rotZ = rotacao.z,
            sensitivity = jogador.sensibilidade,
            volume = AudioListener.volume
        });
        PlayerPrefs.Save();

        textoStatus.text = "Jogo salvo nesta cena.";
        proximoStatus = Time.unscaledTime + 3f;
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
        JogoPausado = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Demo1");
    }
}
