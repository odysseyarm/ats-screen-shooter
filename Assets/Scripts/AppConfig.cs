using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class ConfigData
{
    [NonSerialized]
    public List<byte[]> helmet_uuids = new();

    // Serializable proxy for `helmet_uuids`
    [SerializeField]
    private List<string> helmet_hex_uuids = new();

    public void FromBytes()
    {
        helmet_uuids.Clear();
        foreach (string hex in helmet_hex_uuids)
        {
            byte[] bytes = new byte[6];
            for (int i = 0; i < 6; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            helmet_uuids.Add(bytes);
        }
    }

    public void ToHex()
    {
        helmet_hex_uuids.Clear();
        foreach (var bytes in helmet_uuids)
        {
            if (bytes.Length != 6)
                throw new InvalidOperationException("UUID must be 6 bytes.");
            helmet_hex_uuids.Add(BitConverter.ToString(bytes).Replace("-", "").ToLower());
        }
    }
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
            Data.FromBytes();
        }
        else
        {
            Data = new ConfigData();
        }
    }

    public void Save()
    {
        Data.ToHex();
        string json = JsonUtility.ToJson(Data, prettyPrint: true);
        File.WriteAllText(ConfigPath, json);
    }
}
