using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Header("Configuração da Tarefa")]
    public string taskNameName; // Ex: TarefaRemedio

    [Header("Animações Simultâneas")]
    public Animator[] animadoresParaDisparar; // Lista de animadores que vão tocar juntos
    
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
        if (isDone) return;
        if (outlineScript == null) return;
        outlineScript.enabled = state;
    }

    public void Interact()
    {
        if (isDone) return; 
        isDone = true;

        if (outlineScript != null)
        {
            outlineScript.enabled = false;
        }

        // Dispara TODOS os animadores da lista ao mesmo tempo
        foreach (Animator anim in animadoresParaDisparar)
        {
            if (anim != null)
            {
                anim.SetTrigger("DoAction");
            }
        }

        TaskManager taskMgr = FindObjectOfType<TaskManager>();
        if (taskMgr != null)
        {
            taskMgr.CompleteTask(taskNameName);
        }

        Debug.Log("Interagiu com: " + taskNameName);
    }
}