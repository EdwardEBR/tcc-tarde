using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public string taskNameName; // Nome da tarefa correspondente na lista (ex: "Fazer café")
    private Renderer objRenderer;
    private Color originalColor;
    public Color highlightColor = new Color(1.0f, 0.5f, 0.0f); // Laranja
    private bool isDone = false;

    void Start()
    {
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null)
        {
            originalColor = objRenderer.material.color;
        }
    }

    // Chamado quando o jogador olha para o objeto (via Raycast da câmera)
    public void Highlight(bool state)
    {
        if (isDone || objRenderer == null) return;

        if (state)
        {
            objRenderer.material.color = highlightColor; // Fica laranja
        }
        else
        {
            objRenderer.material.color = originalColor; // Volta ao normal
        }
    }

    // Chamado quando o jogador clica no objeto
    public void Interact()
    {
        if (isDone) return;

        isDone = true;
        
        // 1. Toca a animação do objeto (se tiver um Animator anexado)
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("DoAction");
        }

        // 2. Avisa o gerenciador de tarefas para marcar o "X"
        FindObjectOfType<TaskManager>().CompleteTask(taskNameName);

        // 3. Remove o destaque laranja definitivo após concluir
        Highlight(false);
    }
}