using UnityEngine;

[CreateAssetMenu(fileName = "NewTask", menuName = "Game/Task")]
public class TaskData : ScriptableObject
{
    public string taskName;
    public bool isCompleted;
}