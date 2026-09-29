using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public string taskNameName; // Nome da tarefa correspondente (ex: "Cafetera")
    private Outline outlineScript; // Referência ao script de contorno
    private bool isDone = false;

    void Start()
    {
        // Pega o componente QuickOutline que está no mesmo objeto
        outlineScript = GetComponent<Outline>();
        
        if (outlineScript != null)
        {
            outlineScript.enabled = false; // Começa desligado
        }
    }

    // Chamado pelo PlayerMovement quando o player olha para o objeto
    public void Highlight(bool state)
    {
        if (isDone || outlineScript == null) return;

        // Liga ou desliga o contorno laranja
        outlineScript.enabled = state;
    }

    // Chamado quando o jogador clica no objeto
    public void Interact()
    {
        if (isDone) return;

        isDone = true;
        
        // 1. Toca a animação (se houver Animator)
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("DoAction");
        }

        // 2. Avisa o gerenciador para marcar a tarefa
        TaskManager taskMgr = FindObjectOfType<TaskManager>();
        if (taskMgr != null)
        {
            taskMgr.CompleteTask(taskNameName);
        }

        // 3. Desliga o contorno definitivamente após concluir
        Highlight(false);
    }
}