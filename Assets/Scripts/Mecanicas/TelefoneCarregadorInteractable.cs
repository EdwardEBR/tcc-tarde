using UnityEngine;

/// <summary>Permite retirar/devolver o telefone no carregador após concluir as tarefas.</summary>
public class TelefoneCarregadorInteractable : MonoBehaviour
{
    [Header("Telefone")]
    public ItemSeguravel telefone;
    public Transform pontoDeRetorno;

    [Header("Desbloqueio")]
    public TaskManager taskManager;

    [Header("Ligação (opcional)")]
    public ConversaTelefonicaUI conversaTelefonica;

    [Header("Fotos que aparecem após devolver o telefone")]
    public GameObject[] fotosParaMostrarAoDevolver;

    private Outline outline;
    private Vector3 posicaoLocalDeRetorno;
    private Quaternion rotacaoLocalDeRetorno;
    private bool poseDeRetornoCapturada;
    private bool telefoneArquivado;
    private bool toqueLiberado;
    private bool toqueCanceladoPorInteracao;
    private AudioSource audioToque;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        if (outline == null) outline = GetComponentInChildren<Outline>(true);
        // QuickOutline pode começar habilitado no Inspector. O carregador só brilha
        // quando o raycast do jogador realmente o estiver mirando.
        if (outline != null) outline.enabled = false;
        if (taskManager == null) taskManager = FindAnyObjectByType<TaskManager>();
        CapturarPoseDeRetorno();
        audioToque = SomJogo.CriarFonte(transform);
        // O telefone deve ser ouvido em qualquer ponto da casa.
        audioToque.spatialBlend = 0f;
        audioToque.loop = true;
        audioToque.clip = SomJogo.Carregar("Snd-Telefone");

        if (fotosParaMostrarAoDevolver != null)
        {
            foreach (GameObject foto in fotosParaMostrarAoDevolver)
            {
                if (foto != null) foto.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (!toqueLiberado && EstaDesbloqueado)
        {
            toqueLiberado = true;
            Invoke(nameof(IniciarToque), 7f);
        }
    }

    private void IniciarToque()
    {
        if (!toqueCanceladoPorInteracao && PodeInteragir && telefone != null
            && !telefone.estaSendoSegurado && audioToque != null && audioToque.clip != null)
            audioToque.Play();
    }

    public bool EstaDesbloqueado => taskManager != null && taskManager.TodasTarefasConcluidas;
    public bool PodeInteragir => EstaDesbloqueado && !telefoneArquivado;

    public bool PodePegarTelefone => PodeInteragir && telefone != null
        && !telefone.estaSendoSegurado && telefone.podeSerPego;

    public void SetHighlight(bool ativo)
    {
        if (outline != null) outline.enabled = ativo && PodeInteragir;
    }

    public bool TentarPegar(Transform mao)
    {
        if (!PodePegarTelefone) return false;
        toqueCanceladoPorInteracao = true;
        CancelInvoke(nameof(IniciarToque));
        if (audioToque != null) audioToque.Stop();
        telefone.PegarItem(mao);
        // O jogador para de atualizar os alvos enquanto a conversa está ativa;
        // apague o destaque agora para ele não ficar preso no carregador.
        SetHighlight(false);
        if (conversaTelefonica != null) conversaTelefonica.IniciarConversa();
        return true;
    }

    public bool TentarDevolver(ItemSeguravel itemNaMao)
    {
        if (!PodeInteragir || telefone == null || itemNaMao != telefone) return false;
        Transform destino = pontoDeRetorno != null ? pontoDeRetorno : transform;
        if (!poseDeRetornoCapturada) CapturarPoseDeRetorno();

        telefone.SoltarItem(destino);
        telefone.transform.localPosition = posicaoLocalDeRetorno;
        telefone.transform.localRotation = rotacaoLocalDeRetorno;

        if (conversaTelefonica != null && conversaTelefonica.ConversaConcluida)
        {
            MostrarFotosDaLigacao();
            telefoneArquivado = true;
            telefone.podeSerPego = false;
            telefone.SetHighlight(false);
            SetHighlight(false);
        }

        return true;
    }

    private void MostrarFotosDaLigacao()
    {
        if (fotosParaMostrarAoDevolver == null) return;

        foreach (GameObject foto in fotosParaMostrarAoDevolver)
        {
            if (foto != null) foto.SetActive(true);
        }
    }

    private void CapturarPoseDeRetorno()
    {
        if (telefone == null) return;

        Transform destino = pontoDeRetorno != null ? pontoDeRetorno : transform;
        posicaoLocalDeRetorno = destino.InverseTransformPoint(telefone.transform.position);
        rotacaoLocalDeRetorno = Quaternion.Inverse(destino.rotation) * telefone.transform.rotation;
        poseDeRetornoCapturada = true;
    }
}
