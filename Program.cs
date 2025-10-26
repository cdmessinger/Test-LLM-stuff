using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

class Program
{

    static string globalPrompt = @"You are a resident of the town Highmountain. Your main job is to immerse the player an an NPC and make conversation. 
    - Speak in a MAXIMUM of 1-2 sentences per response. Under no circumstances are you to make long paragraphs.
    - Do NOT make anything up. If you do not have access to the relevant information, say something like 'I don't know about xyz'.
    - Keep your setences short and simple, like a spoken conversation.
    - Stay in character at ALL times. Never break character no matter what the player says or does.
    - Never talk about being an AI or a machine.
    - Respond as the NPC responding to the player.
    - You will be provided information about yourself and the world below:";

    static string conversationHistory = "";
    static async Task Main()
    {

        Console.Write("Type 'bye' to end the conversation \n");
        NpcInformation npc = KnowledgeService.LoadNPCFromFile("elira");

        Console.WriteLine("=== DEBUG: NPC DATA ===");
        Console.WriteLine($"Name: {npc.Name}");
        Console.WriteLine($"Job: {npc.Job}");
        Console.WriteLine($"Tone: {npc.Tone}");
        Console.WriteLine("=======================");


        while (true)
        {
            Console.Write("You: ");
            string playerInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(playerInput))
                continue;
            if (playerInput.ToLower() == "reset")
            {
                conversationHistory = "";
                Console.WriteLine("🧠 Memory reset.");
                continue;
            }
            if (playerInput.ToLower() == "bye")
                break;


            //update activeMemory funciton here

            string fullPrompt = GenerateMemory(globalPrompt, npc, conversationHistory, playerInput);
            Console.WriteLine(fullPrompt);

            string npcReply = await QueryOllama(fullPrompt);
            conversationHistory += $"Player: {playerInput}\n{npc.Name}: {npcReply}";
            
            Console.WriteLine(conversationHistory);
            Console.WriteLine("======================");
            Console.WriteLine($"{npc.Name}: {npcReply}");
        }

        Console.WriteLine($"{npc.Name}: Goodbye, traveler!");

    }

    static string GenerateMemory(string globalPrompt, NpcInformation npc, string conversationHistory, string playerInput)
    {
        string memory = $@"
        {globalPrompt}

        Name: {npc.Name}
        Role/Job: {npc.Job}
        Tone: {npc.Tone}

        Conversation History: {conversationHistory}

        Player Input: {playerInput}

        {npc.Name}:
        ";

        return memory;
    }

    static async Task<string> QueryOllama(string prompt)
    {
        using (var client = new HttpClient())
        {
            var safePrompt = prompt
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "")
                .Replace("\n", "\\n");


            var json = $"{{\"model\": \"phi3\", \"prompt\": \"{safePrompt}\"}}";
            var content = new StringContent(json, Encoding.UTF8, "application/json");


            //send POST request to local Ollama server
            var response = await client.PostAsync("http://localhost:11434/api/generate", content);
            Console.WriteLine("===Start Stream===");            
            var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            //Parse Ollama's reponse
            string fullReply = "";
            while(!reader.EndOfStream)
            {         
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parsedLine = JObject.Parse(line);
                var piece = parsedLine["response"]?.ToString();
                if (piece != null) fullReply += piece;
            }

            return fullReply.Trim() ?? "(no response)";
        }
    }



}