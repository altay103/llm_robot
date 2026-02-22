using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[System.Serializable]
public class GeminiResponse
{
    public Candidate[] candidates;
}

[System.Serializable]
public class Candidate
{
    public Content content;
}

[System.Serializable]
public class Content
{
    public Part[] parts;
}

[System.Serializable]
public class Part
{
    public string text;
}

public class AICarController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float turnSpeed = 90f;
    
    private int currentGas = 0;
    private bool isExecutingCommands = false;
    private Coroutine commandCoroutine;

    void Update()
    {
        // Handle continuous movement based on gas value
        if (currentGas != 0)
        {
            transform.Translate(Vector3.forward * currentGas * moveSpeed * Time.deltaTime);
        }
    }

    public void PlayResponse(string response)
    {
        if (isExecutingCommands)
        {
            Debug.LogWarning("[AICarController] Komutlar zaten çalışıyor, lütfen bekleyin.");
            return;
        }

        // Parse JSON response to extract text
        string commandText = ExtractTextFromResponse(response);
        
        if (string.IsNullOrEmpty(commandText))
        {
            Debug.LogError("[AICarController] Yanıttan komut metni çıkarılamadı.");
            return;
        }

        Debug.Log($"[AICarController] Çalıştırılacak komutlar:\n{commandText}");
        
        // Parse and execute commands
        List<Command> commands = ParseCommands(commandText);
        if (commands.Count > 0)
        {
            commandCoroutine = StartCoroutine(ExecuteCommands(commands));
        }
    }

    private string ExtractTextFromResponse(string jsonResponse)
    {
        try
        {
            GeminiResponse geminiResponse = JsonUtility.FromJson<GeminiResponse>(jsonResponse);
            if (geminiResponse?.candidates != null && 
                geminiResponse.candidates.Length > 0 &&
                geminiResponse.candidates[0].content?.parts != null &&
                geminiResponse.candidates[0].content.parts.Length > 0)
            {
                return geminiResponse.candidates[0].content.parts[0].text;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AICarController] JSON parse hatası: {e.Message}");
        }
        return null;
    }

    private List<Command> ParseCommands(string text)
    {
        List<Command> commands = new List<Command>();
        
        // Regex patterns for each command
        string movePattern = @"move\s*\(\s*(-?\d+)\s*\)";
        string turnPattern = @"turn\s*\(\s*(-?\d+)\s*\)";
        string sleepPattern = @"sleep\s*\(\s*([\d.]+)\s*\)";
        string messagePattern = @"message\s*\(\s*""([^""]*)""\s*\)";
        string stopPattern = @"stop\s*\(\s*(true|false)\s*\)";

        // Find all commands with their positions
        List<(int index, Command cmd)> commandsWithIndex = new List<(int, Command)>();

        foreach (Match match in Regex.Matches(text, movePattern))
        {
            int gas = int.Parse(match.Groups[1].Value);
            gas = Mathf.Clamp(gas, -1, 1);
            commandsWithIndex.Add((match.Index, new Command(CommandType.Move, gas)));
        }

        foreach (Match match in Regex.Matches(text, turnPattern))
        {
            int direction = int.Parse(match.Groups[1].Value);
            direction = Mathf.Clamp(direction, -180, 180);
            commandsWithIndex.Add((match.Index, new Command(CommandType.Turn, direction)));
        }

        foreach (Match match in Regex.Matches(text, sleepPattern))
        {
            float time = float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            commandsWithIndex.Add((match.Index, new Command(CommandType.Sleep, time)));
        }

        foreach (Match match in Regex.Matches(text, messagePattern))
        {
            string message = match.Groups[1].Value;
            commandsWithIndex.Add((match.Index, new Command(CommandType.Message, message)));
        }

        foreach (Match match in Regex.Matches(text, stopPattern, RegexOptions.IgnoreCase))
        {
            bool result = match.Groups[1].Value.ToLower() == "true";
            commandsWithIndex.Add((match.Index, new Command(CommandType.Stop, result)));
        }

        // Sort by index to maintain order
        commandsWithIndex.Sort((a, b) => a.index.CompareTo(b.index));
        
        foreach (var item in commandsWithIndex)
        {
            commands.Add(item.cmd);
        }

        return commands;
    }

    private IEnumerator ExecuteCommands(List<Command> commands)
    {
        isExecutingCommands = true;

        foreach (Command cmd in commands)
        {
            switch (cmd.type)
            {
                case CommandType.Move:
                    ExecuteMove((int)cmd.value);
                    break;

                case CommandType.Turn:
                    ExecuteTurn((int)cmd.value);
                    break;

                case CommandType.Sleep:
                    yield return new WaitForSeconds((float)cmd.value);
                    break;

                case CommandType.Message:
                    Debug.Log($"<color=cyan>[AI Mesaj]</color> {cmd.stringValue}");
                    break;

                case CommandType.Stop:
                    ExecuteStop((bool)cmd.value);
                    yield break; // Stop execution
            }
        }

        isExecutingCommands = false;
    }

    private void ExecuteMove(int gas)
    {
        currentGas = gas;
        string state = gas == 1 ? "İleri" : gas == -1 ? "Geri" : "Durdu";
        Debug.Log($"<color=yellow>[AI Move]</color> {state}");
    }

    private void ExecuteTurn(int direction)
    {
        transform.Rotate(Vector3.up, direction);
        Debug.Log($"<color=yellow>[AI Turn]</color> {direction} derece döndü");
    }

    private void ExecuteStop(bool result)
    {
        currentGas = 0;
        isExecutingCommands = false;
        string status = result ? "<color=green>BAŞARILI</color>" : "<color=red>BAŞARISIZ</color>";
        Debug.Log($"[AI Stop] Görev {status}");
    }

    public void StopAllCommands()
    {
        if (commandCoroutine != null)
        {
            StopCoroutine(commandCoroutine);
        }
        currentGas = 0;
        isExecutingCommands = false;
    }
}

public enum CommandType
{
    Move,
    Turn,
    Sleep,
    Message,
    Stop
}

public class Command
{
    public CommandType type;
    public object value;
    public string stringValue;

    public Command(CommandType type, int value)
    {
        this.type = type;
        this.value = value;
    }

    public Command(CommandType type, float value)
    {
        this.type = type;
        this.value = value;
    }

    public Command(CommandType type, bool value)
    {
        this.type = type;
        this.value = value;
    }

    public Command(CommandType type, string value)
    {
        this.type = type;
        this.stringValue = value;
    }
}
