using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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
    public bool mostrarColagemDesdeInicio;

    private Coroutine rotinaDeTexto;
    private Coroutine rotinaDeEncerramento;
    private bool conversaAtiva;
    private bool aguardandoSentarParaContinuar;
    private bool cameraLivreAtiva;
    private Keyboard tecladoInscrito;
    private string bufferComandoAdmin = string.Empty;
    private const string ComandoPularConversa = "PULAR";
    private string bufferComandoTarefas = string.Empty;
    private const string ComandoCompletarTarefas = "pular";
    private int repeticoesAnalogicoParaBaixo;
    private int repeticoesAnalogicoParaCima;
    private bool aguardarSoltarAnalogicos;
    public bool ConversaConcluida { get; private set; }
    [SerializeField, HideInInspector] private int versaoDoRoteiroExemplo;
    private const int VersaoAtualDoRoteiroExemplo = 6;
    private const float PausaMinimaDeLeitura = 2.5f;
    private const float PausaMaximaDeLeitura = 5f;
    private const float PausaPorCaractere = 0.012f;

    public bool ConversaAtiva => conversaAtiva;
    public int IndiceNoAtual => indiceDoNoAtual;

    public void DefinirCameraLivreAtiva(bool ativa)
    {
        cameraLivreAtiva = ativa;
        if (painel != null && conversaAtiva)
            painel.SetActive(!cameraLivreAtiva);
    }

    private void Reset()
    {
        CriarConversaDeExemplo();
    }

    private void Awake()
    {
        if (jogador == null) jogador = FindAnyObjectByType<PlayerFPS>();
        if (conversa == null) conversa = new List<NoConversa>();
        string nomeCena = SceneManager.GetActiveScene().name;
        if (string.Equals(nomeCena, "Nivel1", StringComparison.OrdinalIgnoreCase))
            CriarConversaNivel1();
        else if (string.Equals(nomeCena, "Nivel2", StringComparison.OrdinalIgnoreCase))
            CriarConversaNivel2();
        else if (string.Equals(nomeCena, "Nivel3", StringComparison.OrdinalIgnoreCase))
            CriarConversaNivel3();
        else if (versaoDoRoteiroExemplo < VersaoAtualDoRoteiroExemplo || conversa.Count == 0)
            CriarConversaDeExemplo();
        if (painel == null) CriarInterfacePadrao();
        if (painel != null) painel.SetActive(false);
        if (!mostrarColagemDesdeInicio && colagemParaMostrar != null)
            colagemParaMostrar.SetActive(false);
        if (!mostrarColagemDesdeInicio && objetosDaColagemParaMostrar != null)
        {
            foreach (GameObject objeto in objetosDaColagemParaMostrar)
            {
                if (objeto != null) objeto.SetActive(false);
            }
        }
    }

    private void Start()
    {
        StartCoroutine(RestaurarConversaSalvaAposInicializacao());
    }

    private IEnumerator RestaurarConversaSalvaAposInicializacao()
    {
        // Aguarda PlayerFPS.Start e a inicialização dos outros componentes da cena.
        yield return null;

        SaveGameDatabase.SaveData save = SaveGameDatabase.Load();
        if (save == null || !save.conversationActive
            || !string.Equals(save.sceneName,
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                System.StringComparison.OrdinalIgnoreCase)
            || !string.Equals(save.conversationObjectName, gameObject.name,
                System.StringComparison.Ordinal))
            yield break;

        RestaurarConversaSalva(save.conversationNode);
    }

    private void RestaurarConversaSalva(int indice)
    {
        if (conversa == null || indice < 0 || indice >= conversa.Count) return;

        if (jogador == null) jogador = FindAnyObjectByType<PlayerFPS>();
        if (jogador == null)
        {
            Debug.LogWarning("ConversaTelefonicaUI: não foi possível restaurar a conversa porque o jogador não foi encontrado.", this);
            return;
        }

        if (painel != null) painel.SetActive(true);
        conversaAtiva = true;
        aguardandoSentarParaContinuar = false;
        ConversaConcluida = false;
        indiceDoNoAtual = indice;
        jogador.DefinirModoConversa(true);
        MostrarNo(indiceDoNoAtual);
        Debug.Log("ConversaTelefonicaUI: conversa restaurada no nó " + indiceDoNoAtual + ".", this);
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

    private sealed class EntradaRoteiro
    {
        public string id;
        public string fala;
        public string resposta1;
        public string destino1;
        public string resposta2;
        public string destino2;
        public bool piscar;
        public bool falaAvancaAutomaticamente;
        public bool eventoAutoAvanca;
        public float duracaoLapso;

        public EntradaRoteiro(string id, string fala, string resposta1 = null, string destino1 = null,
            string resposta2 = null, string destino2 = null, bool piscar = false,
            bool falaAvancaAutomaticamente = false, bool eventoAutoAvanca = false, float duracaoLapso = 0.48f)
        {
            this.id = id;
            this.fala = fala;
            this.resposta1 = resposta1;
            this.destino1 = destino1;
            this.resposta2 = resposta2;
            this.destino2 = destino2;
            this.piscar = piscar;
            this.falaAvancaAutomaticamente = falaAvancaAutomaticamente;
            this.eventoAutoAvanca = eventoAutoAvanca;
            this.duracaoLapso = duracaoLapso;
        }
    }

    private static EntradaRoteiro Linha(string id, string pessoa, string fala, string proximo) =>
        new EntradaRoteiro(id, pessoa + ":\n" + fala, destino1: proximo, falaAvancaAutomaticamente: true);

    private static EntradaRoteiro Escolha(string id, string fala, string resposta1, string destino1,
        string resposta2, string destino2, bool piscar = false) =>
        new EntradaRoteiro(id, fala, resposta1, destino1, resposta2, destino2, piscar);

    private static EntradaRoteiro Final(string id, string pessoa, string fala) =>
        new EntradaRoteiro(id, pessoa + ":\n" + fala);

    private static EntradaRoteiro EventoPiscada(string id, string proximo, float duracao) =>
        new EntradaRoteiro(id, string.Empty, destino1: proximo, piscar: true,
            eventoAutoAvanca: true, duracaoLapso: duracao);

    private static List<NoConversa> MaterializarRoteiro(IReadOnlyList<EntradaRoteiro> roteiro)
    {
        Dictionary<string, int> indices = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < roteiro.Count; i++) indices[roteiro[i].id] = i;

        List<NoConversa> resultado = new List<NoConversa>(roteiro.Count);
        foreach (EntradaRoteiro entrada in roteiro)
        {
            resultado.Add(new NoConversa
            {
                falaDaPessoa = entrada.fala,
                resposta1 = entrada.resposta1 ?? string.Empty,
                proximoNoResposta1 = ObterIndiceDestino(indices, entrada.destino1),
                resposta2 = entrada.resposta2 ?? string.Empty,
                proximoNoResposta2 = ObterIndiceDestino(indices, entrada.destino2),
                piscarTelaAntesDaFala = entrada.piscar,
                falaAvancaAutomaticamente = entrada.falaAvancaAutomaticamente,
                eventoAutoAvanca = entrada.eventoAutoAvanca,
                duracaoLapso = entrada.duracaoLapso
            });
        }
        return resultado;
    }

    private static int ObterIndiceDestino(Dictionary<string, int> indices, string id)
    {
        if (string.IsNullOrEmpty(id)) return -1;
        if (indices.TryGetValue(id, out int indice)) return indice;
        Debug.LogError("ConversaTelefonicaUI: destino de diálogo não encontrado: " + id);
        return -1;
    }

    private void CriarConversaNivel1()
    {
        conversa = MaterializarRoteiro(new List<EntradaRoteiro>
        {
            Linha("elisa_phone_001", "Elisa", "Oi, mãe! Como você está se sentindo hoje? Está conseguindo me ouvir direitinho?", "elisa_phone_002"),
            Linha("elisa_phone_002", "Elisa", "Como você está se adaptando ao remédio novo? O Dr. Myers comentou que ele pode tirar um pouco o apetite... talvez causar enjoo. Você sentiu alguma coisa?", "elisa_phone_choice_medicine"),
            Escolha("elisa_phone_choice_medicine", string.Empty,
                "Não, não tive problema nenhum... embora, pensando bem, hoje eu só tenha tomado café.", "elisa_phone_coffee_reaction",
                "Um pouco de enjoo, talvez. E quase não senti fome. Achei que fosse só porque acordei sem vontade de comer.", "elisa_phone_side_effect_reaction"),
            Linha("elisa_phone_coffee_reaction", "Elisa", "Ah, mãe... você não pode tomar o remédio e ficar só no café. Eu sei que você gosta de começar o dia assim, mas precisa comer alguma coisinha também. Está me ouvindo?", "elisa_phone_anxiety_reminder"),
            Linha("elisa_phone_side_effect_reaction", "Elisa", "Poxa, mãe... eu queria estar aí para preparar alguma coisa para você. Não quero que passe mal sozinha. Se o enjoo continuar, vamos conversar com o Dr. Myers, tá? Não precisa esconder de mim quando alguma coisa não estiver bem.", "elisa_phone_anxiety_reminder"),
            Linha("elisa_phone_anxiety_reminder", "Elisa", "E tenta não se cobrar tanto, mãe. O médico comentou que essa fase de adaptação pode ser difícil. Vá com calma, um dia de cada vez. E cuidado para a ansiedade não tomar conta de tudo, está bem?", "elisa_phone_choice_anxiety"),
            Escolha("elisa_phone_choice_anxiety", string.Empty,
                "Obrigada, querida. Eu sei... tenho ficado um pouquinho nervosa hoje.", "elisa_phone_anxiety_response_01",
                "Eu tento ficar calma, filha. Mas às vezes me dá medo... parece que até as coisas mais simples estão ficando difíceis para mim.", "elisa_phone_anxiety_response_02"),
            Linha("elisa_phone_anxiety_response_01", "Elisa", "Eu imaginei, mãe. O médico comentou que pode ser difícil para você no começo. Tenta descansar um pouco enquanto se adapta, tá? Não precisa resolver tudo hoje.", "elisa_phone_collage_intro"),
            Linha("elisa_phone_anxiety_response_02", "Elisa", "Ah, mãe... eu queria poder tirar esse medo de você. Não vou fingir que é fácil, porque sei que não é. Mas você não precisa passar por isso tentando parecer forte o tempo todo. Pode falar comigo, mesmo quando não souber explicar o que está sentindo.", "elisa_phone_collage_intro"),
            Linha("elisa_phone_collage_intro", "Elisa", "Mas me conta: como está o seu projeto de colagem? Deve fazer bem voltar a mexer com as coisas, escolher as fotos, montar tudo com calma... O Martin ficou tão feliz quando encontrou aquelas fotografias dele com a Nathalia e a Julia ainda bebês.", "elisa_phone_blink_01"),
            EventoPiscada("elisa_phone_blink_01", "elisa_phone_choice_collage", 0.45f),
            Escolha("elisa_phone_choice_collage", string.Empty,
                "Minha colagem?", "elisa_phone_collage_response_01",
                "Eu comecei alguma coisa, não comecei? Às vezes olho para as fotos e não sei mais se já escolhi, se já colei... parece que me perco no meio do que estava fazendo.", "elisa_phone_collage_response_02"),
            Linha("elisa_phone_collage_response_01", "Elisa", "Isso, mãe. A sua colagem. Aquela que está na escrivaninha do seu quarto. Fico feliz que esteja fazendo alguma coisa com as mãos de novo...", "elisa_phone_family_photos"),
            Linha("elisa_phone_collage_response_02", "Elisa", "Começou, sim, mãe. Está na escrivaninha do seu quarto. E não tem problema se precisar olhar as fotos mais de uma vez. Não é uma tarefa para terminar depressa. Quero que você possa aproveitar cada lembrança no seu tempo.", "elisa_phone_family_photos"),
            Linha("elisa_phone_family_photos", "Elisa", "A família toda está gostando de separar fotografias para você colocar na colagem. Todo mundo anda procurando aquelas fotos antigas, sabe? É bonito ver cada um lembrando de uma história diferente.", "elisa_phone_choice_girls_photos"),
            Escolha("elisa_phone_choice_girls_photos", string.Empty,
                "Você já me mandou aquelas fotos das meninas?", "elisa_phone_girls_photos_response_01",
                "Aquelas fotos da Nathalia e da Julia... você pode me mandar de novo? Quero olhar para elas com calma.", "elisa_phone_girls_photos_response_02"),
            Linha("elisa_phone_girls_photos_response_01", "Elisa", "Sim, mãe. Você lembra que eu levei algumas fotos neste fim de semana? Deixei todas na mesa de centro da sala...", "elisa_phone_girls_photos_followup"),
            Linha("elisa_phone_girls_photos_response_02", "Elisa", "Claro, mãe. Eu posso mandar de novo. Mas eu também tinha levado algumas impressas neste fim de semana. Deixei na mesa de centro da sala, lembra?", "elisa_phone_girls_photos_followup"),
            Linha("elisa_phone_girls_photos_followup", "Elisa", "Você chegou a colocar em outro lugar ou já incluiu alguma delas na colagem?", "elisa_phone_blink_02"),
            EventoPiscada("elisa_phone_blink_02", "elisa_phone_choice_missing_photos", 0.55f),
            Escolha("elisa_phone_choice_missing_photos", string.Empty,
                "Não estou vendo as fotos. Acho que devo ter colocado em algum lugar e esquecido.", "elisa_phone_missing_photos_response_01",
                "Eu sei que você deixou aqui, filha... mas não consigo lembrar onde. Estou olhando para a sala e parece que a resposta deveria estar bem na minha frente.", "elisa_phone_missing_photos_response_02"),
            Linha("elisa_phone_missing_photos_response_01", "Elisa", "Tudo bem, mãe. Não precisa ficar chateada. A gente procura juntas quando eu chegar. Se não estiverem na mesa, vamos olhar em outro lugar. Não tem problema.", "elisa_phone_dinner_invitation"),
            Linha("elisa_phone_missing_photos_response_02", "Elisa", "Eu sei, mãe. E imagino como deve ser angustiante sentir que a lembrança está quase ali, mas não conseguir alcançá-la. Não vamos transformar isso em mais uma preocupação. Quando eu chegar, procuramos juntas, com calma.", "elisa_phone_dinner_invitation"),
            Linha("elisa_phone_dinner_invitation", "Elisa", "Que tal eu passar aí no fim da semana para jantar com você? A gente procura as fotos juntas depois. Não precisa preparar nada, viu? Eu levo alguma coisa. Quero passar um tempo com você também.", "elisa_phone_goodbye_setup"),
            Linha("elisa_phone_goodbye_setup", "Elisa", "Está bem, mãe. Fico muito feliz que a gente tenha conseguido conversar hoje. Eu estava com saudade de ouvir sua voz.", "elisa_phone_choice_goodbye"),
            Escolha("elisa_phone_choice_goodbye", string.Empty,
                "Eu também, Lis. Tchau, tchau.", "elisa_phone_goodbye_01",
                "Eu também, Lis... Antes de desligar, fica só mais um pouquinho? A casa fica tão quieta quando a ligação termina.", "elisa_phone_goodbye_02"),
            Linha("elisa_phone_goodbye_01", "Elisa", "Tchau, mãe. Te amo. A gente se vê no jantar, tá?", "elisa_phone_end"),
            Linha("elisa_phone_goodbye_02", "Elisa", "Claro, mãe. Eu fico mais um pouquinho. Você não precisa pedir para ouvir a minha voz. Eu também gosto de ficar aqui com você, mesmo pelo telefone.", "elisa_phone_goodbye_03"),
            Linha("elisa_phone_goodbye_03", "Helena", "Às vezes eu tenho medo de esquecer até essas conversas... mas acho que vou guardar o carinho, mesmo quando as palavras escaparem.", "elisa_phone_goodbye_04"),
            Linha("elisa_phone_goodbye_04", "Elisa", "Então guarda isso, mãe: eu te amo. E, quando alguma lembrança ficar difícil, a gente encontra outras maneiras de estar perto. Até mais tarde.", "elisa_phone_end"),
            new EntradaRoteiro("elisa_phone_end", string.Empty)
        });
        noInicial = 0;
        versaoDoRoteiroExemplo = VersaoAtualDoRoteiroExemplo;
        fraseQueMostraColagem = "projeto de colagem";
    }

    private void CriarConversaNivel2()
    {
        conversa = MaterializarRoteiro(new List<EntradaRoteiro>
        {
            Linha("d1_001", "Elisa", "Mãe? Mãe, vem cá um pouquinho.", "d1_002"),
            Linha("d1_002", "Elisa", "A gente já regou sua planta hoje. Não precisa regar de novo, tá?", "d1_003"),
            Linha("d1_003", "Elisa", "Vem se sentar com a gente. A janta está pronta e já está todo mundo esperando.", "d1_004"),
            Linha("d1_004", "Elisa", "É tão bom ver a família reunida outra vez. Fico feliz que você tenha vindo, tia Maria.", "d1_005"),
            Linha("d1_005", "Maria", "Eu também, querida. Fazia tempo que a gente não sentava todo mundo junto assim. Quem sabe a gente não repete no Natal?", "d1_006"),
            Escolha("d1_006", "Maria:\nAliás, Helena, onde você gostaria de passar o Natal este ano? Lá em casa ou aqui com vocês?",
                "Maria, você podia receber todo mundo. Assim eu teria uma boa desculpa para rever a Patrícia.", "d1_natal_maria_host",
                "A gente podia fazer a ceia aqui em casa. Faz tempo que não recebemos a família toda.", "d1_natal_helena_host"),
            Linha("d1_natal_maria_host", "Maria", "Então você quer passar o Natal lá em casa? Você ainda nem conhece meu apartamento direito. Não é grande coisa, mas a gente dá um jeito.", "d1_natal_merge"),
            Linha("d1_natal_helena_host", "Maria", "Aqui também seria uma delícia, mana. A gente pode trazer as coisas, ajudar a arrumar a mesa... você não vai precisar fazer tudo sozinha.", "d1_natal_merge"),
            Linha("d1_natal_merge", "Helena", "O importante é estar todo mundo junto. A casa pode ser pequena, desde que tenha lugar para mais uma cadeira.", "d1_lucas_natal"),
            Linha("d1_lucas_natal", "Lucas", "Por mim, a gente podia passar o Natal lá em casa também. A Ana e a Sarah vivem perguntando quando vão ver você, mãe.", "d1_elisa_viagem"),
            Linha("d1_elisa_viagem", "Elisa", "Eu sei, mas é uma viagem longa para a mãe. Não quero que ela fique cansada só para agradar a gente.", "d1_choice_viagem"),
            new EntradaRoteiro("d1_choice_viagem", string.Empty,
                "Acho melhor não viajar. Tenho me sentido mais segura ficando aqui em casa.", "d1_viagem_nao",
                "Talvez eu consiga ir. Ia ser bom ver as meninas e sair um pouco de casa.", "d1_viagem_sim"),
            Linha("d1_viagem_nao", "Lucas", "Tudo bem, mãe. Não precisa se preocupar. Se não for bom para você, a gente vem para cá.", "d1_viagem_merge"),
            Linha("d1_viagem_sim", "Lucas", "Sério? A Ana e a Sarah vão ficar felizes demais. Mas vamos combinar tudo com calma. Se você cansar, a gente volta ou descansa no caminho.", "d1_viagem_merge"),
            Linha("d1_viagem_merge", "Elisa", "A gente organiza tudo do jeito mais tranquilo. O importante é você não se sentir obrigada a nada.", "d1_maria_brinca"),
            Linha("d1_maria_brinca", "Maria", "Olha só... esses dois podem discordar de tudo, mas quando o assunto é você, viram uma equipe.", "d1_elisa_brinca"),
            Linha("d1_elisa_brinca", "Elisa", "A gente tenta, tia. Só não conta para ele que eu concordei.", "d1_lucas_brinca"),
            Linha("d1_lucas_brinca", "Lucas", "Eu ouvi, viu? Agora vamos comer antes que a comida esfrie. A maionese não vai esperar a gente.", "d1_cena_infancia"),
            Linha("d1_cena_infancia", "Narrador", "Mais tarde, depois da janta, os pratos já estão quase vazios. A conversa segue pela mesa, entre risadas e histórias antigas.", "d1_julia_abre"),
            Linha("d1_julia_abre", "Julia", "E ela ficou segurando a maçaneta do outro lado! Eu juro que achei que a porta nunca mais ia abrir.", "d1_nathalia"),
            Linha("d1_nathalia", "Nathalia", "Não foi bem assim! Você conta essa história como se a casa tivesse pegado fogo.", "d1_julia_pergunta"),
            Linha("d1_julia_pergunta", "Julia", "Vó, você costumava colocar a mamãe e o tio Lucas de castigo quando eles eram crianças? Aposto que os dois davam um trabalhão para você e para o vô.", "d1_choice_lapso"),
            new EntradaRoteiro("d1_choice_lapso", "Helena:\n...", "Desculpa, querida... eu me distraí. O que você estava me contando mesmo?", "d1_lapso_admite",
                "Ah, vocês dois aprontavam bastante... disso eu lembro.", "d1_lapso_disfarca", piscar: true),
            Linha("d1_lapso_admite", "Julia", "Eu perguntei se a mamãe e o tio Lucas davam trabalho quando eram pequenos. Eles deviam aprontar bastante, né, vó?", "d1_lapso_admite_helena"),
            Linha("d1_lapso_admite_helena", "Helena", "Ah... sim. Às vezes eram uns pestinhas. Desculpa, minha cabeça foi para outro lugar.", "d1_lapso_merge"),
            Linha("d1_lapso_disfarca", "Julia", "Eles aprontavam mesmo. Mas, vó... você lembra de alguma história? Não precisa responder correndo.", "d1_lapso_disfarca_helena"),
            Linha("d1_lapso_disfarca_helena", "Helena", "Eu... lembro de algumas. Só preciso encontrar a lembrança certa.", "d1_lapso_merge"),
            Linha("d1_lapso_merge", "Nathalia", "Sem pressa, vó. A gente gosta de ouvir suas histórias, mas não precisa se esforçar por nossa causa.", "d1_historia_movel"),
            Linha("d1_historia_movel", "Helena", "Teve uma vez com aquele móvel antigo da sala. Quando o Erik puxou a gaveta de cima, do lado esquerdo, sabe o que encontrou?", "d1_julia_movel"),
            Linha("d1_julia_movel", "Julia", "Aquela assinatura de mais de duzentos anos no cantinho de baixo?", "d1_historia_resposta"),
            Linha("d1_historia_resposta", "Helena", "Isso... essa mesma. Eu já contei essa história antes, não foi?", "d1_julia_repetiu"),
            Linha("d1_julia_repetiu", "Julia", "Contou, vó. Mais cedo, enquanto a gente ajudava a pôr a mesa.", "d1_julia_pergunta_bem"),
            Escolha("d1_julia_pergunta_bem", "Julia:\nVó Helena... você está bem? Você ficou quietinha de repente.",
                "Estou bem, minha querida. Acho que só fiquei um pouco cansada.", "d1_bem_tranquiliza",
                "Para falar a verdade, eu estou assustada. Às vezes a lembrança está aqui e, quando vou pegar, ela some.", "d1_bem_confessa"),
            Linha("d1_bem_tranquiliza", "Helena", "Não precisa se preocupar comigo. Continuem conversando. Eu gosto de ficar ouvindo vocês.", "d1_bem_tranquiliza_elisa"),
            Linha("d1_bem_tranquiliza_elisa", "Elisa", "Mãe, quer descansar um pouquinho? A gente pode terminar a conversa depois. Ninguém vai embora por causa disso.", "d1_lucas_bebida"),
            Linha("d1_bem_confessa", "Julia", "A gente não vai ficar brava, vó. Não precisa esconder quando isso acontece.", "d1_bem_confessa_maria"),
            Linha("d1_bem_confessa_maria", "Maria", "Helena, você não precisa pedir desculpa por isso. Eu sei que dá medo, mas você não está sozinha. A gente continua aqui, mesmo quando a lembrança não vem.", "d1_bem_merge"),
            Linha("d1_bem_merge", "Elisa", "Mãe, quer descansar um pouquinho? A gente pode terminar a conversa depois. Ninguém vai embora por causa disso.", "d1_lucas_bebida"),
            Escolha("d1_lucas_bebida", "Lucas:\nMãe, quer que eu pegue alguma coisa para beber? Depois eu e a Elisa tiramos a mesa. Você não precisa se preocupar com a louça.",
                "Não, obrigada. Não quero beber nada. Já está tarde.", "d1_bebida_recusa",
                "Um copo de água seria bom, obrigada. Hoje eu vou deixar vocês cuidarem da mesa.", "d1_bebida_aceita"),
            Linha("d1_bebida_recusa", "Lucas", "Tudo bem, mãe. Eu só queria ajudar.", "d1_bebida_merge"),
            Linha("d1_bebida_aceita", "Lucas", "Claro. Já trago. E pode deixar que a gente cuida de tudo por aqui.", "d1_bebida_merge"),
            Final("d1_bebida_merge", "Elisa", "Vem, mãe. Senta mais um pouquinho. Hoje você não precisa dar conta de tudo.")
        });
        noInicial = 0;
        versaoDoRoteiroExemplo = VersaoAtualDoRoteiroExemplo;
    }

    private void CriarConversaNivel3()
    {
        conversa = MaterializarRoteiro(new List<EntradaRoteiro>
        {
            Linha("d2_001", "Maria", "...mas papai nunca teria conversado comigo sobre as finanças da família. Você sabe como ele era reservado com essas coisas.", "d2_002"),
            Linha("d2_002", "Helena", "Você chegou a conversar com papai sobre isso?", "d2_choice_lapso"),
            new EntradaRoteiro("d2_choice_lapso", "...", "O-o quê? Maria?", "d2_lapso_resposta_1",
                "Papai...? Desculpa, Maria. Em que parte da conversa a gente estava?", "d2_lapso_resposta_2", piscar: true),
            Linha("d2_lapso_resposta_1", "Maria", "Sou eu, mana. É a Maria. Você tinha acabado de me perguntar se eu conversei com papai sobre as finanças da família.", "d2_lapso_merge"),
            Linha("d2_lapso_resposta_2", "Maria", "A gente estava falando de papai. Eu disse que ele era muito fechado quando o assunto era dinheiro. Está tudo bem, Helena. Eu posso repetir.", "d2_lapso_merge"),
            Linha("d2_lapso_merge", "Maria", "Vai, senta um pouquinho e descansa. Não precisa ficar andando pela casa e tentando arrumar tudo enquanto conversa comigo.", "d2_helena_senta"),
            Escolha("d2_helena_senta", "Maria:\nAssim está melhor? Quero saber de você, mana. Como você está hoje, de verdade?",
                "Estou bem... tranquila.", "d2_estado_merge",
                "Tem dias em que eu fico com medo, Maria. Começo uma coisa e, de repente, não lembro mais o que estava fazendo.", "d2_estado_confessa"),
            Linha("d2_estado_confessa", "Maria", "Eu queria poder tirar esse medo de você, mana. Não posso prometer que vai ser fácil, mas posso prometer que você não precisa fingir comigo. Pode me contar quando estiver difícil.", "d2_estado_merge"),
            Linha("d2_estado_merge", "Maria", "Fico feliz de estar falando com você. E estão cuidando bem de você por aí?", "d2_choice_cuidados"),
            Escolha("d2_choice_cuidados", "Maria:\nFico feliz de estar falando com você. E estão cuidando bem de você por aí?",
                "Ah, sim. A Elisa tem me ajudado bastante. Está tudo bem.", "d2_cuidado_merge",
                "A Elisa me ajuda muito... mas às vezes me sinto culpada por precisar tanto dela.", "d2_cuidado_culpa"),
            Linha("d2_cuidado_culpa", "Maria", "Você passou a vida inteira cuidando de tanta gente, Helena. Deixa a gente cuidar de você um pouco também. Isso não é um peso que você colocou na família.", "d2_cuidado_merge"),
            Linha("d2_cuidado_merge", "Maria", "E a colagem? Como está ficando? Fiquei tão feliz quando você me ligou pedindo algumas fotos.", "d2_fotos_intro"),
            Linha("d2_fotos_intro", "Maria", "Encontrei fotos lindas do Cristiano, de antes de ele falecer, e também dos nossos pais, de quando a gente ainda era criança.", "d2_helena_fotos"),
            Escolha("d2_helena_fotos", "Helena:\nVocê encontrou mesmo? Faz tanto tempo que eu não vejo essas fotografias...",
                "Que foto bonita... essa foi de qual viagem mesmo?", "d2_foto_viagem",
                "É aquela foto do papai com o uniforme da Marinha?", "d2_foto_marinha"),
            Linha("d2_foto_viagem", "Maria", "É da primeira viagem das meninas para Santos. Lembra? A gente passou a tarde na praia, e elas não queriam sair da água. Você ficou com a bolsa e os chinelos de todo mundo.", "d2_foto_viagem_helena"),
            Linha("d2_foto_viagem_helena", "Helena", "Santos... Eu consigo imaginar a praia. Mas não sei se estou lembrando daquele dia ou se estou imaginando agora que você contou.", "d2_fotos_merge"),
            Linha("d2_foto_marinha", "Maria", "Essa mesma. Papai gostava muito daquela farda. Eu lembro de como ele ficou sério para a fotografia e depois reclamou que ninguém tinha avisado que ia tirar foto.", "d2_foto_marinha_helena"),
            Linha("d2_foto_marinha_helena", "Helena", "Ele sempre foi assim... fazia pose de bravo, mas depois queria ver a foto primeiro que todo mundo.", "d2_fotos_merge"),
            Linha("d2_fotos_merge", "Maria", "Também separei uma foto sua com ele. Olha só, vocês estão lado a lado. Ele está segurando sua mão.", "d2_foto_pai"),
            Escolha("d2_foto_pai", "Helena:\nSou eu? Eu estava tão perto dele... Maria, por que eu não consigo lembrar desse dia?",
                "Eu queria lembrar. Parece tão importante para mim...", "d2_pai_quer_lembrar",
                "Não sei se essa lembrança é minha ou se estou só acreditando no que você está me contando.", "d2_pai_duvida"),
            Linha("d2_pai_quer_lembrar", "Maria", "Eu sei, mana. Essa foto é importante, mas você não precisa provar que se lembra para que ela continue sendo sua. Eu posso contar a história quantas vezes você quiser.", "d2_pai_merge"),
            Linha("d2_pai_duvida", "Maria", "Eu entendo. Deve ser muito difícil não saber se uma lembrança está vindo de você ou daquilo que alguém contou. Mas eu estava lá, Helena. Eu me lembro de vocês duas juntas. Posso dividir essa lembrança com você.", "d2_pai_merge"),
            Linha("d2_pai_merge", "Helena", "Então vamos colocar essa na colagem. Mesmo que eu não consiga lembrar agora... eu não quero perder essa fotografia também.", "d2_colagem_transicao"),
            Final("d2_colagem_transicao", "Maria", "Vamos colocar, sim. E as outras também, uma de cada vez. Não precisa terminar tudo hoje.")
        });
        noInicial = 0;
        versaoDoRoteiroExemplo = VersaoAtualDoRoteiroExemplo;
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
        textoInstrucao.text = "Escolha uma resposta: 1/A ou 2/B";
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
        texto.textWrappingMode = TextWrappingModes.Normal;
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
        if (!PauseMenuManager.JogoPausado && !cameraLivreAtiva)
            AtualizarCheatPorAnalogicos();
        if (PauseMenuManager.JogoPausado || cameraLivreAtiva || !conversaAtiva || rotinaDeTexto != null) return;

        bool resposta1 = Keyboard.current != null
            && (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame);
        bool resposta2 = Keyboard.current != null
            && (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame);
        resposta1 |= ControleCompatibilidade.BotaoXboxAPressionado(Gamepad.current);
        resposta2 |= ControleCompatibilidade.BotaoXboxBPressionado(Gamepad.current);
        resposta1 |= ControleCompatibilidade.BotaoJoystickPressionado(0);
        resposta2 |= ControleCompatibilidade.BotaoJoystickPressionado(1);

        if (resposta1)
            EscolherResposta(0);
        else if (resposta2)
            EscolherResposta(1);
    }

    private void AtualizarCheatPorAnalogicos()
    {
        ControleCompatibilidade.LerAnalogicos(out Vector2 esquerdo, out Vector2 direito);
        bool analogicosNeutros = Mathf.Abs(esquerdo.y) < 0.3f && Mathf.Abs(direito.y) < 0.3f;
        if (aguardarSoltarAnalogicos)
        {
            if (analogicosNeutros) aguardarSoltarAnalogicos = false;
            return;
        }

        bool ambosParaBaixo = esquerdo.y <= -0.8f && direito.y <= -0.8f;
        bool ambosParaCima = esquerdo.y >= 0.8f && direito.y >= 0.8f;
        if (!ambosParaBaixo && !ambosParaCima) return;

        aguardarSoltarAnalogicos = true;
        if (ambosParaBaixo)
        {
            repeticoesAnalogicoParaCima = 0;
            if (!conversaAtiva)
            {
                repeticoesAnalogicoParaBaixo = 0;
                return;
            }

            repeticoesAnalogicoParaBaixo++;
            if (repeticoesAnalogicoParaBaixo >= 5)
            {
                repeticoesAnalogicoParaBaixo = 0;
                bufferComandoAdmin = string.Empty;
                PularParaUltimaFala();
            }
            return;
        }

        repeticoesAnalogicoParaBaixo = 0;
        TaskManager gerenciadorTarefas = FindAnyObjectByType<TaskManager>();
        if (gerenciadorTarefas == null || gerenciadorTarefas.dailyTasks == null
            || gerenciadorTarefas.dailyTasks.Count == 0)
        {
            repeticoesAnalogicoParaCima = 0;
            return;
        }

        repeticoesAnalogicoParaCima++;
        if (repeticoesAnalogicoParaCima >= 5)
        {
            repeticoesAnalogicoParaCima = 0;
            bufferComandoTarefas = string.Empty;
            CompletarTodasAsTarefas();
        }
    }

    public void IniciarConversa()
    {
        IniciarConversa(false);
    }

    public bool IniciarConversaAguardandoSentar()
    {
        return IniciarConversa(true);
    }

    private bool IniciarConversa(bool aguardarSentar)
    {
        if (conversa == null || conversa.Count == 0 || noInicial < 0 || noInicial >= conversa.Count)
        {
            Debug.LogWarning("ConversaTelefonicaUI: configure ao menos um nó válido da conversa.", this);
            return false;
        }

        ConversaConcluida = false;
        conversaAtiva = true;
        aguardandoSentarParaContinuar = aguardarSentar;
        bufferComandoAdmin = string.Empty;
        indiceDoNoAtual = noInicial;
        if (painel != null) painel.SetActive(true);
        if (jogador == null) jogador = FindAnyObjectByType<PlayerFPS>();
        if (jogador != null) jogador.DefinirModoConversa(!aguardarSentar);
        MostrarNo(noInicial);
        return true;
    }

    public void ContinuarAposSentar()
    {
        if (!conversaAtiva || !aguardandoSentarParaContinuar) return;

        aguardandoSentarParaContinuar = false;
        if (jogador == null) jogador = FindAnyObjectByType<PlayerFPS>();
        if (jogador != null) jogador.DefinirModoConversa(true);
        if (rotinaDeTexto == null && indiceDoNoAtual >= 0 && indiceDoNoAtual < conversa.Count)
            MostrarRespostas(conversa[indiceDoNoAtual]);
    }

    public void EncerrarConversa()
    {
        conversaAtiva = false;
        aguardandoSentarParaContinuar = false;
        bufferComandoAdmin = string.Empty;
        if (rotinaDeTexto != null) StopCoroutine(rotinaDeTexto);
        rotinaDeTexto = null;
        if (rotinaDeEncerramento != null) StopCoroutine(rotinaDeEncerramento);
        rotinaDeEncerramento = null;
        if (painel != null) painel.SetActive(false);
        if (jogador != null) jogador.DefinirModoConversa(false);
        PauseMenuManager.TentarAutosave();
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
        rotinaDeTexto = no.piscarTelaAntesDaFala
            ? StartCoroutine(ExecutarLapsoAntesDaFala(no))
            : StartCoroutine(DigitarFala(no));
    }

    private IEnumerator ExecutarLapsoAntesDaFala(NoConversa no)
    {
        yield return StartCoroutine(PiscarTelaPorLapso(no.duracaoLapso));
        rotinaDeTexto = null;
        rotinaDeTexto = StartCoroutine(DigitarFala(no));
    }

    private IEnumerator PiscarTelaPorLapso(float duracaoTotal)
    {
        GameObject canvasObjeto = new GameObject("Piscada_Lapso_Memoria", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObjeto.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        CanvasScaler scaler = canvasObjeto.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject telaObjeto = new GameObject("Fechamento_dos_Olhos", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        telaObjeto.transform.SetParent(canvasObjeto.transform, false);
        RectTransform retangulo = telaObjeto.GetComponent<RectTransform>();
        retangulo.anchorMin = Vector2.zero;
        retangulo.anchorMax = Vector2.one;
        retangulo.offsetMin = Vector2.zero;
        retangulo.offsetMax = Vector2.zero;
        Image tela = telaObjeto.GetComponent<Image>();
        tela.color = new Color(0f, 0f, 0f, 0f);
        tela.raycastTarget = false;

        float volumeOriginal = AudioListener.volume;
        float duracaoFechamento = Mathf.Min(0.08f, duracaoTotal * 0.18f);
        float duracaoEspera = Mathf.Min(0.12f, duracaoTotal * 0.25f);
        float duracaoAbertura = Mathf.Max(0.05f, duracaoTotal - duracaoFechamento - duracaoEspera);
        yield return AnimarPiscada(tela, 0f, 1f, duracaoFechamento);
        AudioListener.volume = volumeOriginal * 0.35f;
        yield return new WaitForSecondsRealtime(duracaoEspera);
        AudioListener.volume = volumeOriginal;
        yield return AnimarPiscada(tela, 1f, 0f, duracaoAbertura);
        Destroy(canvasObjeto);
    }

    private static IEnumerator AnimarPiscada(Image tela, float inicio, float fim, float duracao)
    {
        float tempo = 0f;
        while (tempo < duracao)
        {
            tempo += Time.unscaledDeltaTime;
            Color cor = tela.color;
            cor.a = Mathf.Lerp(inicio, fim, Mathf.Clamp01(tempo / duracao));
            tela.color = cor;
            yield return null;
        }
        Color corFinal = tela.color;
        corFinal.a = fim;
        tela.color = corFinal;
    }

    private IEnumerator DigitarFala(NoConversa no)
    {
        if (!string.IsNullOrEmpty(no.falaDaPessoa))
        {
            textoPessoa.text = string.Empty;
            foreach (char caractere in no.falaDaPessoa)
            {
                while (cameraLivreAtiva || PauseMenuManager.JogoPausado)
                    yield return null;

                textoPessoa.text += caractere;
                if (intervaloEntreLetras > 0f) yield return new WaitForSeconds(intervaloEntreLetras);
            }
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

        if (aguardandoSentarParaContinuar)
        {
            if (painelRespostas != null) painelRespostas.SetActive(false);
            if (textoInstrucao != null) textoInstrucao.gameObject.SetActive(false);
            return;
        }

        bool temResposta1 = !string.IsNullOrWhiteSpace(no.resposta1);
        bool temResposta2 = !string.IsNullOrWhiteSpace(no.resposta2);
        ConfigurarResposta(botaoResposta1, textoResposta1, no.resposta1, 1);
        ConfigurarResposta(botaoResposta2, textoResposta2, no.resposta2, 2);

        if (!temResposta1 && !temResposta2)
        {
            if (painelRespostas != null) painelRespostas.SetActive(false);
            if (textoInstrucao != null) textoInstrucao.gameObject.SetActive(false);
            if (no.eventoAutoAvanca && no.proximoNoResposta1 >= 0)
            {
                if (rotinaDeEncerramento != null) StopCoroutine(rotinaDeEncerramento);
                rotinaDeEncerramento = StartCoroutine(AvancarAposEvento(no.proximoNoResposta1));
                return;
            }
            if (no.falaAvancaAutomaticamente && no.proximoNoResposta1 >= 0)
            {
                if (rotinaDeEncerramento != null) StopCoroutine(rotinaDeEncerramento);
                rotinaDeEncerramento = StartCoroutine(AvancarAposFala(no.proximoNoResposta1, no.falaDaPessoa));
                return;
            }
            if (rotinaDeEncerramento != null) StopCoroutine(rotinaDeEncerramento);
            rotinaDeEncerramento = StartCoroutine(EncerrarDepoisDaDespedida());
            return;
        }

        if (painelRespostas != null) painelRespostas.SetActive(true);
        if (textoInstrucao != null)
        {
            textoInstrucao.text = temResposta1 && temResposta2
                ? "Escolha uma resposta: 1/A ou 2/B"
                : temResposta1 ? "Pressione 1 ou A para responder" : "Pressione 2 ou B para responder";
            textoInstrucao.gameObject.SetActive(true);
        }
    }

    private IEnumerator AvancarAposEvento(int proximo)
    {
        yield return null;
        rotinaDeEncerramento = null;
        indiceDoNoAtual = proximo;
        MostrarNo(indiceDoNoAtual);
    }

    private IEnumerator AvancarAposFala(int proximo, string fala)
    {
        yield return AguardarPausaDeLeitura(fala);
        rotinaDeEncerramento = null;
        indiceDoNoAtual = proximo;
        MostrarNo(indiceDoNoAtual);
    }

    private IEnumerator AguardarPausaDeLeitura(string fala)
    {
        int quantidadeCaracteres = string.IsNullOrEmpty(fala) ? 0 : fala.Length;
        float duracao = Mathf.Clamp(
            PausaMinimaDeLeitura + quantidadeCaracteres * PausaPorCaractere,
            PausaMinimaDeLeitura,
            PausaMaximaDeLeitura);
        float tempo = 0f;
        while (tempo < duracao)
        {
            if (!cameraLivreAtiva && !PauseMenuManager.JogoPausado)
                tempo += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator EncerrarDepoisDaDespedida()
    {
        float tempo = 0f;
        while (tempo < 2.5f)
        {
            if (!cameraLivreAtiva && !PauseMenuManager.JogoPausado)
                tempo += Time.deltaTime;
            yield return null;
        }

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
        if (!conversaAtiva || aguardandoSentarParaContinuar || rotinaDeTexto != null || conversa.Count == 0) return;

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
        // O cheat de tarefas funciona durante o jogo todo e diferencia
        // maiúsculas de minúsculas: somente "pular" ativa esse comando.
        if (char.IsLower(caractere))
        {
            bufferComandoAdmin = string.Empty;
            bufferComandoTarefas += caractere;

            if (!ComandoCompletarTarefas.StartsWith(bufferComandoTarefas, System.StringComparison.Ordinal))
                bufferComandoTarefas = caractere == 'p' ? "p" : string.Empty;

            if (bufferComandoTarefas == ComandoCompletarTarefas)
            {
                bufferComandoTarefas = string.Empty;
                CompletarTodasAsTarefas();
            }
            return;
        }

        bufferComandoTarefas = string.Empty;
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

    private void CompletarTodasAsTarefas()
    {
        TaskManager gerenciadorTarefas = FindAnyObjectByType<TaskManager>();
        if (gerenciadorTarefas == null || gerenciadorTarefas.dailyTasks == null)
        {
            Debug.LogWarning("ConversaTelefonicaUI: não encontrei um TaskManager com tarefas para o cheat 'pular'.", this);
            return;
        }

        foreach (TaskData tarefa in gerenciadorTarefas.dailyTasks)
        {
            if (tarefa != null && !string.IsNullOrWhiteSpace(tarefa.taskName))
                gerenciadorTarefas.CompleteTask(tarefa.taskName);
        }

        Debug.Log("Cheat 'pular': todas as tarefas foram concluídas.", this);
    }

    private void PularParaUltimaFala()
    {
        if (!conversaAtiva || aguardandoSentarParaContinuar || conversa == null || conversa.Count == 0) return;

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
