using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MenuPrincipalManager : MonoBehaviour
{
    [Header("Configurações de Cena")]
    [SerializeField] private string nomeDoLevelDeJogo = "Casa";

    [Header("Painéis do Menu")]
    [SerializeField] private GameObject painelMenuInicial;
    [SerializeField] private GameObject painelOpcoes;
    private GameObject painelEscolhaJogo;

    private void Awake()
    {
        CriarSubmenuJogo();
    }

    private void Start()
    {
        ControleCompatibilidade.GarantirModuloUI(EventSystem.current);
        SelecionarPrimeiroElemento(painelMenuInicial != null ? painelMenuInicial.transform : transform);
    }

    private void Update()
    {
        ControleCompatibilidade.AtualizarNavegacaoUI();
        bool voltar = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        voltar |= Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
        voltar |= ControleCompatibilidade.BotaoJoystickPressionado(1);
        if (!voltar) return;

        if (painelOpcoes != null && painelOpcoes.activeSelf)
            FecharOpcoes();
        else if (painelEscolhaJogo != null && painelEscolhaJogo.activeSelf)
            VoltarAoMenu();
    }

    public void Jogar()
    {
        painelMenuInicial.SetActive(false);
        painelEscolhaJogo.SetActive(true);
        SelecionarPrimeiroElemento(painelEscolhaJogo.transform);
    }

    private void CriarSubmenuJogo()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform pai = canvas != null ? canvas.transform : transform;
        painelEscolhaJogo = new GameObject("Painel_ContinuarOuNovo", typeof(RectTransform), typeof(Image));
        painelEscolhaJogo.transform.SetParent(pai, false);
        RectTransform painel = painelEscolhaJogo.GetComponent<RectTransform>();
        painel.anchorMin = painel.anchorMax = new Vector2(0.5f, 0.5f);
        painel.sizeDelta = new Vector2(520f, 430f);
        painelEscolhaJogo.GetComponent<Image>().color = new Color(0.035f, 0.045f, 0.065f, 0.97f);

        CriarTexto(painelEscolhaJogo.transform, "JOGAR", 34f, new Vector2(0f, 145f));
        CriarBotao(painelEscolhaJogo.transform, "Continuar", new Vector2(0f, 65f), ContinuarJogo);
        CriarBotao(painelEscolhaJogo.transform, "Carregar Jogo", new Vector2(0f, -15f), CarregarJogo);
        CriarBotao(painelEscolhaJogo.transform, "Novo Jogo", new Vector2(0f, -95f), NovoJogo);
        CriarBotao(painelEscolhaJogo.transform, "Voltar", new Vector2(0f, -175f), VoltarAoMenu);
        painelEscolhaJogo.SetActive(false);
    }

    private static void CriarTexto(Transform pai, string texto, float fonte, Vector2 posicao)
    {
        GameObject objeto = new GameObject("Título", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        objeto.transform.SetParent(pai, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = new Vector2(460f, 60f);
        TextMeshProUGUI label = objeto.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = texto;
        label.fontSize = fonte;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
    }

    private static void CriarBotao(Transform pai, string rotulo, Vector2 posicao, System.Action acao)
    {
        GameObject objeto = new GameObject("Botão_" + rotulo, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        objeto.transform.SetParent(pai, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posicao;
        rect.sizeDelta = new Vector2(390f, 58f);
        Image imagem = objeto.GetComponent<Image>();
        imagem.color = new Color(0.16f, 0.24f, 0.34f, 1f);
        Button botao = objeto.GetComponent<Button>();
        botao.targetGraphic = imagem;
        botao.onClick.AddListener(() => acao());
        CriarTexto(objeto.transform, rotulo, 22f, Vector2.zero);
    }

    public void ContinuarJogo() => CarregarJogo();

    public void CarregarJogo()
    {
        SaveGameDatabase.SaveData save = SaveGameDatabase.Load();
        if (save != null && Application.CanStreamedLevelBeLoaded(save.sceneName))
            SceneManager.LoadScene(save.sceneName);
        else
            SceneManager.LoadScene(nomeDoLevelDeJogo);
    }

    public void NovoJogo()
    {
        SaveGameDatabase.Delete();
        SceneManager.LoadScene(nomeDoLevelDeJogo);
    }

    public void VoltarAoMenu()
    {
        painelEscolhaJogo.SetActive(false);
        painelMenuInicial.SetActive(true);
        SelecionarPrimeiroElemento(painelMenuInicial.transform);
    }

    public void AbrirOpcoes()
    {
        painelMenuInicial.SetActive(false);
        painelOpcoes.SetActive(true);
        SelecionarPrimeiroElemento(painelOpcoes.transform);
    }

    public void FecharOpcoes()
    {
        painelOpcoes.SetActive(false);
        painelMenuInicial.SetActive(true);
        SelecionarPrimeiroElemento(painelMenuInicial.transform);
    }

    private static void SelecionarPrimeiroElemento(Transform raiz)
    {
        EventSystem sistema = EventSystem.current;
        if (sistema == null || raiz == null) return;
        foreach (Selectable elemento in raiz.GetComponentsInChildren<Selectable>())
        {
            if (elemento == null || !elemento.isActiveAndEnabled) continue;
            Navigation navegacao = elemento.navigation;
            if (navegacao.mode == Navigation.Mode.None)
            {
                navegacao.mode = Navigation.Mode.Automatic;
                elemento.navigation = navegacao;
            }
            if (!elemento.interactable) continue;
            sistema.SetSelectedGameObject(elemento.gameObject);
            return;
        }
        sistema.SetSelectedGameObject(null);
    }

    public void SairJogo()
    {
        Debug.Log("Saindo do Jogo...");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
