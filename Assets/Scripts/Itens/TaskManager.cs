using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    [Header("Quadro na Parede")]
    public Transform quadroObjeto; // Arraste o objeto "Quadro-Tarefas" aqui

    [Header("Lista de Tarefas")]
    public List<TaskData> dailyTasks;

    [Header("Iluminação após regar a planta")]
    public GameObject luzesApagadas;
    public GameObject luzesAcessas;
    public GameObject shaders;
    public GameObject[] luzesParaDesativarAoRegar;
    public GameObject alimentacao;
    public bool regarPlantaConcluiTodasTarefas;
    public GameObject telaPretaFecharRapido;
    public GameObject cadeiraSentavel;
    public ConversaTelefonicaUI conversaAposRegar;
    public GameObject posJantar;

    private Dictionary<string, GameObject> dicionarioX3D = new Dictionary<string, GameObject>();
    private HashSet<string> tarefasConcluidas = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    private bool transicaoRegarPlantaIniciada;
    private bool dialogoAguardandoSentar;
    private bool dialogoAposSentarIniciado;
    private GameObject noiteJanelaSala;
    private GameObject noiteJanelaQuarto;
    private GameObject breuJanelaSala;
    private GameObject breuJanelaQuarto;

    private void Awake()
    {
        PrepararCadeiraSentavel();
        PrepararJanelasBreu();
        ConfigurarObjetosPosJantar(false);
        if (alimentacao != null) alimentacao.SetActive(false);
        if (posJantar != null) posJantar.SetActive(false);
    }

    public bool TodasTarefasConcluidas
    {
        get
        {
            if (dailyTasks == null || dailyTasks.Count == 0) return false;
            foreach (TaskData task in dailyTasks)
                if (task == null || !tarefasConcluidas.Contains(task.taskName)) return false;
            return true;
        }
    }

    public bool IsTaskCompleted(string nomeDaTarefa)
    {
        return !string.IsNullOrWhiteSpace(nomeDaTarefa) && tarefasConcluidas.Contains(nomeDaTarefa);
    }

    void Start()
    {
        ConfigurarQuadro();
    }

    void ConfigurarQuadro()
    {
        dicionarioX3D.Clear();
        tarefasConcluidas.Clear();

        if (dailyTasks == null || quadroObjeto == null) return;

        foreach (var task in dailyTasks)
        {
            if (task == null || string.IsNullOrWhiteSpace(task.taskName)) continue;

            // Procura o objeto filho exatamente pelo nome da tarefa (ex: "Make Coffee", etc.)
            Transform marcaX = quadroObjeto.Find(task.taskName);

            if (marcaX != null)
            {
                // Deixa o X invisível no início do jogo
                marcaX.gameObject.SetActive(false);
                
                // Guarda no dicionário
                dicionarioX3D.Add(task.taskName, marcaX.gameObject);
                Debug.Log("Mapeado com sucesso: " + task.taskName);
            }
            else
            {
                Debug.LogWarning("Não encontrou o objeto filho com o nome '" + task.taskName + "' dentro do Quadro-Tarefas!");
            }
        }
    }

    public void CompleteTask(string nomeDaTarefa)
    {
        if (dailyTasks == null || string.IsNullOrWhiteSpace(nomeDaTarefa)) return;

        foreach (var task in dailyTasks)
        {
            if (task != null && string.Equals(task.taskName, nomeDaTarefa, System.StringComparison.OrdinalIgnoreCase))
            {
                // O nome configurado pode variar entre "TarefaPlanta" e "RegarPlanta".
                if (string.Equals(task.taskName, "TarefaPlanta", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(task.taskName, "RegarPlanta", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (!transicaoRegarPlantaIniciada)
                    {
                        transicaoRegarPlantaIniciada = true;
                        TocarTelaPretaFecharRapido();
                        StartCoroutine(ConcluirRegarPlantaAposDoisSegundos(task));
                    }
                }
                else
                {
                    RegistrarTarefaConcluida(task);
                }

                break;
            }
        }
    }

    private void TocarTelaPretaFecharRapido()
    {
        if (telaPretaFecharRapido == null)
        {
            Debug.LogWarning("TaskManager: TelaPretaFecharRapido não foi atribuída.", this);
            return;
        }

        telaPretaFecharRapido.SetActive(true);
        Transform canvas = telaPretaFecharRapido.transform.Find("Canvas");
        if (canvas != null) canvas.localScale = Vector3.one;

        Animator[] animadores = telaPretaFecharRapido.GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animadores)
        {
            if (animator == null) continue;

            string estado = animator.name == "Cima" ? "FecharRapidoCima"
                : animator.name == "Baixo" ? "FecharRapidoBaixo" : null;
            if (estado == null) continue;

            animator.enabled = true;
            animator.Play(estado, 0, 0f);
            animator.Update(0f);
        }
    }

    public void ReproduzirTelaPretaFecharRapido()
    {
        TocarTelaPretaFecharRapido();
        StartCoroutine(OcultarTelaPretaQuandoAnimacaoTerminar());
    }

    private IEnumerator OcultarTelaPretaQuandoAnimacaoTerminar()
    {
        yield return new WaitForSecondsRealtime(3.5f);
        if (telaPretaFecharRapido != null)
            telaPretaFecharRapido.SetActive(false);
    }

    private IEnumerator ConcluirRegarPlantaAposDoisSegundos(TaskData tarefaPlanta)
    {
        yield return new WaitForSecondsRealtime(2f);

        RegistrarTarefaConcluida(tarefaPlanta);
        AtivarIluminacaoAcesa();
        if (regarPlantaConcluiTodasTarefas)
            ConcluirTodasAsTarefas();
        LiberarCadeiraSentavel();
        dialogoAguardandoSentar = conversaAposRegar != null;
        if (conversaAposRegar != null)
        {
            if (conversaAposRegar.IniciarConversaAguardandoSentar())
                StartCoroutine(AguardarFimDoDialogoAposRegar());
            else
                dialogoAguardandoSentar = false;
        }
        else
        {
            Debug.LogWarning("TaskManager: atribua a conversa que será iniciada ao sentar após regar a planta.", this);
        }

        // Deixa a animação completar a abertura depois de aplicar as mudanças.
        yield return new WaitForSecondsRealtime(1.5f);
        if (telaPretaFecharRapido != null)
            telaPretaFecharRapido.SetActive(false);
    }

    public void IniciarDialogoAposSentar()
    {
        if (!dialogoAguardandoSentar || dialogoAposSentarIniciado || conversaAposRegar == null) return;

        dialogoAguardandoSentar = false;
        dialogoAposSentarIniciado = true;
        conversaAposRegar.ContinuarAposSentar();
    }

    private IEnumerator AguardarFimDoDialogoAposRegar()
    {
        while (conversaAposRegar != null && !conversaAposRegar.ConversaConcluida)
            yield return null;

        if (posJantar != null) posJantar.SetActive(true);

        TocarTelaPretaFecharRapido();
        yield return new WaitForSecondsRealtime(2f);

        PlayerFPS jogador = FindAnyObjectByType<PlayerFPS>();
        if (jogador != null)
        {
            jogador.TeleportarPara(new Vector3(-7.66099977f, 2.42600012f, -32.2929993f));
            CadeiraSentavelInteractable cadeira = cadeiraSentavel != null
                ? cadeiraSentavel.GetComponent<CadeiraSentavelInteractable>()
                : null;
            if (cadeira != null) cadeira.RestaurarColliders();
        }

        if (alimentacao != null) alimentacao.SetActive(false);
        ConfigurarObjetosPosJantar(true);
        ConfigurarObjetosDaCozinha(false,
            "Telefone",
            "CanecaOri",
            "LIVROPTESTANTE");

        TrocarJanelasParaBreu();

        MecanicaColagem mecanicaColagem = FindAnyObjectByType<MecanicaColagem>();
        if (mecanicaColagem != null)
            mecanicaColagem.MostrarFotosDaSala();

        StartCoroutine(DesativarTelaPretaRapidaDepoisDaTransicao());
    }

    private void PrepararJanelasBreu()
    {
        Transform raizJanelas = GameObject.Find("Brilhos-Janelas")?.transform
            ?? EncontrarFilhoEmCena("Brilhos-Janelas");

        noiteJanelaSala = EncontrarFilho(raizJanelas, "Noite-Janela-Sala")?.gameObject
            ?? GameObject.Find("Noite-Janela-Sala");
        noiteJanelaQuarto = EncontrarFilho(raizJanelas, "Noite-Janela-Quarto")?.gameObject
            ?? GameObject.Find("Noite-Janela-Quarto");
        breuJanelaSala = EncontrarFilho(raizJanelas, "Breu-Janela-Sala")?.gameObject
            ?? GameObject.Find("Breu-Janela-Sala");
        breuJanelaQuarto = EncontrarFilho(raizJanelas, "Breu-Janela-Quarto")?.gameObject
            ?? GameObject.Find("Breu-Janela-Quarto");

        if (breuJanelaSala == null && noiteJanelaSala != null)
            breuJanelaSala = CriarJanelaBreu(noiteJanelaSala, "Breu-Janela-Sala");
        if (breuJanelaQuarto == null && noiteJanelaQuarto != null)
            breuJanelaQuarto = CriarJanelaBreu(noiteJanelaQuarto, "Breu-Janela-Quarto");

        if (breuJanelaSala != null) breuJanelaSala.SetActive(false);
        if (breuJanelaQuarto != null) breuJanelaQuarto.SetActive(false);
    }

    private static GameObject CriarJanelaBreu(GameObject modelo, string nome)
    {
        GameObject breu = Instantiate(modelo, modelo.transform.parent);
        breu.name = nome;
        breu.transform.localPosition = modelo.transform.localPosition;
        breu.transform.localRotation = modelo.transform.localRotation;
        breu.transform.localScale = modelo.transform.localScale;

        foreach (Renderer renderizador in breu.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material material in renderizador.materials)
            {
                if (material == null) continue;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.black);
                if (material.HasProperty("_Color")) material.SetColor("_Color", Color.black);
            }
        }

        foreach (Collider colisor in breu.GetComponentsInChildren<Collider>(true))
        {
            if (colisor != null) colisor.enabled = false;
        }

        breu.SetActive(false);
        return breu;
    }

    private void TrocarJanelasParaBreu()
    {
        if (noiteJanelaSala != null) noiteJanelaSala.SetActive(false);
        if (noiteJanelaQuarto != null) noiteJanelaQuarto.SetActive(false);
        if (breuJanelaSala != null) breuJanelaSala.SetActive(true);
        if (breuJanelaQuarto != null) breuJanelaQuarto.SetActive(true);
    }

    private IEnumerator DesativarTelaPretaRapidaDepoisDaTransicao()
    {
        // O clipe tem 3,5 s e começa 2 s antes do teleporte; espera apenas
        // os 1,5 s restantes para não deixar o clipe em loop reiniciar.
        yield return new WaitForSecondsRealtime(1.5f);
        if (telaPretaFecharRapido != null)
            telaPretaFecharRapido.SetActive(false);
    }

    private void LiberarCadeiraSentavel()
    {
        PrepararCadeiraSentavel();

        if (cadeiraSentavel == null)
        {
            Debug.LogWarning("TaskManager: não encontrou CadeiraSentavel dentro de CadeiradeJantar (6).", this);
            return;
        }

        int camadaInterativa = LayerMask.NameToLayer("Interativel");
        if (camadaInterativa >= 0) DefinirCamadaRecursiva(cadeiraSentavel.transform, camadaInterativa);

        CadeiraSentavelInteractable interacao = cadeiraSentavel.GetComponent<CadeiraSentavelInteractable>();
        if (interacao == null) return;
        interacao.Liberar();
    }

    private void PrepararCadeiraSentavel()
    {
        GameObject cadeiraPai = GameObject.Find("CadeiradeJantar (6)");
        Transform pontoDaCadeira = EncontrarFilho(cadeiraPai != null ? cadeiraPai.transform : null, "CadeiraSentavel");

        if (cadeiraPai != null)
            cadeiraSentavel = cadeiraPai;
        else if (cadeiraSentavel == null && pontoDaCadeira != null)
            cadeiraSentavel = pontoDaCadeira.parent != null ? pontoDaCadeira.parent.gameObject : pontoDaCadeira.gameObject;

        if (cadeiraSentavel == null) return;

        CadeiraSentavelInteractable interacao = cadeiraSentavel.GetComponent<CadeiraSentavelInteractable>();
        if (interacao == null) interacao = cadeiraSentavel.AddComponent<CadeiraSentavelInteractable>();
        interacao.DesativarOutlines();
    }

    private static Transform EncontrarFilho(Transform raiz, string nome)
    {
        if (raiz == null) return null;
        foreach (Transform filho in raiz)
        {
            if (string.Equals(filho.name, nome, System.StringComparison.OrdinalIgnoreCase)) return filho;
            Transform resultado = EncontrarFilho(filho, nome);
            if (resultado != null) return resultado;
        }
        return null;
    }

    private static Transform EncontrarFilhoEmCena(string nome)
    {
        foreach (GameObject objetoRaiz in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (string.Equals(objetoRaiz.name, nome, System.StringComparison.OrdinalIgnoreCase))
                return objetoRaiz.transform;

            Transform encontrado = EncontrarFilho(objetoRaiz.transform, nome);
            if (encontrado != null) return encontrado;
        }
        return null;
    }

    private static void ConfigurarObjetosPosJantar(bool ativos)
    {
        ConfigurarObjetosDaCozinha(ativos,
            "LIVROPT",
            "Cafederramado",
            "CanecaDerrubada",
            "Telefone-ForadeLugar");
    }

    private static void ConfigurarObjetosDaCozinha(bool ativos, params string[] nomes)
    {
        foreach (string nome in nomes)
        {
            Transform objeto = EncontrarFilhoEmCena(nome);
            if (objeto != null)
            {
                if (ativos && string.Equals(nome, "LIVROPT", System.StringComparison.OrdinalIgnoreCase))
                    PrepararLivroPegavel(objeto.gameObject);

                objeto.gameObject.SetActive(ativos);
            }
            else
                Debug.LogWarning("TaskManager: não encontrou o objeto '" + nome + "' na cena.");
        }
    }

    private static void PrepararLivroPegavel(GameObject livro)
    {
        ItemSeguravel item = livro.GetComponent<ItemSeguravel>();
        if (item == null) item = livro.AddComponent<ItemSeguravel>();
        item.nomeDoItem = "LIVROPT";
        item.podeSerPego = true;
        item.usarAjusteDeMao = true;
        item.posicaoLocalNaMao = new Vector3(0.02f, -0.02f, 0f);
        item.rotacaoLocalNaMao = new Vector3(90f, 0f, 90f);
        // A escala original da instância já é ~0,1; use uma redução moderada
        // para chegar a um livro de tamanho natural, sem encolhê-lo 13 vezes.
        item.escalaRelativaNaMao = 0.55f;
        item.centralizarMalhaNaMao = true;

        Outline contorno = livro.GetComponent<Outline>();
        if (contorno == null) contorno = livro.AddComponent<Outline>();
        contorno.OutlineMode = Outline.Mode.OutlineAll;
        contorno.OutlineColor = new Color(1f, 0.45336083f, 0f, 1f);
        contorno.OutlineWidth = 10f;
        contorno.enabled = false;

        MeshFilter[] malhas = livro.GetComponentsInChildren<MeshFilter>(true);
        bool temColisor = livro.GetComponentInChildren<Collider>(true) != null;
        if (!temColisor)
        {
            foreach (MeshFilter malha in malhas)
            {
                if (malha == null || malha.sharedMesh == null) continue;

                MeshCollider colisor = malha.gameObject.AddComponent<MeshCollider>();
                colisor.sharedMesh = malha.sharedMesh;
            }
        }

        int camadaInterativa = LayerMask.NameToLayer("Interativel");
        if (camadaInterativa >= 0)
            DefinirCamadaRecursiva(livro.transform, camadaInterativa);
    }

    private static void DefinirCamadaRecursiva(Transform raiz, int camada)
    {
        raiz.gameObject.layer = camada;
        foreach (Transform filho in raiz)
            DefinirCamadaRecursiva(filho, camada);
    }

    private void ConcluirTodasAsTarefas()
    {
        foreach (TaskData task in dailyTasks)
        {
            if (task != null) RegistrarTarefaConcluida(task);
        }
    }

    private void RegistrarTarefaConcluida(TaskData task)
    {
        if (task == null || string.IsNullOrWhiteSpace(task.taskName)
            || !tarefasConcluidas.Add(task.taskName)) return;

        AtivarXNoQuadro(task.taskName);
        Debug.Log("Tarefa concluída: " + task.taskName);

        if (task.taskName.IndexOf("cafe", System.StringComparison.OrdinalIgnoreCase) >= 0
            || task.taskName.IndexOf("coffee", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            TrocarXicaraAoDesviarOlhar xicara = Object.FindAnyObjectByType<TrocarXicaraAoDesviarOlhar>();
            if (xicara != null) xicara.ConcluirTarefaCafe();
        }
    }

    private void AtivarIluminacaoAcesa()
    {
        if (luzesApagadas != null) luzesApagadas.SetActive(false);
        if (luzesAcessas != null) luzesAcessas.SetActive(true);
        if (alimentacao != null) alimentacao.SetActive(true);
        if (shaders != null) AtivarHierarquia(shaders.transform);
        if (luzesParaDesativarAoRegar != null)
        {
            foreach (GameObject luz in luzesParaDesativarAoRegar)
            {
                if (luz != null) luz.SetActive(false);
            }
        }
    }

    private static void AtivarHierarquia(Transform raiz)
    {
        raiz.gameObject.SetActive(true);
        foreach (Transform filho in raiz)
            AtivarHierarquia(filho);
    }

    void AtivarXNoQuadro(string nomeTarefa)
    {
        if (dicionarioX3D.ContainsKey(nomeTarefa))
        {
            GameObject xObjeto = dicionarioX3D[nomeTarefa];
            if (xObjeto != null)
            {
                // Ativa o X na tela do quadro!
                xObjeto.SetActive(true);
            }
        }
    }
}
