using UnityEngine;

/// <summary>Permite retirar/devolver o telefone no carregador após concluir as tarefas.</summary>
public class TelefoneCarregadorInteractable : MonoBehaviour
{
    [Header("Telefone")]
    public ItemSeguravel telefone;
    public ItemSeguravel telefoneForaDeLugar;
    public Transform pontoDeRetorno;

    [Header("Desbloqueio")]
    public TaskManager taskManager;

    [Header("Ligação (opcional)")]
    public ConversaTelefonicaUI conversaTelefonica;

    [Header("Fotos que aparecem após devolver o telefone")]
    public GameObject[] fotosParaMostrarAoDevolver;

    [Header("Estado inicial")]
    public GameObject[] objetosVisiveisDesdeInicio;
    public bool telefoneDesligado;

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
                if (foto != null && !DeveFicarVisivelDesdeInicio(foto)) foto.SetActive(false);
            }
        }

        if (objetosVisiveisDesdeInicio != null)
        {
            foreach (GameObject objeto in objetosVisiveisDesdeInicio)
            {
                if (objeto != null) objeto.SetActive(true);
            }
        }

        if (telefoneDesligado)
        {
            toqueLiberado = true;
            CancelInvoke(nameof(IniciarToque));
            if (audioToque != null) audioToque.Stop();
        }
    }

    private void Update()
    {
        if (!telefoneDesligado && !toqueLiberado && EstaDesbloqueado)
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
    public bool PodeInteragir => !telefoneDesligado && EstaDesbloqueado && !telefoneArquivado;

    public bool PodePegarTelefone => PodeInteragir && telefone != null
        && !telefone.estaSendoSegurado && telefone.podeSerPego;

    public void SetHighlight(bool ativo, ItemSeguravel itemNaMao = null)
    {
        bool podeDevolverTelefoneDeslocado = itemNaMao != null
            && itemNaMao == telefoneForaDeLugar
            && telefone == telefoneForaDeLugar
            && EstaDesbloqueado
            && !telefoneArquivado;
        if (outline != null)
            outline.enabled = ativo && (PodeInteragir || podeDevolverTelefoneDeslocado);
    }

    public bool TentarPegar(Transform mao)
    {
        if (!PodePegarTelefone) return false;
        toqueCanceladoPorInteracao = true;
        CancelInvoke(nameof(IniciarToque));
        if (audioToque != null) audioToque.Stop();
        AudioClip somAtender = SomJogo.Carregar("Snd-Atender");
        if (audioToque != null && somAtender != null) audioToque.PlayOneShot(somAtender);
        telefone.PegarItem(mao);
        // O jogador para de atualizar os alvos enquanto a conversa está ativa;
        // apague o destaque agora para ele não ficar preso no carregador.
        SetHighlight(false);
        if (conversaTelefonica != null) conversaTelefonica.IniciarConversa();
        return true;
    }

    public bool TentarDevolver(ItemSeguravel itemNaMao)
    {
        bool devolvendoTelefoneForaDeLugar = itemNaMao != null && itemNaMao == telefoneForaDeLugar;
        if (telefone == null || itemNaMao != telefone
            || (!PodeInteragir && !devolvendoTelefoneForaDeLugar)) return false;
        AudioClip somAtender = SomJogo.Carregar("Snd-Atender");
        if (audioToque != null && somAtender != null) audioToque.PlayOneShot(somAtender);

        Transform destino = pontoDeRetorno != null ? pontoDeRetorno : transform;
        if (!poseDeRetornoCapturada) CapturarPoseDeRetorno();

        telefone.SoltarItem(destino);
        telefone.transform.localPosition = posicaoLocalDeRetorno;
        telefone.transform.localRotation = rotacaoLocalDeRetorno;

        if (devolvendoTelefoneForaDeLugar)
        {
            telefone.podeSerPego = false;
            telefoneArquivado = true;
            telefone.SetHighlight(false);
            SetHighlight(false);
            return true;
        }

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

    public void RegistrarTelefoneForaDeLugar(ItemSeguravel item)
    {
        if (item == null || telefoneForaDeLugar == item) return;

        if (telefone != null && telefone != item)
            telefone.gameObject.SetActive(false);

        telefoneForaDeLugar = item;
        telefone = item;
        telefoneDesligado = true;
        telefoneArquivado = false;
    }

    private void MostrarFotosDaLigacao()
    {
        if (fotosParaMostrarAoDevolver == null) return;

        foreach (GameObject foto in fotosParaMostrarAoDevolver)
        {
            if (foto != null) foto.SetActive(true);
        }
    }

    private bool DeveFicarVisivelDesdeInicio(GameObject objeto)
    {
        if (objetosVisiveisDesdeInicio == null) return false;
        foreach (GameObject visivel in objetosVisiveisDesdeInicio)
        {
            if (visivel == objeto) return true;
        }
        return false;
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
