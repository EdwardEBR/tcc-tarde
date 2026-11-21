using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SequenciaCronologicaPessoas : MonoBehaviour
{
    [Min(0.05f)] public float duracaoEsmaecimento = 0.8f;
    [Min(0f)] public float atrasoInicial = 3f;
    [Min(0f)] public float pausaEntrePessoas = 3f;

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
            yield return new WaitForSecondsRealtime(atrasoInicial);

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
                yield return new WaitForSecondsRealtime(pausaEntrePessoas);
        }
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
            tempo += Time.unscaledDeltaTime;
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
