using UnityEngine;
using System.Collections;

public class InteractableObject : MonoBehaviour
{
    private const string TarefaRemedio = "TarefaRemedio";
    private const float AtrasoAteAsPilulasSairem = 2.3f;
    private const float DuracaoDoVooDasPilulas = 1.2f;

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

        if (string.Equals(taskNameName, TarefaRemedio, System.StringComparison.OrdinalIgnoreCase))
            SomJogo.Tocar(transform, "Snd-Remedio");
        else if (string.Equals(gameObject.name, "Wine_0", System.StringComparison.OrdinalIgnoreCase))
            StartCoroutine(TocarSomRegadorComAtraso());

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

        // A animação original já espera a caixa abrir e levanta as pílulas.
        // Só substituímos o trecho final, que tinha um destino fixo no cenário,
        // pelo trajeto até a câmera do jogador.
        if (taskNameName == TarefaRemedio)
        {
            PlayerFPS jogador = FindAnyObjectByType<PlayerFPS>();
            Animator animadorPilulas = EncontrarAnimadorPilulas();
            if (jogador != null && jogador.cameraPlayer != null && animadorPilulas != null)
            {
                StartCoroutine(LevarPilulasAteJogador(animadorPilulas.transform, animadorPilulas, jogador));
            }
        }

        TaskManager taskMgr = FindAnyObjectByType<TaskManager>();
        if (taskMgr != null)
        {
            taskMgr.CompleteTask(taskNameName);
        }

        Debug.Log("Interagiu com: " + taskNameName);
    }

    private IEnumerator TocarSomRegadorComAtraso()
    {
        yield return new WaitForSeconds(1f);
        if (this != null) SomJogo.Tocar(transform, "Snd_Regar");
    }

    private Animator EncontrarAnimadorPilulas()
    {
        foreach (Animator animador in animadoresParaDisparar)
        {
            if (animador != null && animador.name.Equals("Pilulas", System.StringComparison.OrdinalIgnoreCase))
                return animador;
        }

        return null;
    }

    private IEnumerator LevarPilulasAteJogador(Transform pilulas, Animator animadorPilulas, PlayerFPS jogador)
    {
        yield return new WaitForSeconds(AtrasoAteAsPilulasSairem);

        if (pilulas == null || animadorPilulas == null || jogador == null || jogador.cameraPlayer == null)
            yield break;

        // Congela a animação antes do trecho que empurraria as pílulas para um
        // ponto fixo; a posição atual já é a posição elevada após a caixa abrir.
        animadorPilulas.enabled = false;
        Vector3 inicio = pilulas.position;
        float tempo = 0f;

        while (tempo < DuracaoDoVooDasPilulas)
        {
            if (pilulas == null || jogador == null || jogador.cameraPlayer == null)
                yield break;

            tempo += Time.deltaTime;
            float progresso = Mathf.Clamp01(tempo / DuracaoDoVooDasPilulas);
            float suave = progresso * progresso * (3f - 2f * progresso);
            Transform camera = jogador.cameraPlayer;
            Vector3 destino = camera.position + camera.forward * 0.35f - camera.up * 0.15f;
            pilulas.position = Vector3.Lerp(inicio, destino, suave)
                + camera.up * (Mathf.Sin(progresso * Mathf.PI) * 0.12f);

            yield return null;
        }

        if (pilulas != null)
            pilulas.gameObject.SetActive(false);
    }
}
