using UnityEngine;

/// <summary>Permite retirar/devolver o telefone no carregador após concluir as tarefas.</summary>
public class TelefoneCarregadorInteractable : MonoBehaviour
{
    [Header("Telefone")]
    public ItemSeguravel telefone;
    public Transform pontoDeRetorno;

    [Header("Desbloqueio")]
    public TaskManager taskManager;

    private Outline outline;
    private Vector3 posicaoLocalDeRetorno;
    private Quaternion rotacaoLocalDeRetorno;
    private bool poseDeRetornoCapturada;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        if (outline == null) outline = GetComponentInChildren<Outline>(true);
        // QuickOutline pode começar habilitado no Inspector. O carregador só brilha
        // quando o raycast do jogador realmente o estiver mirando.
        if (outline != null) outline.enabled = false;
        if (taskManager == null) taskManager = FindAnyObjectByType<TaskManager>();
        CapturarPoseDeRetorno();
    }

    public bool EstaDesbloqueado => taskManager != null && taskManager.TodasTarefasConcluidas;

    public bool PodePegarTelefone => EstaDesbloqueado && telefone != null
        && !telefone.estaSendoSegurado && telefone.podeSerPego;

    public void SetHighlight(bool ativo)
    {
        if (outline != null) outline.enabled = ativo && EstaDesbloqueado;
    }

    public bool TentarPegar(Transform mao)
    {
        if (!PodePegarTelefone) return false;
        telefone.PegarItem(mao);
        return true;
    }

    public bool TentarDevolver(ItemSeguravel itemNaMao)
    {
        if (!EstaDesbloqueado || telefone == null || itemNaMao != telefone) return false;
        Transform destino = pontoDeRetorno != null ? pontoDeRetorno : transform;
        if (!poseDeRetornoCapturada) CapturarPoseDeRetorno();

        telefone.SoltarItem(destino);
        telefone.transform.localPosition = posicaoLocalDeRetorno;
        telefone.transform.localRotation = rotacaoLocalDeRetorno;
        return true;
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
