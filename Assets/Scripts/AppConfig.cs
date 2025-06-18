using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class ConfigData
{
    [NonSerialized]
    public List<ulong> helmet_uuids = new();

    // Serializable proxy for `helmet_uuids`
    [SerializeField]
    private List<string> helmet_hex_uuids = new();

    public void FromBytes()
    {
        helmet_uuids.Clear();
        foreach (string hex in helmet_hex_uuids)
        {
            helmet_uuids.Add(Convert.ToUInt64(hex, 16));
        }
    }

    public void ToHex()
    {
        helmet_hex_uuids.Clear();
        foreach (var num in helmet_uuids)
        {
            helmet_hex_uuids.Add(string.Format("0x{0:X}", num));
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
