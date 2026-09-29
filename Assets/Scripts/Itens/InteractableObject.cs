using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public string taskNameName; 
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

        // Mantém ligado para testes mesmo após interagir
        outlineScript.enabled = state;
    }

    public void Interact()
    {
        isDone = true;
        
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("DoAction");
        }

        TaskManager taskMgr = FindObjectOfType<TaskManager>();
        if (taskMgr != null)
        {
            taskMgr.CompleteTask(taskNameName);
        }
        
        Debug.Log("Interagiu com: " + taskNameName);
    }
}