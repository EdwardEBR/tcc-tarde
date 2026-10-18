using UnityEngine;

public class CafeteiraInteractable : MonoBehaviour
{
    [Header("Configuração da Tarefa")]
    public string taskNameName = "TarefaCafe";

    [Header("Animações da Cafeteira")]
    public Animator animadorCafeteira; 

    [Header("Referências Visuais do Café")]
    public GameObject cafeCheioObj;    // Objeto do café cheio (aparece quando a animação acaba)
    public GameObject cafeMetadeObj;   // Objeto do café pela metade (quando enche a xícara)
    public GameObject visualCafeNaXicara; // Líquido/modelo de café que aparece dentro da xícara

    [Header("Condição da Xícara")]
    public PortaItemSlot slotDaXicara;     // Arraste o PortaItem (quadrado azul) aqui
    public ItemSeguravel xicaraDaMesa;     // Arraste a xícara da mesa aqui
    public TrocarXicaraAoDesviarOlhar trocaXicaraAoDesviarOlhar;

    private Outline outlineScript;
    public bool isDone = false;
    private bool cafeProntoParaServir = false;
    private bool aguardandoFimDaAnimacao;
    private bool animacaoDeEncherIniciou;

    void Start()
    {
        outlineScript = GetComponent<Outline>();
        if (outlineScript == null) outlineScript = GetComponentInChildren<Outline>(true);
        if (outlineScript == null) outlineScript = GetComponentInParent<Outline>();
        if (outlineScript != null) outlineScript.enabled = false;

        // Permite deixar o Animator no próprio objeto da cafeteira ou em um dos seus filhos.
        if (animadorCafeteira == null)
        {
            animadorCafeteira = GetComponentInParent<Animator>();
            if (animadorCafeteira == null)
            {
                animadorCafeteira = GetComponentInChildren<Animator>(true);
            }
        }

        if (trocaXicaraAoDesviarOlhar == null)
        {
            TrocarXicaraAoDesviarOlhar[] trocas = FindObjectsByType<TrocarXicaraAoDesviarOlhar>(FindObjectsInactive.Include);
            foreach (TrocarXicaraAoDesviarOlhar troca in trocas)
            {
                if (troca != null && slotDaXicara != null
                    && (troca.xicaraOriginal == slotDaXicara.objetoAoOcupar
                        || troca.gameObject == slotDaXicara.objetoAoOcupar))
                {
                    trocaXicaraAoDesviarOlhar = troca;
                    break;
                }
            }

            if (trocaXicaraAoDesviarOlhar == null && trocas.Length == 1)
                trocaXicaraAoDesviarOlhar = trocas[0];
        }

        if (animadorCafeteira == null)
        {
            Debug.LogWarning("CafeteiraInteractable: nenhum Animator foi atribuído ou encontrado neste objeto e seus filhos.", this);
        }

        if (cafeCheioObj != null) cafeCheioObj.SetActive(false);
        if (cafeMetadeObj != null) cafeMetadeObj.SetActive(false);
        if (visualCafeNaXicara != null) visualCafeNaXicara.SetActive(false);
        DesativarVisualCafe(xicaraDaMesa != null ? xicaraDaMesa.gameObject : null);
        DesativarVisualCafe(slotDaXicara != null ? slotDaXicara.objetoAoOcupar : null);
    }

    void Update()
    {
        if (!aguardandoFimDaAnimacao || animadorCafeteira == null)
        {
            return;
        }

        if (animadorCafeteira.IsInTransition(0)
            && animadorCafeteira.GetNextAnimatorStateInfo(0).IsName("Cafes"))
        {
            animacaoDeEncherIniciou = true;
        }

        AnimatorStateInfo estadoAtual = animadorCafeteira.GetCurrentAnimatorStateInfo(0);
        if (estadoAtual.IsName("Cafes"))
        {
            animacaoDeEncherIniciou = true;
            if (estadoAtual.normalizedTime >= 1f)
            {
                AtivarCafeteiraCheia();
            }
        }
        else if (animacaoDeEncherIniciou && !animadorCafeteira.IsInTransition(0))
        {
            AtivarCafeteiraCheia();
        }
    }

    public void SetHighlight(bool state)
    {
        // Se a tarefa já acabou, mas a cafeteira está cheia e pronta para encher a xícara, ela pode brilhar de novo!
        if (isDone && !cafeProntoParaServir) return; 
        if (outlineScript == null) return;
        outlineScript.enabled = state;
    }

    public void Interact()
    {
        // --- ETAPA 2: Depois de cheia, o clique serve o café na xícara do slot ---
        if (cafeProntoParaServir)
        {
            if (slotDaXicara != null && slotDaXicara.TemItemVisivelNoSlot)
            {
                Debug.Log("A encher a xícara de café...");

                if (cafeCheioObj != null) cafeCheioObj.SetActive(false);
                // O Animator está no CafePivot, que representa o reservatório cheio.
                if (animadorCafeteira != null) animadorCafeteira.gameObject.SetActive(false);
                if (cafeMetadeObj != null) cafeMetadeObj.SetActive(true);

                if (xicaraDaMesa != null)
                {
                    xicaraDaMesa.nomeDoItem = "Xícara com Café";
                    bool visualAtivado = AtivarVisualCafe(xicaraDaMesa.gameObject);

                    // A representação do slot também precisa mostrar o café enquanto a xícara está nele.
                    if (slotDaXicara.objetoAoOcupar != null
                        && slotDaXicara.objetoAoOcupar != xicaraDaMesa.gameObject)
                    {
                        visualAtivado |= AtivarVisualCafe(slotDaXicara.objetoAoOcupar);
                    }

                    if (!visualAtivado)
                    {
                        Debug.LogWarning("A xícara não tem visual de café. Atribua 'Visual Cafe Na Xicara' ou adicione um filho 'CafenaXicara'.", this);
                    }
                }
                else
                {
                    Debug.LogWarning("CafeteiraInteractable: atribua a xícara em 'Xícara Da Mesa'.", this);
                }

                cafeProntoParaServir = false;
                if (outlineScript != null) outlineScript.enabled = false;
            }
            else
            {
                Debug.Log("Atenção: a xícara não está no PortaItem.");
            }

            return;
        }

        // --- ETAPA 1: O primeiro clique liga a cafeteira ---
        if (isDone)
        {
            return;
        }

        if (animadorCafeteira == null)
        {
            Debug.LogError("CafeteiraInteractable: atribua um Animator à cafeteira antes de interagir.", this);
            return;
        }

        if (!PossuiTrigger(animadorCafeteira, "DoAction"))
        {
            Debug.LogError("CafeteiraInteractable: o Controller do Animator precisa ter um parâmetro Trigger chamado 'DoAction'.", animadorCafeteira);
            return;
        }

        isDone = true;
        aguardandoFimDaAnimacao = true;
        animacaoDeEncherIniciou = false;

        // A troca por desvio de olhar fica ativa apenas enquanto a cafeteira anima.
        if (trocaXicaraAoDesviarOlhar != null)
        {
            trocaXicaraAoDesviarOlhar.PrepararTrocaDuranteAnimacao();
        }

        if (outlineScript != null) outlineScript.enabled = false;

        animadorCafeteira.SetTrigger("DoAction");

        TaskManager taskMgr = FindAnyObjectByType<TaskManager>();
        if (taskMgr != null)
        {
            taskMgr.CompleteTask(taskNameName);
        }

        // Mantém a vigilância vinculada ao intervalo da animação mesmo se a tarefa
        // não estiver registrada no TaskManager da cena.
        if (trocaXicaraAoDesviarOlhar != null)
        {
            trocaXicaraAoDesviarOlhar.ConcluirTarefaCafe();
        }

        Debug.Log("Cafeteira ligada! A aguardar o fim da animação...");
    }

    private bool AtivarVisualCafe(GameObject xicara)
    {
        bool ativouVisual = false;

        if (visualCafeNaXicara != null)
        {
            visualCafeNaXicara.SetActive(true);
            ativouVisual = true;
        }

        if (xicara != null)
        {
            Transform liquidoCafe = EncontrarVisualCafe(xicara.transform);

            if (liquidoCafe != null)
            {
                liquidoCafe.gameObject.SetActive(true);
                ativouVisual = true;
            }
        }

        return ativouVisual;
    }

    private void DesativarVisualCafe(GameObject xicara)
    {
        if (xicara == null) return;

        Transform liquidoCafe = EncontrarVisualCafe(xicara.transform);

        if (liquidoCafe != null)
        {
            liquidoCafe.gameObject.SetActive(false);
        }
    }

    private static Transform EncontrarVisualCafe(Transform xicara)
    {
        if (xicara == null) return null;

        Transform visual = xicara.Find("CafeNaXicara");
        if (visual == null) visual = xicara.Find("CafenaXicara");
        if (visual == null) visual = xicara.Find("LiquidoCafe");
        return visual;
    }

    private static bool PossuiTrigger(Animator animator, string nome)
    {
        foreach (AnimatorControllerParameter parametro in animator.parameters)
        {
            if (parametro.type == AnimatorControllerParameterType.Trigger && parametro.name == nome)
            {
                return true;
            }
        }

        return false;
    }

    // Marca a cafeteira como pronta quando a animação de enchimento termina.
    public void AtivarCafeteiraCheia()
    {
        aguardandoFimDaAnimacao = false;
        cafeProntoParaServir = true;

        if (trocaXicaraAoDesviarOlhar != null)
        {
            trocaXicaraAoDesviarOlhar.EncerrarVigilanciaAposAnimacao();
        }

        if (cafeCheioObj != null)
        {
            cafeCheioObj.SetActive(true); // Deixa o café cheio visível
        }

        Debug.Log("Animação terminada! A cafeteira está cheia e pronta para encher a xícara.");
    }
}
