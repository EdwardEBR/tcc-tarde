using System;
using System.IO;
using UnityEngine;

/// <summary>Save local do jogo armazenado em JSON no computador.</summary>
public static class SaveGameDatabase
{
    [Serializable]
    public sealed class SaveData
    {
        public string sceneName;
        public float x, y, z;
        public float rotX, rotY, rotZ;
        public float sensitivity;
        public float volume;
        public string[] completedTasks;
        public bool conversationActive;
        public string conversationObjectName;
        public int conversationNode;
    }

    private static string SavePath => Path.Combine(Application.persistentDataPath, "savegame.json");

    public static bool HasSave => File.Exists(SavePath) && Load() != null;

    public static void Save(SaveData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.sceneName)) return;
        string path = SavePath;
        string temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonUtility.ToJson(data, true));
        if (File.Exists(path)) File.Delete(path);
        File.Move(temporaryPath, path);
    }

    public static SaveData Load()
    {
        try
        {
            if (!File.Exists(SavePath)) return null;
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            return data != null && !string.IsNullOrWhiteSpace(data.sceneName) ? data : null;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Não foi possível ler o jogo salvo: " + exception.Message);
            return null;
        }
    }

    public static void Delete()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
    }
}
