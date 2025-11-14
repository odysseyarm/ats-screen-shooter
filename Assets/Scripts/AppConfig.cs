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
            if (string.IsNullOrWhiteSpace(hex))
            {
                continue;
            }

            string sanitized = hex.Trim();
            if (sanitized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                sanitized = sanitized.Substring(2);
            }

            if ((sanitized.Length & 1) == 1)
            {
                sanitized = "0" + sanitized;
            }

            var bytes = new byte[sanitized.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(sanitized.Substring(i * 2, 2), 16);
            }

            helmet_uuids.Add(bytes);
        }
    }

    public void ToHex()
    {
        helmet_hex_uuids.Clear();
        foreach (var bytes in helmet_uuids)
        {
            if (bytes == null)
            {
                continue;
            }

            string hex = BitConverter.ToString(bytes).Replace("-", string.Empty).ToUpperInvariant();
            helmet_hex_uuids.Add($"0x{hex}");
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
