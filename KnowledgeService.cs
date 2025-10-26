using System;
using System.IO;
using Newtonsoft.Json;

public static class KnowledgeService
{
    public static NpcInformation LoadNPCFromFile(string npcName)
    {
        string filePath = Path.Combine("data", "npcs", $"{npcName.ToLower()}.json");
        Console.WriteLine("Reading file from: " + filePath);

        string json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<NpcInformation>(json);;
    }
}