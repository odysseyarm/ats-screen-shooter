using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class ConfigData
{
    public List<string> uuids = new();
}

public class AppConfig
{
    private static string ConfigPath => Path.Combine(Application.persistentDataPath, "config.json");

    public ConfigData Data { get; private set; } = new();

    public void Load()
    {
        if (File.Exists(ConfigPath))
        {
            string json = File.ReadAllText(ConfigPath);
            Data = JsonUtility.FromJson<ConfigData>(json);
        }
        else
        {
            Data = new ConfigData(); // start with empty config
        }
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(Data, prettyPrint: true);
        File.WriteAllText(ConfigPath, json);
    }

    public void AddUUID(byte[] uuid6)
    {
        if (uuid6.Length != 6)
            throw new ArgumentException("UUID must be 6 bytes.");

        string hex = BitConverter.ToString(uuid6).Replace("-", "").ToLowerInvariant();
        if (!Data.uuids.Contains(hex))
        {
            Data.uuids.Add(hex);
            Save();
        }
    }

    public List<byte[]> GetUUIDs()
    {
        List<byte[]> result = new();
        foreach (string hex in Data.uuids)
        {
            byte[] bytes = new byte[6];
            for (int i = 0; i < 6; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            result.Add(bytes);
        }
        return result;
    }
}
