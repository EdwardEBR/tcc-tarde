using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SequenciaCronologicaPessoas : MonoBehaviour
{
    [Min(0.05f)] public float duracaoEsmaecimento = 0.8f;
    [Min(0f)] public float atrasoInicial = 3f;
    [Min(0f)] public float pausaEntrePessoas = 3f;
    [Min(0f)] public float esperaAntesDosCreditos = 4f;
    [Min(0.05f)] public float duracaoTelaPreta = 1.5f;
    [Min(0.1f)] public float duracaoRolagemCreditos = 40f;

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

        GameObject textObject = new GameObject("Creditos", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(canvasObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.08f, 0f);
        textRect.anchorMax = new Vector2(0.92f, 0f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.sizeDelta = new Vector2(0f, 0f);

        TextMeshProUGUI creditText = textObject.GetComponent<TextMeshProUGUI>();
        creditText.font = TMP_Settings.defaultFontAsset;
        creditText.fontSize = 44f;
        creditText.color = Color.white;
        creditText.alignment = TextAlignmentOptions.Center;
        creditText.textWrappingMode = TextWrappingModes.Normal;
        creditText.lineSpacing = 8f;
        string destaque = ColorUtility.ToHtmlStringRGB(new Color(1f, 0.686f, 0f));
        creditText.text =
            "<size=64><b>CRÉDITOS</b></size>\n\n" +
            "<color=#" + destaque + "><b>DIREÇÃO E PRODUÇÃO</b></color>\n" +
            "Gerente de Projeto / Produção: Eduardo\nGame Design: Eduardo\nLevel Design: Eduardo\n\n" +
            "<color=#" + destaque + "><b>ROTEIRO E DOCUMENTAÇÃO</b></color>\n" +
            "Roteiro e Narrativa: Diogo, Eduardo, Felipe, Gabriel e Gabriely\nDocumentação Técnica / GDD: Felipe\n\n" +
            "<color=#" + destaque + "><b>PROGRAMAÇÃO</b></color>\n" +
            "Programação de Gameplay: Eduardo\nProgramação de UI / HUD: Eduardo\nArte Técnica / Iluminação / Otimização: Eduardo\n\n" +
            "<color=#" + destaque + "><b>ARTE 3D E VISUAL</b></color>\n" +
            "Modelagem 3D: Diogo, Eduardo e Gabriely\nTexturização e Materiais: Eduardo\nRigging e Animação: Eduardo\nDesign de UI / UX: Diogo, Eduardo e Gabriely\n\n" +
            "<color=#" + destaque + "><b>ÁUDIO</b></color>\n" +
            "Trilha Sonora / Composição: Gabriel\nSound Design / SFX: Gabriel\n\n" +
            "<color=#" + destaque + "><b>QUALIDADE E TESTES</b></color>\n" +
            "QA / Testes: Eduardo\n\n" +
            "<color=#" + destaque + "><b>AGRADECIMENTOS ESPECIAIS</b></color>\n" +
            "Professores Daniel e Mario\nOrientadora Andressa";
        creditText.ForceMeshUpdate();
        float alturaCreditos = creditText.preferredHeight;
        textRect.sizeDelta = new Vector2(0f, alturaCreditos);
        float alturaTela = scaler.referenceResolution.y;
        textRect.anchoredPosition = new Vector2(0f, -alturaCreditos);

        float tempoFade = 0f;
        while (tempoFade < duracaoTelaPreta)
        {
            tempoFade += Time.deltaTime;
            Color cor = background.color;
            cor.a = Mathf.Clamp01(tempoFade / duracaoTelaPreta);
            background.color = cor;
            yield return null;
        }
        background.color = Color.black;

        float duracao = Mathf.Max(duracaoRolagemCreditos, 0.1f);
        float tempoRolagem = 0f;
        while (tempoRolagem < duracao)
        {
            tempoRolagem += Time.deltaTime;
            float progresso = Mathf.Clamp01(tempoRolagem / duracao);
            textRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(-alturaCreditos, alturaTela, progresso));
            yield return null;
        }
        textRect.anchoredPosition = new Vector2(0f, alturaTela);
        SaveGameDatabase.Delete();
        SceneManager.LoadScene("Demo1");
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
