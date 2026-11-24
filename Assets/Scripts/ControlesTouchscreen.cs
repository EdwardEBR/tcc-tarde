using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

/// <summary>
/// Cria os controles de toque apenas em plataformas móveis e os conecta aos
/// dispositivos virtuais e às ações que o PlayerFPS já utiliza.
/// </summary>
public sealed class ControlesTouchscreen : MonoBehaviour
{
    private const int OrdemCanvas = 50;
    private static Sprite spriteCirculo;
    private static Sprite spriteContorno;

    private PlayerFPS jogador;
    private PauseMenuManager menuPausa;
    private GameObject raizControles;
    private RectTransform areaSegura;
    private Canvas canvasControles;
    private GameObject joystickBase;
    private GameObject joystickEntrada;
    private GameObject legendaJoystick;
    private Button botaoInteracao;
    private Button botaoMenu;
    private bool aplicativoAtivo = true;
    private bool ocultosParaCreditos;
    private Rect ultimaAreaSegura;
    private int ultimaLargura;
    private int ultimaAltura;

    private void Awake()
    {
        jogador = GetComponent<PlayerFPS>();
        menuPausa = GetComponent<PauseMenuManager>();
    }

    private void Start()
    {
        if (!Application.isMobilePlatform || jogador == null || menuPausa == null)
        {
            enabled = false;
            return;
        }

        CriarHUD();
        AtualizarAreaSegura(true);
    }

    private void LateUpdate()
    {
        if (areaSegura == null) return;

        AtualizarAreaSegura(false);
        bool jogoAtivo = aplicativoAtivo && !PauseMenuManager.JogoPausado && !ocultosParaCreditos;
        bool dialogoAtivo = jogador.ConversaAtiva;
        if (raizControles.activeSelf != jogoAtivo)
        {
            raizControles.SetActive(jogoAtivo);
            if (!jogoAtivo && jogador != null)
                jogador.CancelarOlharToque();
        }

        if (canvasControles != null)
            canvasControles.sortingOrder = dialogoAtivo ? -1 : OrdemCanvas;

        bool sentado = jogador.EstaSentadoNaCadeira;
        bool controlesDeMovimentoAtivos = jogoAtivo && !sentado;
        DefinirAtivo(joystickBase, controlesDeMovimentoAtivos);
        DefinirAtivo(joystickEntrada, controlesDeMovimentoAtivos);
        DefinirAtivo(legendaJoystick, controlesDeMovimentoAtivos);

        bool exibirBotaoInteracao = !sentado;
        bool exibirBotaoMenu = !dialogoAtivo;
        DefinirAtivo(botaoInteracao != null ? botaoInteracao.gameObject : null, exibirBotaoInteracao);
        DefinirAtivo(botaoMenu != null ? botaoMenu.gameObject : null, exibirBotaoMenu);

        if (botaoInteracao != null)
        {
            bool podeInteragir = jogoAtivo && exibirBotaoInteracao && jogador.PodeInteragirPelaMira;
            if (botaoInteracao.interactable != podeInteragir)
                botaoInteracao.interactable = podeInteragir;
        }
    }

    private void OnApplicationFocus(bool ganhouFoco)
    {
        aplicativoAtivo = ganhouFoco;
        if (!ganhouFoco && jogador != null)
            jogador.CancelarOlharToque();
        if (!ganhouFoco && raizControles != null)
            raizControles.SetActive(false);
    }

    private void OnApplicationPause(bool pausado)
    {
        aplicativoAtivo = !pausado;
        if (pausado && jogador != null)
            jogador.CancelarOlharToque();
        if (pausado && raizControles != null)
            raizControles.SetActive(false);
    }

    private void OnDestroy()
    {
        if (jogador != null)
            jogador.CancelarOlharToque();
    }

    public void OcultarParaCreditos()
    {
        ocultosParaCreditos = true;
        if (jogador != null)
            jogador.CancelarOlharToque();
        if (raizControles != null)
            raizControles.SetActive(false);
    }

    private void CriarHUD()
    {
        GameObject objetoCanvas = new GameObject(
            "Canvas_ControlesTouchscreen",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasControles = objetoCanvas.GetComponent<Canvas>();
        canvasControles.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasControles.sortingOrder = OrdemCanvas;

        CanvasScaler scaler = objetoCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        objetoCanvas.GetComponent<GraphicRaycaster>().blockingObjects = GraphicRaycaster.BlockingObjects.None;

        areaSegura = CriarRect(objetoCanvas.transform, "AreaSegura", Vector2.zero, Vector2.one);
        raizControles = areaSegura.gameObject;

        CriarZonaOlhar(areaSegura);
        CriarJoystick(areaSegura);
        CriarBotaoInteracao(areaSegura);
        CriarBotaoMenu(areaSegura);
        CriarLegendaMover(areaSegura);
    }

    private void CriarZonaOlhar(Transform pai)
    {
        Image zona = CriarImagem(pai, "ArrastarParaOlhar", null, new Color(1f, 1f, 1f, 0f));
        RectTransform rect = zona.rectTransform;
        rect.anchorMin = new Vector2(0.39f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        zona.raycastTarget = true;
        GestosCameraTouch gestos = zona.gameObject.AddComponent<GestosCameraTouch>();
        gestos.Configurar(jogador);
    }

    private void CriarJoystick(Transform pai)
    {
        RectTransform baseRect = CriarRect(pai, "Joystick_Base", new Vector2(0.145f, 0.18f), Vector2.zero);
        joystickBase = baseRect.gameObject;
        baseRect.anchorMax = baseRect.anchorMin;
        baseRect.sizeDelta = new Vector2(218f, 218f);
        Image fundo = baseRect.gameObject.AddComponent<Image>();
        fundo.sprite = ObterSpriteCirculo();
        fundo.color = new Color(0.035f, 0.035f, 0.03f, 0.36f);
        fundo.raycastTarget = false;

        Image contorno = CriarImagem(baseRect, "Contorno", ObterSpriteContorno(), new Color(0.91f, 0.57f, 0.12f, 0.62f));
        Esticar(contorno.rectTransform);
        contorno.raycastTarget = false;

        RectTransform zonaToque = CriarRect(pai, "Joystick_Entrada", new Vector2(0.145f, 0.18f), Vector2.zero);
        joystickEntrada = zonaToque.gameObject;
        zonaToque.anchorMax = zonaToque.anchorMin;
        zonaToque.sizeDelta = new Vector2(218f, 218f);
        Image imagemEntrada = zonaToque.gameObject.AddComponent<Image>();
        imagemEntrada.color = Color.clear;
        imagemEntrada.raycastTarget = true;

        RectTransform alca = CriarRect(zonaToque, "Alca", new Vector2(0.5f, 0.5f), Vector2.zero);
        alca.anchorMax = alca.anchorMin;
        alca.sizeDelta = new Vector2(82f, 82f);
        Image imagemAlca = alca.gameObject.AddComponent<Image>();
        imagemAlca.sprite = ObterSpriteCirculo();
        imagemAlca.color = new Color(0.94f, 0.62f, 0.18f, 0.72f);
        imagemAlca.raycastTarget = false;

        OnScreenStick joystick = zonaToque.gameObject.AddComponent<OnScreenStick>();
        joystick.controlPath = "<Gamepad>/leftStick";
        joystick.movementRange = 96f;
        joystick.behaviour = OnScreenStick.Behaviour.RelativePositionWithStaticOrigin;
        joystick.useIsolatedInputActions = true;
    }

    private void CriarBotaoInteracao(Transform pai)
    {
        RectTransform rect = CriarRect(pai, "Botao_Interagir", new Vector2(0.845f, 0.2f), Vector2.zero);
        rect.anchorMax = rect.anchorMin;
        rect.sizeDelta = new Vector2(168f, 168f);
        Image fundo = rect.gameObject.AddComponent<Image>();
        fundo.sprite = ObterSpriteCirculo();
        fundo.color = new Color(0.035f, 0.035f, 0.03f, 0.7f);
        fundo.raycastTarget = true;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = fundo;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.colors = CriarCoresBotao();
        button.onClick.AddListener(jogador.SolicitarInteracaoToque);
        botaoInteracao = button;

        Image contorno = CriarImagem(rect, "Contorno", ObterSpriteContorno(), Color.white);
        contorno.color = new Color(0.91f, 0.57f, 0.12f, 0.86f);
        Esticar(contorno.rectTransform);
        contorno.raycastTarget = false;

        CriarIconeMao(rect);
        CriarTexto(rect, "Legenda", "MÃO", 30f, new Vector2(0f, -54f), new Vector2(136f, 30f));

        button.interactable = jogador.PodeInteragirPelaMira;
    }

    private void CriarBotaoMenu(Transform pai)
    {
        RectTransform rect = CriarRect(pai, "Botao_Menu", new Vector2(0.94f, 0.88f), Vector2.zero);
        rect.anchorMax = rect.anchorMin;
        rect.sizeDelta = new Vector2(150f, 150f);
        Image fundo = rect.gameObject.AddComponent<Image>();
        fundo.sprite = ObterSpriteCirculo();
        fundo.color = new Color(0.035f, 0.035f, 0.03f, 0.58f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = fundo;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.colors = CriarCoresBotao();
        button.onClick.AddListener(menuPausa.ProcessarEscape);
        botaoMenu = button;

        Image contorno = CriarImagem(rect, "Contorno", ObterSpriteContorno(), new Color(0.91f, 0.57f, 0.12f, 0.64f));
        Esticar(contorno.rectTransform);
        contorno.raycastTarget = false;

        Color icone = new Color(0.95f, 0.88f, 0.74f, 0.96f);
        CriarImagemRetangulo(rect, "BarraPausa_1", new Vector2(-8f, 11f), new Vector2(8f, 30f), icone);
        CriarImagemRetangulo(rect, "BarraPausa_2", new Vector2(8f, 11f), new Vector2(8f, 30f), icone);
        CriarTexto(rect, "Legenda", "MENU", 26f, new Vector2(0f, -36f), new Vector2(100f, 30f));
    }

    private void CriarIconeMao(Transform pai)
    {
        Color ouro = new Color(0.97f, 0.68f, 0.26f, 1f);
        CriarImagemRetangulo(pai, "Mao_Palma", new Vector2(0f, 10f), new Vector2(48f, 32f), ouro);
        CriarImagemRetangulo(pai, "Mao_Dedo1", new Vector2(-18f, 28f), new Vector2(10f, 32f), ouro);
        CriarImagemRetangulo(pai, "Mao_Dedo2", new Vector2(-6f, 33f), new Vector2(10f, 42f), ouro);
        CriarImagemRetangulo(pai, "Mao_Dedo3", new Vector2(6f, 33f), new Vector2(10f, 42f), ouro);
        CriarImagemRetangulo(pai, "Mao_Dedo4", new Vector2(18f, 27f), new Vector2(10f, 31f), ouro);
        RectTransform polegar = CriarImagemRetangulo(pai, "Mao_Polegar", new Vector2(-26f, 1f), new Vector2(24f, 11f), ouro);
        polegar.localRotation = Quaternion.Euler(0f, 0f, -38f);
    }

    private void CriarLegendaMover(Transform pai)
    {
        CriarTexto(pai, "LegendaJoystick", "MOVER", 26f, Vector2.zero, new Vector2(150f, 32f),
            new Vector2(0.145f, 0.045f), new Vector2(0.145f, 0.045f));
        legendaJoystick = pai.Find("LegendaJoystick")?.gameObject;
    }

    private static void DefinirAtivo(GameObject objeto, bool ativo)
    {
        if (objeto != null && objeto.activeSelf != ativo)
            objeto.SetActive(ativo);
    }

    private static RectTransform CriarRect(Transform pai, string nome, Vector2 ancora, Vector2 posicao)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform));
        objeto.transform.SetParent(pai, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = ancora;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        return rect;
    }

    private static Image CriarImagem(Transform pai, string nome, Sprite sprite, Color cor)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        objeto.transform.SetParent(pai, false);
        Image image = objeto.GetComponent<Image>();
        image.sprite = sprite;
        image.color = cor;
        return image;
    }

    private static RectTransform CriarImagemRetangulo(Transform pai, string nome, Vector2 posicao, Vector2 tamanho, Color cor)
    {
        RectTransform rect = CriarRect(pai, nome, new Vector2(0.5f, 0.5f), posicao);
        rect.sizeDelta = tamanho;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = cor;
        image.raycastTarget = false;
        return rect;
    }

    private static void CriarTexto(
        Transform pai, string nome, string conteudo, float fonte, Vector2 posicao, Vector2 tamanho,
        Vector2? ancoraMinima = null, Vector2? ancoraMaxima = null)
    {
        Vector2 min = ancoraMinima ?? new Vector2(0.5f, 0.5f);
        Vector2 max = ancoraMaxima ?? min;
        RectTransform rect = CriarRect(pai, nome, min, posicao);
        rect.anchorMax = max;
        rect.sizeDelta = tamanho;
        TextMeshProUGUI texto = rect.gameObject.AddComponent<TextMeshProUGUI>();
        texto.font = TMP_Settings.defaultFontAsset;
        texto.text = conteudo;
        texto.fontSize = fonte;
        texto.color = new Color(0.97f, 0.93f, 0.85f, 0.92f);
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;
    }

    private static ColorBlock CriarCoresBotao()
    {
        ColorBlock cores = ColorBlock.defaultColorBlock;
        cores.normalColor = Color.white;
        cores.highlightedColor = new Color(1f, 0.85f, 0.59f, 1f);
        cores.pressedColor = new Color(0.88f, 0.57f, 0.19f, 1f);
        cores.selectedColor = cores.highlightedColor;
        cores.disabledColor = new Color(0.58f, 0.58f, 0.58f, 0.4f);
        cores.colorMultiplier = 1f;
        cores.fadeDuration = 0.08f;
        return cores;
    }

    private void AtualizarAreaSegura(bool forcado)
    {
        if (Screen.width <= 0 || Screen.height <= 0) return;
        Rect safe = Screen.safeArea;
        if (!forcado && safe == ultimaAreaSegura
            && ultimaLargura == Screen.width && ultimaAltura == Screen.height)
            return;

        ultimaAreaSegura = safe;
        ultimaLargura = Screen.width;
        ultimaAltura = Screen.height;
        areaSegura.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        areaSegura.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        areaSegura.offsetMin = Vector2.zero;
        areaSegura.offsetMax = Vector2.zero;
    }

    private static void Esticar(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Sprite ObterSpriteCirculo()
    {
        if (spriteCirculo != null) return spriteCirculo;
        spriteCirculo = CriarSpriteProcedural(false);
        return spriteCirculo;
    }

    private static Sprite ObterSpriteContorno()
    {
        if (spriteContorno != null) return spriteContorno;
        spriteContorno = CriarSpriteProcedural(true);
        return spriteContorno;
    }

    private static Sprite CriarSpriteProcedural(bool apenasContorno)
    {
        const int tamanho = 64;
        Texture2D textura = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false, true)
        {
            name = apenasContorno ? "ContornoCircularTouch" : "FundoCircularTouch",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        Color32[] pixels = new Color32[tamanho * tamanho];
        float centro = (tamanho - 1) * 0.5f;
        float raio = tamanho * 0.48f;
        for (int y = 0; y < tamanho; y++)
        {
            for (int x = 0; x < tamanho; x++)
            {
                float distancia = Vector2.Distance(new Vector2(x, y), new Vector2(centro, centro));
                float alfa = apenasContorno
                    ? Mathf.Clamp01(Mathf.Min(distancia - raio + 4f, raio - distancia + 1.5f))
                    : Mathf.Clamp01(raio - distancia + 1f);
                pixels[y * tamanho + x] = new Color(1f, 1f, 1f, alfa);
            }
        }
        textura.SetPixels32(pixels);
        textura.Apply(false, true);
        Sprite sprite = Sprite.Create(textura, new Rect(0f, 0f, tamanho, tamanho), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = textura.name + "Sprite";
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
