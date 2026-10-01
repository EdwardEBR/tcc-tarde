using UnityEngine;

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
        // Se a tarefa já foi feita, impede que o contorno seja ligado novamente ao passar o mouse
        if (isDone) return;

        if (outlineScript == null) return;
        outlineScript.enabled = state;
    }

    public void Interact()
    {
        if (isDone) return; // Se já foi feito, não faz nada
        isDone = true;

        // 1. Desliga o contorno laranja IMEDIATAMENTE ao interagir
        if (outlineScript != null)
        {
            outlineScript.enabled = false;
        }

        // 2. Toca a animação (se o objeto tiver um Animator)
        Animator animObjeto = GetComponentInChildren<Animator>();
        if (animObjeto != null)
        {
            animObjeto.SetTrigger("DoAction");
        }

        // 3. Marca a tarefa como concluída no TaskManager (faz aparecer o X no quadro)
        TaskManager taskMgr = FindObjectOfType<TaskManager>();
        if (taskMgr != null)
        {
            taskMgr.CompleteTask(taskNameName);
        }

        Debug.Log("Interagiu com: " + taskNameName);
    }
}