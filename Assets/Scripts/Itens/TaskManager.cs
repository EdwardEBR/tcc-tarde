using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    [Header("Quadro na Parede")]
    public Transform quadroObjeto; // Arraste o objeto "Quadro-Tarefas" aqui

    [Header("Lista de Tarefas")]
    public List<TaskData> dailyTasks;

    private Dictionary<string, GameObject> dicionarioX3D = new Dictionary<string, GameObject>();

    void Start()
    {
        ConfigurarQuadro();
    }

    void ConfigurarQuadro()
    {
        dicionarioX3D.Clear();

        foreach (var task in dailyTasks)
        {
            task.isCompleted = false;

            // Procura o objeto filho exatamente pelo nome da tarefa (ex: "Make Coffee", etc.)
            Transform marcaX = quadroObjeto.Find(task.taskName);

            if (marcaX != null)
            {
                // Deixa o X invisível no início do jogo
                marcaX.gameObject.SetActive(false);
                
                // Guarda no dicionário
                dicionarioX3D.Add(task.taskName, marcaX.gameObject);
                Debug.Log("Mapeado com sucesso: " + task.taskName);
            }
            else
            {
                Debug.LogWarning("Não encontrou o objeto filho com o nome '" + task.taskName + "' dentro do Quadro-Tarefas!");
            }
        }
    }

    public void CompleteTask(string nomeDaTarefa)
    {
        foreach (var task in dailyTasks)
        {
            if (task.taskName.ToLower() == nomeDaTarefa.ToLower())
            {
                task.isCompleted = true;
                AtivarXNoQuadro(task.taskName);
                Debug.Log("Tarefa concluída: " + task.taskName);
                break;
            }
        }
    }

    void AtivarXNoQuadro(string nomeTarefa)
    {
        if (dicionarioX3D.ContainsKey(nomeTarefa))
        {
            GameObject xObjeto = dicionarioX3D[nomeTarefa];
            if (xObjeto != null)
            {
                // Ativa o X na tela do quadro!
                xObjeto.SetActive(true);
            }
        }
    }
}