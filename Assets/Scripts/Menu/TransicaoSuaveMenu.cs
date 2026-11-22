using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TransicaoSuaveMenu : MonoBehaviour
{
    private static bool transicaoEmAndamento;

    private Image telaPreta;
    private float duracaoEscurecimento;
    private float duracaoRevelacao;
    private string cenaDestino;

    public static void CarregarComTransicao(string cena, float fadeParaPreto, float fadeDoPreto)
    {
        if (transicaoEmAndamento) return;
        if (!Application.CanStreamedLevelBeLoaded(cena))
        {
            Debug.LogError("TransicaoSuaveMenu: a cena '" + cena + "' não está disponível no Build Settings.");
            return;
        }

        transicaoEmAndamento = true;
        GameObject objetoTransicao = new GameObject(
            "TransicaoSuaveMenu",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        DontDestroyOnLoad(objetoTransicao);

        Canvas canvas = objetoTransicao.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = objetoTransicao.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject telaObject = new GameObject(
            "SobreposicaoPreta",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        telaObject.transform.SetParent(objetoTransicao.transform, false);
        RectTransform rect = telaObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TransicaoSuaveMenu controlador = objetoTransicao.AddComponent<TransicaoSuaveMenu>();
        controlador.telaPreta = telaObject.GetComponent<Image>();
        controlador.telaPreta.color = new Color(0f, 0f, 0f, 0f);
        controlador.telaPreta.raycastTarget = true;
        controlador.duracaoEscurecimento = Mathf.Max(0f, fadeParaPreto);
        controlador.duracaoRevelacao = Mathf.Max(0f, fadeDoPreto);
        controlador.cenaDestino = cena;
        controlador.StartCoroutine(controlador.ExecutarTransicao());
    }

    private IEnumerator ExecutarTransicao()
    {
        yield return Esmaecer(0f, 1f, duracaoEscurecimento);

        AsyncOperation carregamento = SceneManager.LoadSceneAsync(cenaDestino);
        if (carregamento == null)
        {
            Debug.LogError("TransicaoSuaveMenu: não foi possível carregar a cena '" + cenaDestino + "'.");
            yield return Esmaecer(1f, 0f, duracaoRevelacao);
            transicaoEmAndamento = false;
            Destroy(gameObject);
            yield break;
        }

        while (!carregamento.isDone)
            yield return null;

        // Dá à cena do menu um frame para inicializar antes de revelar a tela.
        yield return null;
        yield return Esmaecer(1f, 0f, duracaoRevelacao);

        transicaoEmAndamento = false;
        Destroy(gameObject);
    }

    private IEnumerator Esmaecer(float alphaInicial, float alphaFinal, float duracao)
    {
        if (duracao <= 0f)
        {
            DefinirAlpha(alphaFinal);
            yield break;
        }

        float tempo = 0f;
        while (tempo < duracao)
        {
            tempo += Time.unscaledDeltaTime;
            DefinirAlpha(Mathf.Lerp(alphaInicial, alphaFinal, Mathf.Clamp01(tempo / duracao)));
            yield return null;
        }

        DefinirAlpha(alphaFinal);
    }

    private void DefinirAlpha(float alpha)
    {
        Color cor = telaPreta.color;
        cor.a = alpha;
        telaPreta.color = cor;
    }
}
