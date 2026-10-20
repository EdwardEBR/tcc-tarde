using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Exibe uma conversa telefônica ramificada usando nós configurados no Inspector.</summary>
public class ConversaTelefonicaUI : MonoBehaviour
{
    [Header("Interface")]
    public GameObject painel;
    public TMP_Text textoPessoa;
    public GameObject painelRespostas;
    public Button botaoResposta1;
    public TMP_Text textoResposta1;
    public Button botaoResposta2;
    public TMP_Text textoResposta2;
    public TMP_Text textoInstrucao;

    [Header("Conversa")]
    public List<NoConversa> conversa = new List<NoConversa>();
    [Min(0)] public int noInicial;
    [Min(0f)] public float intervaloEntreLetras = 0.025f;

    [Header("Jogador")]
    public PlayerFPS jogador;

    [Header("Aparição da colagem")]
    public GameObject colagemParaMostrar;
    public GameObject[] objetosDaColagemParaMostrar;
    public string fraseQueMostraColagem = "colagem da árvore genealógica";

    private Coroutine rotinaDeTexto;
    private Coroutine rotinaDeEncerramento;
    private bool conversaAtiva;
    private Keyboard tecladoInscrito;
    private string bufferComandoAdmin = string.Empty;
    private const string ComandoPularConversa = "PULAR";
    public bool ConversaConcluida { get; private set; }
    [SerializeField, HideInInspector] private int versaoDoRoteiroExemplo;
    private const int VersaoAtualDoRoteiroExemplo = 6;

    private void Reset()
    {
        CriarConversaDeExemplo();
    }

    private void Awake()
    {
        if (jogador == null) jogador = FindAnyObjectByType<PlayerFPS>();
        if (conversa == null) conversa = new List<NoConversa>();
        if (versaoDoRoteiroExemplo < VersaoAtualDoRoteiroExemplo || conversa.Count == 0)
            CriarConversaDeExemplo();
        if (painel == null) CriarInterfacePadrao();
        if (painel != null) painel.SetActive(false);
        if (colagemParaMostrar != null) colagemParaMostrar.SetActive(false);
        if (objetosDaColagemParaMostrar != null)
        {
            foreach (GameObject objeto in objetosDaColagemParaMostrar)
            {
                if (objeto != null) objeto.SetActive(false);
            }
        }
    }

    private void CriarConversaDeExemplo()
    {
        versaoDoRoteiroExemplo = VersaoAtualDoRoteiroExemplo;
        conversa = new List<NoConversa>
        {
            new NoConversa
            {
                falaDaPessoa = "Oi, mãe! É a Elisa. Tô te ligando no caminho de casa. Você tá me ouvindo bem? Como foi sua manhã? Lembrei que hoje você começou o remédio novo e queria saber como se sentiu.",
                resposta1 = "Tô bem, filha... só tomei um café hoje.",
                proximoNoResposta1 = 1,
                resposta2 = "Desculpa, pode repetir? Me perdi no meio.",
                proximoNoResposta2 = 2
            },
            new NoConversa
            {
                falaDaPessoa = "Café conta como começo, né? Você sempre dizia que sem café o dia nem começava. Conseguiu comer mais alguma coisa?",
                resposta1 = "Acho que sim, mas não lembro o quê.",
                proximoNoResposta1 = 3,
                resposta2 = "Só o café. Não tô com fome.",
                proximoNoResposta2 = 4
            },
            new NoConversa
            {
                falaDaPessoa = "Claro, mãe. Vou repetir devagar. Perguntei como foi sua manhã e como você se sentiu depois do remédio.",
                resposta1 = "Acho que tomei só café.",
                proximoNoResposta1 = 3,
                resposta2 = "Eu me perdi de novo, desculpa.",
                proximoNoResposta2 = 4
            },
            new NoConversa
            {
                falaDaPessoa = "Tudo bem. Não precisa se apressar. Conseguiu comer alguma coisa além do café?",
                resposta1 = "Acho que não.",
                proximoNoResposta1 = 5,
                resposta2 = "Eu não lembro.",
                proximoNoResposta2 = 6
            },
            new NoConversa
            {
                falaDaPessoa = "Tudo bem, mãe. A gente não precisa descobrir tudo agora. Quer me dizer se está se sentindo enjoada ou se está tudo bem?",
                resposta1 = "Senti um pouco de enjoo.",
                proximoNoResposta1 = 5,
                resposta2 = "Não sei dizer.",
                proximoNoResposta2 = 6
            },
            new NoConversa
            {
                falaDaPessoa = "Entendi, mãe. Quando eu chegar, a gente confere juntas a orientação do Dr. Mário. E a colagem da árvore genealógica que você queria montar? Eu trouxe as fotos que você me pediu pra colocar nela.",
                resposta1 = "É a colagem da árvore da família?",
                proximoNoResposta1 = 7,
                resposta2 = "Não tô lembrando, filha.",
                proximoNoResposta2 = 8
            },
            new NoConversa
            {
                falaDaPessoa = "Isso, mãe. É pra reunir os nomes e as histórias da nossa família. Eu trouxe aquelas fotos que você me pediu para colocar na colagem.",
                resposta1 = "Essa colagem fica onde?",
                proximoNoResposta1 = 9,
                resposta2 = "Eu não lembro direito.",
                proximoNoResposta2 = 10
            },
            new NoConversa
            {
                falaDaPessoa = "Você comentou que queria fazer uma colagem com a árvore da família e enfeitar com fotos. Eu trouxe as que você me pediu. Você pode começar sem pressa, no seu ritmo.",
                resposta1 = "Essa colagem fica onde?",
                proximoNoResposta1 = 9,
                resposta2 = "Eu não lembro direito.",
                proximoNoResposta2 = 10
            },
            new NoConversa
            {
                falaDaPessoa = "Não tem problema, mãe. A colagem é pra reunir os nomes e as histórias da família com as fotos que eu trouxe. Posso te mostrar quando você quiser.",
                resposta1 = "Prefiro não olhar agora.",
                proximoNoResposta1 = 16,
                resposta2 = "Não sei por onde começar.",
                proximoNoResposta2 = 10
            },
            new NoConversa
            {
                falaDaPessoa = "Olha em cima da escrivaninha, no seu quarto, mãe. É a colagem da árvore genealógica que você queria fazer. Agora só falta encontrar as fotos que eu te entreguei para você continuar seu projeto.",
                resposta1 = "Acho que lembro dessas fotos.",
                proximoNoResposta1 = 11,
                resposta2 = "Não lembro onde coloquei as fotos.",
                proximoNoResposta2 = 12
            },
            new NoConversa
            {
                falaDaPessoa = "O projeto está no seu quarto, em cima da escrivaninha. É a colagem da árvore da família. Você me pediu fotos pra completar; lembra onde guardou?",
                resposta1 = "Acho que lembro dessas fotos.",
                proximoNoResposta1 = 11,
                resposta2 = "Não lembro onde coloquei as fotos.",
                proximoNoResposta2 = 12
            },
            new NoConversa
            {
                falaDaPessoa = "Você me pediu pra trazer as fotos e escolheu quais queria usar. Se lembrar onde guardou, me conta; se não, tudo bem, mãe.",
                resposta1 = "Tenho medo de não encontrar as fotos.",
                proximoNoResposta1 = 13,
                resposta2 = "Vou procurar depois da ligação.",
                proximoNoResposta2 = 14
            },
            new NoConversa
            {
                falaDaPessoa = "Tudo bem, mãe. Às vezes a gente guarda uma coisa e depois não lembra onde deixou. Não precisa se preocupar com isso agora.",
                resposta1 = "E se eu não achar as fotos?",
                proximoNoResposta1 = 13,
                resposta2 = "Você me ajuda a procurar?",
                proximoNoResposta2 = 14
            },
            new NoConversa
            {
                falaDaPessoa = "Você pode procurar pela casa com calma, mãe. Essa colagem pode ser um jeito gostoso de exercitar a memória e recordar a história da nossa família.",
                resposta1 = "Tenho medo de esquecer você.",
                proximoNoResposta1 = 15,
                resposta2 = "Fica comigo mais um pouquinho?",
                proximoNoResposta2 = 16
            },
            new NoConversa
            {
                falaDaPessoa = "Claro, mãe. Quando eu passar aí, vou gostar de ver como sua colagem ficou. Tenho certeza de que você vai fazer do seu jeito.",
                resposta1 = "Promete que vem?",
                proximoNoResposta1 = 17,
                resposta2 = "Vou esperar você.",
                proximoNoResposta2 = 16
            },
            new NoConversa
            {
                falaDaPessoa = "Eu também fico triste quando penso nisso, mãe. Mas, se um dia você não lembrar meu nome, eu posso contar de novo quem sou. Sem pressa. Eu sou a Elisa, sua filha, e te amo.",
                resposta1 = "Eu também te amo, Lise. Tchau, tchau.",
                proximoNoResposta1 = 18,
                resposta2 = "Obrigada por ficar comigo. Te amo, minha filha.",
                proximoNoResposta2 = 19
            },
            new NoConversa
            {
                falaDaPessoa = "Fico, mãe. A gente não precisa falar o tempo todo. Eu gosto de estar com você, mesmo quando a conversa se perde.",
                resposta1 = "Eu também te amo, Lise. Tchau, tchau.",
                proximoNoResposta1 = 18,
                resposta2 = "Obrigada por ficar comigo. Te amo, minha filha.",
                proximoNoResposta2 = 19
            },
            new NoConversa
            {
                falaDaPessoa = "Prometo, mãe. Vou te ligar antes e, quando chegar, vou querer te dar um abraço bem apertado.",
                resposta1 = "Eu também te amo, Lise. Tchau, tchau.",
                proximoNoResposta1 = 18,
                resposta2 = "Obrigada por ficar comigo. Te amo, minha filha.",
                proximoNoResposta2 = 19
            },
            new NoConversa
            {
                falaDaPessoa = "Eu também te amo, mãe. Depois que desligar, procura pela casa as fotos que eu te entreguei pra colagem. Você tinha guardado em algum lugar, mas não lembra onde. Quando encontrar, pode continuar seu projeto no seu tempo. Vai ser bom recordar essas histórias. Um beijo, mãe. Tchau.",
                resposta1 = "",
                proximoNoResposta1 = -1,
                resposta2 = "",
                proximoNoResposta2 = -1
            },
            new NoConversa
            {
                falaDaPessoa = "Também te amo, mãe. Depois que desligar, dá uma procurada pela casa nas fotos que eu te entreguei. Você não lembra onde guardou, mas vai encontrar com calma. Aí pode escolher quais quer colocar na colagem. Te amo. Um beijo.",
                resposta1 = "",
                proximoNoResposta1 = -1,
                resposta2 = "",
                proximoNoResposta2 = -1
            }
        };
    }

    private void CriarInterfacePadrao()
    {
        GameObject canvasObj = new GameObject("Canvas - Ligacao Telefonica", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        painel = CriarPainel(canvas.transform, "PainelLigacao", new Vector2(0f, 0f), new Vector2(1f, 1f), new Color(0f, 0f, 0f, 0f));

        GameObject caixaFala = CriarPainel(painel.transform, "CaixaFalaPessoa", new Vector2(0.045f, 0.53f), new Vector2(0.47f, 0.91f), new Color(0.02f, 0.025f, 0.035f, 0.78f));
        textoPessoa = CriarTexto(caixaFala.transform, "TextoPessoa", new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f), 32f, TextAlignmentOptions.TopLeft);

        painelRespostas = CriarPainel(painel.transform, "PainelRespostas", new Vector2(0.56f, 0.20f), new Vector2(0.96f, 0.51f), new Color(0.02f, 0.025f, 0.035f, 0.78f));
        botaoResposta1 = CriarBotaoResposta(painelRespostas.transform, "Resposta1", new Vector2(0.04f, 0.53f), new Vector2(0.96f, 0.94f), "1. ", out textoResposta1);
        botaoResposta2 = CriarBotaoResposta(painelRespostas.transform, "Resposta2", new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.47f), "2. ", out textoResposta2);
        textoInstrucao = CriarTexto(painel.transform, "InstrucaoTeclas", new Vector2(0.56f, 0.12f), new Vector2(0.96f, 0.18f), 21f, TextAlignmentOptions.MidlineLeft);
        textoInstrucao.text = "Escolha uma resposta: pressione 1 ou 2";
        textoInstrucao.gameObject.SetActive(false);
        painelRespostas.SetActive(false);
    }

    private static GameObject CriarPainel(Transform pai, string nome, Vector2 ancoraMin, Vector2 ancoraMax, Color cor)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(Image));
        objeto.transform.SetParent(pai, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = ancoraMin;
        rect.anchorMax = ancoraMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image imagem = objeto.GetComponent<Image>();
        imagem.color = cor;
        imagem.raycastTarget = false;
        return objeto;
    }

    private static TMP_Text CriarTexto(Transform pai, string nome, Vector2 ancoraMin, Vector2 ancoraMax, float tamanho, TextAlignmentOptions alinhamento)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(TextMeshProUGUI));
        objeto.transform.SetParent(pai, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = ancoraMin;
        rect.anchorMax = ancoraMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        TextMeshProUGUI texto = objeto.GetComponent<TextMeshProUGUI>();
        texto.font = TMP_Settings.defaultFontAsset;
        texto.fontSize = tamanho;
        texto.color = Color.white;
        texto.alignment = alinhamento;
        texto.enableWordWrapping = true;
        texto.raycastTarget = false;
        return texto;
    }

    private static Button CriarBotaoResposta(Transform pai, string nome, Vector2 ancoraMin, Vector2 ancoraMax, string prefixo, out TMP_Text textoResposta)
    {
        GameObject objeto = new GameObject(nome, typeof(RectTransform), typeof(Image), typeof(Button));
        objeto.transform.SetParent(pai, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = ancoraMin;
        rect.anchorMax = ancoraMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image imagem = objeto.GetComponent<Image>();
        imagem.color = new Color(1f, 1f, 1f, 0.08f);
        imagem.raycastTarget = false;
        Button botao = objeto.GetComponent<Button>();
        botao.transition = Selectable.Transition.None;
        botao.interactable = false; // As escolhas são feitas com as teclas 1 e 2.
        textoResposta = CriarTexto(objeto.transform, "Texto", new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), 25f, TextAlignmentOptions.MidlineLeft);
        textoResposta.text = prefixo;
        return botao;
    }

    private void Update()
    {
        AtualizarTecladoInscrito();
        if (!conversaAtiva || rotinaDeTexto != null || Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
            EscolherResposta(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
            EscolherResposta(1);
    }

    public void IniciarConversa()
    {
        if (conversa == null || conversa.Count == 0 || noInicial < 0 || noInicial >= conversa.Count)
        {
            Debug.LogWarning("ConversaTelefonicaUI: configure ao menos um nó válido da conversa.", this);
            return;
        }

        ConversaConcluida = false;
        conversaAtiva = true;
        bufferComandoAdmin = string.Empty;
        indiceDoNoAtual = noInicial;
        if (painel != null) painel.SetActive(true);
        if (jogador == null) jogador = FindAnyObjectByType<PlayerFPS>();
        if (jogador != null) jogador.DefinirModoConversa(true);
        MostrarNo(noInicial);
    }

    public void EncerrarConversa()
    {
        conversaAtiva = false;
        bufferComandoAdmin = string.Empty;
        if (rotinaDeTexto != null) StopCoroutine(rotinaDeTexto);
        rotinaDeTexto = null;
        if (rotinaDeEncerramento != null) StopCoroutine(rotinaDeEncerramento);
        rotinaDeEncerramento = null;
        if (painel != null) painel.SetActive(false);
        if (jogador != null) jogador.DefinirModoConversa(false);
    }

    private void MostrarNo(int indice)
    {
        if (indice < 0 || indice >= conversa.Count)
        {
            EncerrarConversa();
            return;
        }

        NoConversa no = conversa[indice];
        if (painelRespostas != null) painelRespostas.SetActive(false);
        if (textoInstrucao != null) textoInstrucao.gameObject.SetActive(false);
        if (textoPessoa == null)
        {
            MostrarRespostas(no);
            return;
        }

        if (rotinaDeTexto != null) StopCoroutine(rotinaDeTexto);
        rotinaDeTexto = StartCoroutine(DigitarFala(no));
    }

    private IEnumerator DigitarFala(NoConversa no)
    {
        textoPessoa.text = string.Empty;
        foreach (char caractere in no.falaDaPessoa)
        {
            textoPessoa.text += caractere;
            if (intervaloEntreLetras > 0f) yield return new WaitForSecondsRealtime(intervaloEntreLetras);
        }
        rotinaDeTexto = null;
        MostrarRespostas(no);
    }

    private void MostrarRespostas(NoConversa no)
    {
        if (colagemParaMostrar != null
            && !string.IsNullOrWhiteSpace(fraseQueMostraColagem)
            && !string.IsNullOrEmpty(no.falaDaPessoa)
            && no.falaDaPessoa.IndexOf(fraseQueMostraColagem, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            colagemParaMostrar.SetActive(true);
        }

        if (objetosDaColagemParaMostrar != null
            && !string.IsNullOrWhiteSpace(fraseQueMostraColagem)
            && !string.IsNullOrEmpty(no.falaDaPessoa)
            && no.falaDaPessoa.IndexOf(fraseQueMostraColagem, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            foreach (GameObject objeto in objetosDaColagemParaMostrar)
            {
                if (objeto != null) objeto.SetActive(true);
            }
        }

        bool temResposta1 = !string.IsNullOrWhiteSpace(no.resposta1);
        bool temResposta2 = !string.IsNullOrWhiteSpace(no.resposta2);
        ConfigurarResposta(botaoResposta1, textoResposta1, no.resposta1, 1);
        ConfigurarResposta(botaoResposta2, textoResposta2, no.resposta2, 2);

        if (!temResposta1 && !temResposta2)
        {
            if (painelRespostas != null) painelRespostas.SetActive(false);
            if (textoInstrucao != null) textoInstrucao.gameObject.SetActive(false);
            if (rotinaDeEncerramento != null) StopCoroutine(rotinaDeEncerramento);
            rotinaDeEncerramento = StartCoroutine(EncerrarDepoisDaDespedida());
            return;
        }

        if (painelRespostas != null) painelRespostas.SetActive(true);
        if (textoInstrucao != null)
        {
            textoInstrucao.text = temResposta1 && temResposta2
                ? "Escolha uma resposta: pressione 1 ou 2"
                : temResposta1 ? "Pressione 1 para responder" : "Pressione 2 para responder";
            textoInstrucao.gameObject.SetActive(true);
        }
    }

    private IEnumerator EncerrarDepoisDaDespedida()
    {
        yield return new WaitForSecondsRealtime(2.5f);
        rotinaDeEncerramento = null;
        ConversaConcluida = true;
        EncerrarConversa();
    }

    private static void ConfigurarResposta(Button botao, TMP_Text texto, string resposta, int tecla)
    {
        bool disponivel = !string.IsNullOrWhiteSpace(resposta);
        if (texto != null) texto.text = disponivel ? $"[{tecla}]  {resposta}" : string.Empty;
        if (botao != null) botao.gameObject.SetActive(disponivel);
    }

    private void EscolherResposta(int indiceResposta)
    {
        if (!conversaAtiva || rotinaDeTexto != null || conversa.Count == 0) return;

        NoConversa no = conversa[indiceDoNoAtual];
        string resposta = indiceResposta == 0 ? no.resposta1 : no.resposta2;
        if (string.IsNullOrWhiteSpace(resposta)) return;
        int proximo = indiceResposta == 0 ? no.proximoNoResposta1 : no.proximoNoResposta2;
        if (proximo < 0)
        {
            return; // Os nós terminais fecham automaticamente depois da fala carinhosa.
        }

        indiceDoNoAtual = proximo;
        MostrarNo(indiceDoNoAtual);
    }

    private void AtualizarTecladoInscrito()
    {
        Keyboard tecladoAtual = Keyboard.current;
        if (tecladoAtual == tecladoInscrito) return;

        if (tecladoInscrito != null)
            tecladoInscrito.onTextInput -= ReceberTextoDigitado;

        tecladoInscrito = tecladoAtual;
        if (tecladoInscrito != null)
            tecladoInscrito.onTextInput += ReceberTextoDigitado;
    }

    private void OnDisable()
    {
        if (tecladoInscrito != null)
            tecladoInscrito.onTextInput -= ReceberTextoDigitado;
        tecladoInscrito = null;
    }

    private void ReceberTextoDigitado(char caractere)
    {
        if (!conversaAtiva) return;

        // O texto recebido respeita Caps Lock/Shift. O comando só aceita letras maiúsculas.
        if (!char.IsLetter(caractere) || char.ToUpperInvariant(caractere) != caractere)
        {
            bufferComandoAdmin = string.Empty;
            return;
        }

        bufferComandoAdmin += caractere;
        if (!ComandoPularConversa.StartsWith(bufferComandoAdmin, System.StringComparison.Ordinal))
        {
            bufferComandoAdmin = caractere == 'P' ? "P" : string.Empty;
            return;
        }

        if (bufferComandoAdmin == ComandoPularConversa)
        {
            bufferComandoAdmin = string.Empty;
            PularParaUltimaFala();
        }
    }

    private void PularParaUltimaFala()
    {
        if (!conversaAtiva || conversa == null || conversa.Count == 0) return;

        // Busca de trás para frente a última fala terminal do roteiro configurado.
        for (int indice = conversa.Count - 1; indice >= 0; indice--)
        {
            NoConversa no = conversa[indice];
            if (!string.IsNullOrWhiteSpace(no.falaDaPessoa)
                && string.IsNullOrWhiteSpace(no.resposta1)
                && string.IsNullOrWhiteSpace(no.resposta2))
            {
                if (rotinaDeTexto != null) StopCoroutine(rotinaDeTexto);
                rotinaDeTexto = null;
                if (rotinaDeEncerramento != null) StopCoroutine(rotinaDeEncerramento);
                rotinaDeEncerramento = null;
                MostrarNo(indice);
                return;
            }
        }

        Debug.LogWarning("ConversaTelefonicaUI: não encontrei uma fala terminal para o comando PULAR.", this);
    }

    private int indiceDoNoAtual;
}
