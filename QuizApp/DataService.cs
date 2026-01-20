using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public static class DataService
{
    public static List<T> Load<T>(string path)
    {
        if (!File.Exists(path))
            return new List<T>();

        return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path))
               ?? new List<T>();
    }

    public static void Save<T>(string path, List<T> data)
    {
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
    }
}
