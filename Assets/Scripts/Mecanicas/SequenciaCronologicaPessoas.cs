using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SequenciaCronologicaPessoas : MonoBehaviour
{
    private const float DuracaoFadeTrilhaFinal = 3f;
    [Min(0.05f)] public float duracaoEsmaecimento = 0.8f;
    [Min(0f)] public float atrasoInicial = 3f;
    [Min(0f)] public float pausaEntrePessoas = 3f;
    [Min(0f)] public float esperaAntesDosCreditos = 4f;
    [Min(0.05f)] public float duracaoTelaPreta = 1.5f;

    private readonly List<Renderer[]> pessoas = new();
    private readonly List<ShadowCastingMode[]> sombras = new();
    private readonly List<MaterialFadeState[]> materiais = new();
    private bool iniciada;

    private void Awake()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Renderer[] renderizadores = transform.GetChild(i).GetComponentsInChildren<Renderer>(true);
            pessoas.Add(renderizadores);

            var sombrasPessoa = new ShadowCastingMode[renderizadores.Length];
            var materiaisPessoa = new List<MaterialFadeState>();
            for (int r = 0; r < renderizadores.Length; r++)
            {
                Renderer renderizador = renderizadores[r];
                sombrasPessoa[r] = renderizador.shadowCastingMode;
                renderizador.enabled = false;
                renderizador.shadowCastingMode = ShadowCastingMode.Off;
                foreach (Material material in renderizador.materials)
                {
                    var estadoMaterial = new MaterialFadeState(material);
                    PrepararMaterial(material);
                    DefinirAlpha(material, 0f);
                    materiaisPessoa.Add(estadoMaterial);
                }
            }
            sombras.Add(sombrasPessoa);
            materiais.Add(materiaisPessoa.ToArray());
        }
    }

    public void Iniciar()
    {
        if (iniciada || pessoas.Count == 0) return;
        iniciada = true;
        StartCoroutine(ExibirEmOrdem());
    }

    private IEnumerator ExibirEmOrdem()
    {
        if (atrasoInicial > 0f)
            yield return new WaitForSeconds(atrasoInicial);

        for (int i = 0; i < pessoas.Count; i++)
        {
            foreach (Renderer renderizador in pessoas[i]) renderizador.enabled = true;
            yield return Esmaecer(materiais[i], 0f, 1f, duracaoEsmaecimento);

            // Restaura o modo opaco original quando a pessoa termina de aparecer,
            // para que os meshes sobrepostos voltem a usar depth write corretamente.
            foreach (MaterialFadeState estado in materiais[i]) estado.Restaurar();
            for (int r = 0; r < pessoas[i].Length; r++)
                pessoas[i][r].shadowCastingMode = sombras[i][r];

            if (i < pessoas.Count - 1 && pausaEntrePessoas > 0f)
                yield return new WaitForSeconds(pausaEntrePessoas);
        }

        if (esperaAntesDosCreditos > 0f)
            yield return new WaitForSeconds(esperaAntesDosCreditos);
        yield return StartCoroutine(ExibirCreditos());
    }

    private IEnumerator ExibirCreditos()
    {
        PauseMenuManager menuPausa = FindAnyObjectByType<PauseMenuManager>();
        if (menuPausa != null) menuPausa.enabled = false;
        MiraCircularUI mira = FindAnyObjectByType<MiraCircularUI>();
        if (mira != null) mira.gameObject.SetActive(false);

        GameObject canvasObject = new GameObject("CanvasCreditosFinal", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject backgroundObject = new GameObject("TelaPretaCreditos", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backgroundObject.transform.SetParent(canvasObject.transform, false);
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        Image background = backgroundObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0f);
        background.raycastTarget = true;

        float tempoEscurecer = 0f;
        while (tempoEscurecer < duracaoTelaPreta)
        {
            tempoEscurecer += Time.unscaledDeltaTime;
            Color cor = background.color;
            cor.a = Mathf.Clamp01(tempoEscurecer / duracaoTelaPreta);
            background.color = cor;
            yield return null;
        }
        background.color = Color.black;

        Camera cameraFundo = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        if (cameraFundo != null)
        {
            MainMenuPhotoSlideshow fotosDeFundo = new GameObject("Fotos_Fundo_Creditos")
                .AddComponent<MainMenuPhotoSlideshow>();
            fotosDeFundo.Inicializar(cameraFundo, canvas);
            fotosDeFundo.DefinirContrasteAtivo(false);
            yield return null;
        }

        GameObject textObject = new GameObject("Creditos", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(canvasObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.08f, 0f);
        textRect.anchorMax = new Vector2(0.92f, 0f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.sizeDelta = new Vector2(0f, 0f);

        TextMeshProUGUI creditText = textObject.GetComponent<TextMeshProUGUI>();
        creditText.font = TMP_Settings.defaultFontAsset;
        creditText.fontSize = 48f;
        creditText.color = Color.white;
        creditText.alpha = 0f;
        creditText.alignment = TextAlignmentOptions.Center;
        creditText.textWrappingMode = TextWrappingModes.Normal;
        creditText.lineSpacing = 8f;
        string destaque = ColorUtility.ToHtmlStringRGB(new Color(1f, 0.686f, 0f));
        string tituloSecao = "<size=52><b><color=#" + destaque + ">";
        string fimTituloSecao = "</color></b></size>\n\n";
        string tituloFuncao = "<size=44><b><color=#D6A647>";
        string fimTituloFuncao = "</color></b></size>\n";
        string nomes = "<size=44><color=#FFE5A3>";
        string fimNomes = "</color></size>\n\n";

        creditText.text =
            "<size=98><b><color=#" + destaque + ">MEMÓRIAS PARTIDAS</color></b></size>\n" +
            "<size=42><b><color=#FFE5A3>TRABALHO DE CONCLUSÃO DE CURSO (TCC)</color></b></size>\n\n" +
            "<size=46><color=#FFE5A3>Uma história sobre memória, tempo e identidade,\n" +
            "protagonizada por Dona Helena.</color></size>\n\n\n" +

            tituloSecao + "EQUIPE DE DESENVOLVIMENTO" + fimTituloSecao +
            nomes +
            "Eduardo Pinto Pontes\n" +
            "Felipe Cyller Santana Santos\n" +
            "Diogo Neto Fernandes\n" +
            "Gabriel Antonio da Silva Campos\n" +
            "Gabriely Barbosa Figueiredo" + fimNomes +

            tituloSecao + "FUNÇÕES E ÁREAS" + fimTituloSecao +
            tituloFuncao + "PROGRAMAÇÃO E MECÂNICAS" + fimTituloFuncao +
            nomes + "Eduardo Pinto Pontes" + fimNomes +
            tituloFuncao + "GAME DESIGN E NARRATIVA" + fimTituloFuncao +
            nomes +
            "Diogo Neto Fernandes\n" +
            "Eduardo Pinto Pontes\n" +
            "Felipe Cyller Santana Santos\n" +
            "Gabriel Antonio da Silva Campos\n" +
            "Gabriely Barbosa Figueiredo" + fimNomes +
            tituloFuncao + "MODELAGEM 3D, CENÁRIOS E PROPS" + fimTituloFuncao +
            nomes +
            "Diogo Neto Fernandes\n" +
            "Eduardo Pinto Pontes\n" +
            "Gabriely Barbosa Figueiredo" + fimNomes +
            tituloFuncao + "ARTE 2D, UI E IDENTIDADE VISUAL" + fimTituloFuncao +
            nomes +
            "Diogo Neto Fernandes\n" +
            "Eduardo Pinto Pontes\n" +
            "Gabriely Barbosa Figueiredo" + fimNomes +
            tituloFuncao + "ÁUDIO, EFEITOS SONOROS E TRILHA" + fimTituloFuncao +
            nomes + "Gabriel Antonio da Silva Campos\nEduardo Pinto Pontes" + fimNomes +
            tituloFuncao + "TESTES, BALANCEAMENTO E QA" + fimTituloFuncao +
            nomes + "Eduardo Pinto Pontes" + fimNomes +
            tituloFuncao + "ARTIGO CIENTÍFICO" + fimTituloFuncao +
            nomes +
            "Felipe Cyller Santana Santos\n" +
            "Diogo Neto Fernandes\n" +
            "Gabriely Barbosa Figueiredo" + fimNomes +
            tituloFuncao + "DOCUMENTAÇÃO DO JOGO (GDD / DOCUMENTAÇÃO TÉCNICA)" + fimTituloFuncao +
            nomes +
            "Felipe Cyller Santana Santos\n" +
            "Diogo Neto Fernandes\n" +
            "Eduardo Pinto Pontes\n" +
            "Gabriely Barbosa Figueiredo" + fimNomes +

            tituloSecao + "ORIENTAÇÃO E INSTITUIÇÃO" + fimTituloSecao +
            tituloFuncao + "ORIENTADORA" + fimTituloFuncao +
            nomes + "Andressa Cristina Passos Santos" + fimNomes +
            tituloFuncao + "COORIENTAÇÃO / BANCA" + fimTituloFuncao +
            nomes + "Daniel Minoh Giampaulo\nMario da Silva de Jesus" + fimNomes +
            tituloFuncao + "INSTITUIÇÃO" + fimTituloFuncao +
            nomes + "Etec Profª Ermelinda Giannini Teixeira\n" +
            "Curso Técnico em Programação de Jogos Digitais" + fimNomes +

            tituloSecao + "FERRAMENTAS E TECNOLOGIAS" + fimTituloSecao +
            tituloFuncao + "ENGINE E DESENVOLVIMENTO" + fimTituloFuncao +
            nomes + "Unity" + fimNomes +
            tituloFuncao + "MODELAGEM E ARTE" + fimTituloFuncao +
            nomes + "Blender\nCanva" + fimNomes +
            tituloFuncao + "DOCUMENTAÇÃO E GESTÃO" + fimTituloFuncao +
            nomes + "GitHub\nGoogle Docs\nGoogle Planilhas\nWord\nCanva" + fimNomes +

            tituloSecao + "ASSETS E RECURSOS DE TERCEIROS" + fimTituloSecao +
            tituloFuncao + "MODELOS, TEXTURAS E MATERIAIS" + fimTituloFuncao +
            nomes + "assetstore.unity.com" + fimNomes +
            tituloFuncao + "ÁUDIO, EFEITOS E MÚSICA" + fimTituloFuncao +
            nomes + "pixabay.com\nyoutube.com" + fimNomes +
            tituloFuncao + "FONTES TIPOGRÁFICAS E PLUGINS" + fimTituloFuncao +
            nomes + "assetstore.unity.com" + fimNomes +

            tituloSecao + "AGRADECIMENTOS ESPECIAIS" + fimTituloSecao +
            "<size=44><color=#FFE5A3>Aos professores da ETEC pelo apoio, revisão e\n" +
            "orientação ao longo do desenvolvimento do jogo,\n" +
            "da documentação e do artigo científico.\n\n" +
            "Aos familiares, amigos e colegas que testaram o jogo,\n" +
            "deram feedback e estiveram presentes em cada etapa.\n\n" +
            "E a todos que jogaram e vivenciaram a história\n" +
            "de Dona Helena.</color></size>";
        creditText.ForceMeshUpdate();
        float alturaCreditos = creditText.preferredHeight;
        textRect.sizeDelta = new Vector2(0f, alturaCreditos);
        float alturaTela = scaler.referenceResolution.y;
        textRect.anchoredPosition = new Vector2(0f, -alturaCreditos);
        creditText.ForceMeshUpdate(false, true);
        Vector3[][] verticesOriginais = CopiarVertices(creditText);
        AplicarPerspectivaStarWars(creditText, alturaTela, verticesOriginais);

        float tempoFade = 0f;
        while (tempoFade < duracaoTelaPreta)
        {
            tempoFade += Time.unscaledDeltaTime;
            Color cor = background.color;
            cor.a = 1f - 0.62f * Mathf.Clamp01(tempoFade / duracaoTelaPreta);
            background.color = cor;
            creditText.alpha = Mathf.Clamp01(tempoFade / duracaoTelaPreta);
            yield return null;
        }
        background.color = new Color(0f, 0f, 0f, 0.38f);
        creditText.alpha = 1f;

        const float velocidadeRolagemLegivel = 75f;
        float distanciaRolagem = alturaCreditos + alturaTela;
        float duracao = distanciaRolagem / velocidadeRolagemLegivel;
        float duracaoFadeMusica = Mathf.Min(DuracaoFadeTrilhaFinal, duracao);
        float tempoRolagem = 0f;
        bool fadeMusicaIniciado = false;
        while (tempoRolagem < duracao)
        {
            tempoRolagem += Time.deltaTime;
            float progresso = Mathf.Clamp01(tempoRolagem / duracao);
            textRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(-alturaCreditos, alturaTela, progresso));
            RestaurarVerticesOriginais(creditText, verticesOriginais);
            AplicarPerspectivaStarWars(creditText, alturaTela, verticesOriginais);
            if (!fadeMusicaIniciado && tempoRolagem >= duracao - duracaoFadeMusica)
            {
                fadeMusicaIniciado = true;
                MusicaNivel5.IniciarFadeSaida(duracaoFadeMusica);
            }
            yield return null;
        }
        textRect.anchoredPosition = new Vector2(0f, alturaTela);
        while (MusicaNivel5.FadeSaidaEmAndamento)
            yield return null;

        creditText.alpha = 0f;
        GameObject mensagemFinalObject = new GameObject(
            "MensagemFinalCreditos",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        mensagemFinalObject.transform.SetParent(canvasObject.transform, false);
        RectTransform mensagemFinalRect = mensagemFinalObject.GetComponent<RectTransform>();
        mensagemFinalRect.anchorMin = new Vector2(0.03f, 0.12f);
        mensagemFinalRect.anchorMax = new Vector2(0.97f, 0.88f);
        mensagemFinalRect.offsetMin = Vector2.zero;
        mensagemFinalRect.offsetMax = Vector2.zero;
        TextMeshProUGUI mensagemFinal = mensagemFinalObject.GetComponent<TextMeshProUGUI>();
        mensagemFinal.font = creditText.font;
        mensagemFinal.text = "OBRIGADO POR JOGAR!";
        mensagemFinal.fontSize = 180f;
        mensagemFinal.fontSizeMin = 96f;
        mensagemFinal.fontSizeMax = 180f;
        mensagemFinal.enableAutoSizing = true;
        mensagemFinal.textWrappingMode = TextWrappingModes.NoWrap;
        mensagemFinal.alignment = TextAlignmentOptions.Center;
        mensagemFinal.fontStyle = FontStyles.Bold;
        mensagemFinal.color = new Color(1f, 0.686f, 0f);
        mensagemFinal.alpha = 0f;

        const float duracaoAparicaoMensagemFinal = 0.9f;
        float tempoAparicaoMensagemFinal = 0f;
        while (tempoAparicaoMensagemFinal < duracaoAparicaoMensagemFinal)
        {
            tempoAparicaoMensagemFinal += Time.unscaledDeltaTime;
            mensagemFinal.alpha = Mathf.Clamp01(tempoAparicaoMensagemFinal / duracaoAparicaoMensagemFinal);
            yield return null;
        }
        mensagemFinal.alpha = 1f;
        yield return new WaitForSecondsRealtime(2f);

        SaveGameDatabase.Delete();
        TransicaoSuaveMenu.CarregarComTransicao("Demo1", 1.25f, 1.1f);
    }

    private static Vector3[][] CopiarVertices(TMP_Text texto)
    {
        TMP_MeshInfo[] malhas = texto.textInfo.meshInfo;
        Vector3[][] copia = new Vector3[malhas.Length][];
        for (int i = 0; i < malhas.Length; i++)
            copia[i] = (Vector3[])malhas[i].vertices.Clone();
        return copia;
    }

    private static void RestaurarVerticesOriginais(TMP_Text texto, Vector3[][] verticesOriginais)
    {
        TMP_MeshInfo[] malhas = texto.textInfo.meshInfo;
        for (int i = 0; i < malhas.Length; i++)
            System.Array.Copy(verticesOriginais[i], malhas[i].vertices, verticesOriginais[i].Length);
    }

    private static void AplicarPerspectivaStarWars(TMP_Text texto, float alturaTela, Vector3[][] verticesOriginais)
    {
        TMP_TextInfo informacoes = texto.textInfo;
        float deslocamentoVertical = texto.rectTransform.anchoredPosition.y;
        float horizonte = alturaTela * 1.2f;
        const float escalaNoHorizonte = 0.24f;
        const float compressaoVertical = 0.7f;
        const float inclinacaoNoHorizonte = 0.48f;

        for (int linhaIndice = 0; linhaIndice < informacoes.lineCount; linhaIndice++)
        {
            TMP_LineInfo linha = informacoes.lineInfo[linhaIndice];
            float yLinhaNaTela = deslocamentoVertical + linha.baseline;
            float yLinhaProjetado = yLinhaNaTela;
            float escalaLinha = 1f;
            float escalaVerticalLinha = 1f;

            if (yLinhaNaTela > 0f)
            {
                float alturaRelativa = Mathf.Clamp01(yLinhaNaTela / horizonte);
                escalaLinha = Mathf.Lerp(1f, escalaNoHorizonte, alturaRelativa);
                escalaVerticalLinha = escalaLinha * Mathf.Lerp(1f, inclinacaoNoHorizonte, alturaRelativa);
                if (yLinhaNaTela <= horizonte)
                {
                    float alturaComprimida = alturaRelativa * (1f + compressaoVertical)
                        / (1f + compressaoVertical * alturaRelativa);
                    yLinhaProjetado = alturaComprimida * horizonte;
                }
                else
                {
                    yLinhaProjetado = horizonte + (yLinhaNaTela - horizonte) * escalaNoHorizonte;
                }
            }

            float centroXLinha = (linha.lineExtents.min.x + linha.lineExtents.max.x) * 0.5f;
            int primeiroCaractere = Mathf.Max(0, linha.firstCharacterIndex);
            int ultimoCaractere = Mathf.Min(informacoes.characterCount - 1, linha.lastCharacterIndex);
            for (int caractereIndice = primeiroCaractere; caractereIndice <= ultimoCaractere; caractereIndice++)
            {
                TMP_CharacterInfo caractere = informacoes.characterInfo[caractereIndice];
                if (!caractere.isVisible) continue;

                int indiceMaterial = caractere.materialReferenceIndex;
                int indiceVertice = caractere.vertexIndex;
                Vector3[] vertices = informacoes.meshInfo[indiceMaterial].vertices;
                Vector3[] verticesBase = verticesOriginais[indiceMaterial];
                for (int canto = 0; canto < 4; canto++)
                {
                    int indice = indiceVertice + canto;
                    Vector3 vertice = verticesBase[indice];
                    vertice.x = centroXLinha + (vertice.x - centroXLinha) * escalaLinha;
                    vertice.y = yLinhaProjetado - deslocamentoVertical
                        + (vertice.y - linha.baseline) * escalaVerticalLinha;
                    vertices[indice] = vertice;
                }
            }
        }

        texto.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    private static IEnumerator Esmaecer(MaterialFadeState[] materiaisPessoa, float inicio, float fim, float duracao)
    {
        if (duracao <= 0f)
        {
            foreach (MaterialFadeState estado in materiaisPessoa) DefinirAlpha(estado.material, fim);
            yield break;
        }

        float tempo = 0f;
        while (tempo < duracao)
        {
            tempo += Time.deltaTime;
            float alpha = Mathf.Lerp(inicio, fim, Mathf.Clamp01(tempo / duracao));
            foreach (MaterialFadeState estado in materiaisPessoa) DefinirAlpha(estado.material, alpha);
            yield return null;
        }
        foreach (MaterialFadeState estado in materiaisPessoa) DefinirAlpha(estado.material, fim);
    }

    private sealed class MaterialFadeState
    {
        private static readonly string[] floatProperties =
        {
            "_Surface", "_Blend", "_SrcBlend", "_DstBlend", "_SrcBlendAlpha",
            "_DstBlendAlpha", "_ZWrite", "_AlphaClip", "_Cutoff"
        };
        private static readonly string[] keywords =
        {
            "_SURFACE_TYPE_TRANSPARENT", "_ALPHATEST_ON", "_ALPHAPREMULTIPLY_ON"
        };

        public readonly Material material;
        private readonly int renderQueue;
        private readonly string renderType;
        private readonly Dictionary<string, float> savedFloats = new();
        private readonly bool[] savedKeywords = new bool[keywords.Length];
        private readonly bool hasBaseColor;
        private readonly bool hasColor;
        private readonly Color baseColor;
        private readonly Color color;

        public MaterialFadeState(Material material)
        {
            this.material = material;
            renderQueue = material.renderQueue;
            renderType = material.GetTag("RenderType", false, "");
            foreach (string property in floatProperties)
                if (material.HasProperty(property)) savedFloats[property] = material.GetFloat(property);
            for (int i = 0; i < keywords.Length; i++)
                savedKeywords[i] = material.IsKeywordEnabled(keywords[i]);

            hasBaseColor = material.HasProperty("_BaseColor");
            hasColor = material.HasProperty("_Color");
            if (hasBaseColor) baseColor = material.GetColor("_BaseColor");
            if (hasColor) color = material.GetColor("_Color");
        }

        public void Restaurar()
        {
            foreach (KeyValuePair<string, float> property in savedFloats)
                material.SetFloat(property.Key, property.Value);
            for (int i = 0; i < keywords.Length; i++)
            {
                if (savedKeywords[i]) material.EnableKeyword(keywords[i]);
                else material.DisableKeyword(keywords[i]);
            }
            if (hasBaseColor) material.SetColor("_BaseColor", baseColor);
            if (hasColor) material.SetColor("_Color", color);
            material.SetOverrideTag("RenderType", renderType);
            material.renderQueue = renderQueue;
        }
    }

    private static void PrepararMaterial(Material material)
    {
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_SrcBlendAlpha")) material.SetInt("_SrcBlendAlpha", (int)BlendMode.One);
        if (material.HasProperty("_DstBlendAlpha")) material.SetInt("_DstBlendAlpha", (int)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
        if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0f);

        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private static void DefinirAlpha(Material material, float alpha)
    {
        if (material.HasProperty("_BaseColor"))
        {
            Color cor = material.GetColor("_BaseColor");
            cor.a = alpha;
            material.SetColor("_BaseColor", cor);
        }
        if (material.HasProperty("_Color"))
        {
            Color cor = material.GetColor("_Color");
            cor.a = alpha;
            material.SetColor("_Color", cor);
        }
    }
}
