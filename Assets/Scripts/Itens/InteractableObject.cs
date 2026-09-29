using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Header("Configuração da Tarefa")]
    public string taskNameName; // O NOME EXATO da tarefa (ex: Make Coffee)
    
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
        if (isDone) return; // Se já foi feito, não faz nada de novo

        isDone = true;
        
        // Toca animação se houver
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("DoAction");
        }

        // AVISA O TASK MANAGER PARA COLOCAR O X NO QUADRO!
        TaskManager taskMgr = FindObjectOfType<TaskManager>();
        if (taskMgr != null)
        {
            taskMgr.CompleteTask(taskNameName);
        }
        else
        {
            Debug.LogWarning("TaskManager não foi encontrado na cena!");
        }
        
        Debug.Log("Você interagiu com: " + taskNameName);
    }
}