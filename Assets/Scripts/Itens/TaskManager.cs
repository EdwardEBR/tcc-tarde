using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public List<TaskData> dailyTasks;

    public void CompleteTask(string name)
    {
        foreach (var task in dailyTasks)
        {
            if (task.taskName == name)
            {
                task.isCompleted = true;
                UpdateUI();
            }
        }
    }

    void UpdateUI()
    {
        // Aqui você atualiza a UI/Bloco de notas depois
        Debug.Log("Tarefa concluída!");
    }
}