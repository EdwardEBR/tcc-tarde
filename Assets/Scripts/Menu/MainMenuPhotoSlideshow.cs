using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Exibe as capturas do projeto como um fundo em transição no menu principal.</summary>
public sealed class MainMenuPhotoSlideshow : MonoBehaviour
{
    private static readonly string[] NomesDasFotos =
    {
        "Background_01", "Background_02", "Background_03",
        "Background_04", "Background_05", "Background_06"
    };

    [SerializeField, Min(1f)] private float duracaoPorFoto = 6f;
    [SerializeField, Min(0.1f)] private float duracaoTransicao = 3f;
    private Camera cameraDoMenu;
    private Canvas canvasDaInterface;
    private Material materialFotos;
    private Material materialDesfoque;
    private Texture2D texturaDegradeEscuro;
    private RawImage faixaDesfocada;
    private RawImage faixaEscura;
    private RawImage escurecimentoUniforme;
    private bool desfoqueDisponivel;
    private bool contrasteAtivo = true;

    public void Inicializar(Camera camera, Canvas interfaceMenu)
    {
        cameraDoMenu = camera;
        canvasDaInterface = interfaceMenu;
    }

    private void Start()
    {
        if (cameraDoMenu == null)
            cameraDoMenu = Camera.main;
        if (cameraDoMenu == null)
        {
            Debug.LogWarning("MainMenuPhotoSlideshow: não encontrou a câmera do menu.", this);
            return;
        }

        Texture2D[] fotos = CarregarFotos();
        if (fotos.Length == 0)
        {
            Debug.LogWarning("MainMenuPhotoSlideshow: nenhuma imagem foi encontrada em Resources/MainMenuBackgrounds.", this);
            return;
        }

        StartCoroutine(ExibirFotos(fotos));
    }

    private static Texture2D[] CarregarFotos()
    {
        var carregadas = new System.Collections.Generic.List<Texture2D>();
        foreach (string nome in NomesDasFotos)
        {
            Texture2D textura = Resources.Load<Texture2D>("MainMenuBackgrounds/" + nome);
            if (textura != null) carregadas.Add(textura);
        }
        return carregadas.ToArray();
    }

    private IEnumerator ExibirFotos(Texture2D[] fotos)
    {
        Canvas canvas = new GameObject("Canvas_Fotos_Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cameraDoMenu;
        float distanciaUI = Mathf.Max(cameraDoMenu.nearClipPlane + 1f, 30f);
        if (canvasDaInterface != null && canvasDaInterface.renderMode == RenderMode.WorldSpace)
        {
            float profundidadeUI = cameraDoMenu.WorldToScreenPoint(canvasDaInterface.transform.position).z;
            if (profundidadeUI > cameraDoMenu.nearClipPlane)
                distanciaUI = Mathf.Max(distanciaUI, profundidadeUI + 3f);
        }
        float limiteDistancia = cameraDoMenu.farClipPlane - 0.1f;
        canvas.planeDistance = Mathf.Clamp(distanciaUI, cameraDoMenu.nearClipPlane + 0.1f, limiteDistancia);
        canvas.overrideSorting = true;
        canvas.sortingOrder = -100;

        CanvasScaler escalador = canvas.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        GameObject fundo = new GameObject("Fundo_Fotos", typeof(RectTransform));
        fundo.transform.SetParent(canvas.transform, false);
        RectTransform fundoRect = fundo.GetComponent<RectTransform>();
        fundoRect.anchorMin = Vector2.zero;
        fundoRect.anchorMax = Vector2.one;
        fundoRect.offsetMin = Vector2.zero;
        fundoRect.offsetMax = Vector2.zero;

        RawImage imagemAtual = CriarImagem(fundo.transform, "Foto_Atual");
        RawImage proximaImagem = CriarImagem(fundo.transform, "Próxima_Foto");
        faixaDesfocada = CriarImagem(fundo.transform, "Desfoque_Esquerdo");
        escurecimentoUniforme = CriarImagem(fundo.transform, "Escurecimento_Submenus");
        escurecimentoUniforme.texture = Texture2D.whiteTexture;
        escurecimentoUniforme.color = new Color(0.015f, 0.020f, 0.030f, 0.62f);
        int indiceAtual = 0;
        DefinirFoto(imagemAtual, fotos[indiceAtual]);
        imagemAtual.color = Color.white;
        proximaImagem.color = new Color(1f, 1f, 1f, 0f);
        faixaDesfocada.color = Color.white;
        DefinirFoto(faixaDesfocada, fotos[indiceAtual]);

        Shader shaderFotos = Resources.Load<Shader>("MainMenuBackgrounds/MenuPhotoBackground");
        if (shaderFotos == null) shaderFotos = Shader.Find("UI/MenuPhotoBackground");
        if (shaderFotos != null)
        {
            materialFotos = new Material(shaderFotos)
            {
                name = "Material_Fundo_Menu_Sem_Oclusao",
                hideFlags = HideFlags.HideAndDontSave
            };
            imagemAtual.material = materialFotos;
            proximaImagem.material = materialFotos;
            escurecimentoUniforme.material = materialFotos;
        }
        else
        {
            Debug.LogWarning("MainMenuPhotoSlideshow: shader de fundo não encontrado; usando o material padrão da UI.", this);
        }

        Shader shaderDesfoque = Resources.Load<Shader>("MainMenuBackgrounds/LeftMenuBackgroundBlur");
        if (shaderDesfoque == null) shaderDesfoque = Shader.Find("UI/LeftMenuBackgroundBlur");
        if (shaderDesfoque != null)
        {
            materialDesfoque = new Material(shaderDesfoque)
            {
                name = "Material_Desfoque_Escuro_Menu",
                hideFlags = HideFlags.HideAndDontSave
            };
            materialDesfoque.SetFloat("_BlurSize", 4.5f);
            materialDesfoque.SetFloat("_Darken", 0.24f);
            materialDesfoque.SetFloat("_FullScreen", 0f);
            materialDesfoque.SetTexture("_NextTex", fotos[indiceAtual]);
            materialDesfoque.SetFloat("_Blend", 0f);
            faixaDesfocada.material = materialDesfoque;
            desfoqueDisponivel = true;
        }
        else
        {
            faixaDesfocada.gameObject.SetActive(false);
            Debug.LogWarning("MainMenuPhotoSlideshow: shader de desfoque não encontrado; mantendo apenas o degradê escuro.", this);
        }

        faixaEscura = CriarImagem(fundo.transform, "Degrade_Escuro_Esquerdo");
        AspectRatioFitter ajusteFaixa = faixaEscura.GetComponent<AspectRatioFitter>();
        if (ajusteFaixa != null) ajusteFaixa.enabled = false;
        RectTransform rectFaixa = faixaEscura.rectTransform;
        rectFaixa.anchorMin = Vector2.zero;
        rectFaixa.anchorMax = Vector2.one;
        rectFaixa.offsetMin = Vector2.zero;
        rectFaixa.offsetMax = Vector2.zero;
        faixaEscura.texture = CriarTexturaDegradeEscuro();
        faixaEscura.color = Color.white;
        if (materialFotos != null)
            faixaEscura.material = materialFotos;
        DefinirContrasteAtivo(contrasteAtivo);

        if (fotos.Length == 1) yield break;

        while (true)
        {
            yield return new WaitForSecondsRealtime(duracaoPorFoto);
            int proximoIndice = (indiceAtual + 1) % fotos.Length;
            DefinirFoto(proximaImagem, fotos[proximoIndice]);
            proximaImagem.color = new Color(1f, 1f, 1f, 0f);
            if (materialDesfoque != null)
                materialDesfoque.SetTexture("_NextTex", fotos[proximoIndice]);

            float tempo = 0f;
            while (tempo < duracaoTransicao)
            {
                tempo += Time.unscaledDeltaTime;
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tempo / duracaoTransicao));
                imagemAtual.color = new Color(1f, 1f, 1f, 1f - alpha);
                proximaImagem.color = new Color(1f, 1f, 1f, alpha);
                if (materialDesfoque != null) materialDesfoque.SetFloat("_Blend", alpha);
                yield return null;
            }

            indiceAtual = proximoIndice;
            RawImage temporaria = imagemAtual;
            imagemAtual = proximaImagem;
            proximaImagem = temporaria;
            imagemAtual.color = Color.white;
            proximaImagem.color = new Color(1f, 1f, 1f, 0f);
            DefinirFoto(faixaDesfocada, fotos[indiceAtual]);
            if (materialDesfoque != null)
            {
                materialDesfoque.SetTexture("_NextTex", fotos[indiceAtual]);
                materialDesfoque.SetFloat("_Blend", 0f);
            }
        }
    }

    private void OnDestroy()
    {
        if (materialFotos != null) Destroy(materialFotos);
        if (materialDesfoque != null) Destroy(materialDesfoque);
        if (texturaDegradeEscuro != null) Destroy(texturaDegradeEscuro);
    }

    public void DefinirContrasteAtivo(bool ativo)
    {
        contrasteAtivo = ativo;
        if (materialDesfoque != null)
            materialDesfoque.SetFloat("_FullScreen", ativo ? 0f : 1f);
        if (escurecimentoUniforme != null)
        {
            escurecimentoUniforme.gameObject.SetActive(!ativo);
            if (!ativo)
                escurecimentoUniforme.transform.SetAsLastSibling();
        }
        if (faixaDesfocada != null)
            faixaDesfocada.gameObject.SetActive(desfoqueDisponivel);
        if (faixaEscura != null)
        {
            faixaEscura.gameObject.SetActive(ativo);
            if (ativo)
                faixaEscura.transform.SetAsLastSibling();
        }
    }

    private Texture2D CriarTexturaDegradeEscuro()
    {
        const int largura = 512;
        texturaDegradeEscuro = new Texture2D(largura, 2, TextureFormat.RGBA32, false, true)
        {
            name = "Degrade_Escuro_Menu",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color[] pixels = new Color[largura * 2];
        for (int x = 0; x < largura; x++)
        {
            float posicao = (float)x / (largura - 1);
            float t = Mathf.Clamp01((posicao - 0.30f) / (0.56f - 0.30f));
            float suavizado = t * t * (3f - 2f * t);
            float alpha = (1f - suavizado) * 0.84f;
            Color cor = new Color(0.008f, 0.012f, 0.020f, alpha);
            pixels[x] = cor;
            pixels[x + largura] = cor;
        }

        texturaDegradeEscuro.SetPixels(pixels);
        texturaDegradeEscuro.Apply(false, true);
        return texturaDegradeEscuro;
    }

    private static RawImage CriarImagem(Transform pai, string nome)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(AspectRatioFitter));
        objeto.transform.SetParent(pai, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        RawImage imagem = objeto.GetComponent<RawImage>();
        imagem.raycastTarget = false;
        AspectRatioFitter proporcao = objeto.GetComponent<AspectRatioFitter>();
        proporcao.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        return imagem;
    }

    private static void DefinirFoto(RawImage imagem, Texture2D textura)
    {
        imagem.texture = textura;
        imagem.GetComponent<AspectRatioFitter>().aspectRatio = (float)textura.width / textura.height;
    }
}
