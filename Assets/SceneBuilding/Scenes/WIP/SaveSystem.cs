using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class GameData
{
    public bool[] levelsComplete;

    public void initializeSaveData()
    {
        for (int i = 0; i < 5; i++)
            levelsComplete[i] = false;
    }
}

public static class SaveSystem
{
    /*
    public static void SaveGame()
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + "/player.txt";
        Filestream stream = new Filestream(path, FileMode.Create);

        GameData data = new GameData(player);

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

            GameData data = formatter.Deserialize(stream);
            stream.Close();

            return data;
        } else
        {
            Debug.LogError("Save file not found in " + path);
            return null;
        }
    } */
}
