using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[System.Serializable]
public class GameData
{
    public bool[] levelsComplete;

    public GameData(int completeLevelIndex)
    {
        for (int i = 0; i < 5; i++)
            if (i ==  completeLevelIndex)
                levelsComplete[i] = true;
            else
                levelsComplete[i] = false;
    }

    public GameData(GameData gameData)
    {
        levelsComplete = gameData.levelsComplete;
    }
}

public static class SaveSystem
{
    
    public static void SaveGame(int completeLevelIndex)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + "/player.txt";
        FileStream stream = new FileStream(path, FileMode.Create);

        GameData data = new GameData(completeLevelIndex);

        formatter.Serialize(stream, data);
        stream.Close();
    }

    public static GameData LoadGame()
    {
        string path = Application.persistentDataPath + "/player.txt";
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            GameData data = formatter.Deserialize(stream) as GameData;
            stream.Close();

            return data;
        } else
        {
            Debug.LogError("Save file not found in " + path);
            return null;
        }
    } 
}
