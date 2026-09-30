using UnityEngine;
using System.Collections;

public class InteractableObject : MonoBehaviour
{
    [Header("Configuração da Tarefa")]
    public string taskNameName; // Ex: TarefaPlanta
    
    private Outline outlineScript;
    public bool isDone = false;

    void Start()
    {
        outlineScript = GetComponent<Outline>();
        if (outlineScript != null)
        {
            outlineScript.enabled = false; 
        }
    }

    public void SetHighlight(bool state)
    {
        if (outlineScript == null) return;
        outlineScript.enabled = state;
    }

    public void Interact()
    {
        if (isDone) return; // Se já foi feito, não faz nada

        // Inicia a rotina de interação com animação e espera
        StartCoroutine(ExecutarInteracaoAnimada());
    }

    IEnumerator ExecutarInteracaoAnimada()
    {
        isDone = true;

        // 1. Encontra o script de movimento do player e TRAVA ele
        PlayerMovement movimentoPlayer = FindObjectOfType<PlayerMovement>();
        if (movimentoPlayer != null)
        {
            movimentoPlayer.enabled = false; // Player para de andar e olhar em volta
        }

        // 2. Toca a animação da planta (se houver Animator)
        Animator animPlanta = GetComponent<Animator>();
        float tempoAnimacao = 2f; // Tempo padrão de segurança caso não ache a animação

        if (animPlanta != null)
        {
            animPlanta.SetTrigger("DoAction"); // Dispara a animação

            // Descobre o tempo exato da animação atual para esperar ela acabar
            yield return null; // Espera 1 frame para o Animator processar
            AnimatorStateInfo stateInfo = animPlanta.GetCurrentAnimatorStateInfo(0);
            tempoAnimacao = stateInfo.length;
        }

        // 3. Marca a tarefa como concluída no TaskManager (faz aparecer o X no quadro)
        TaskManager taskMgr = FindObjectOfType<TaskManager>();
        if (taskMgr != null)
        {
            taskMgr.CompleteTask(taskNameName);
        }

        Debug.Log("Assistindo animação de: " + taskNameName);

        // 4. ESPERA a animação da planta terminar por completo
        yield return new WaitForSeconds(tempoAnimacao);

        // 5. DEVOLVE o controle para o jogador voltar a andar
        if (movimentoPlayer != null)
        {
            movimentoPlayer.enabled = true;
        }

        // Desliga o contorno laranja após interagir
        if (outlineScript != null)
        {
            outlineScript.enabled = false;
        }

        Debug.Log("Animação concluída. Jogador liberado!");
    }
}