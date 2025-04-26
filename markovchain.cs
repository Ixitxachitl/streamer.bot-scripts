using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json; // Make sure Newtonsoft.Json is enabled in Streamer.bot

public class CPHInline
{
    private static Dictionary<string, List<string>> transitions = new Dictionary<string, List<string>>();
    private static int messageCounter = 0;
    private static Random rng = new Random();
    private static readonly string saveFilePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\StreamerBot\\markov_brain.json";

    private static readonly List<string> knownBots = new List<string>
    {
        "streamelements", "nightbot", "sery_bot", "wizebot", "kofistreambot",
        "botrixoficial", "tangiabot", "moobot", "own3d", "creatisbot",
        "frostytoolsdotcom", "streamlabs", "pokemoncommunitygame", "fossabot",
        "soundalerts", "botbandera", "overlayexpert", "trackerggbot",
        "songlistbot", "commanderroot", "instructbot", "autogpttest",
        "aerokickbot", "streamerelem", "ronniabot", "tune2livebot",
        "peepostreambot", "playwithviewersbot", "hexe_bot", "super_sweet_bot",
        "streamroutine_bot", "remasuri_bot", "milanitommasobot", "jeetbot",
        "bot584588", "lurky_dogg"
    };

    public bool Execute()
    {
        var botInfo = CPH.TwitchGetBot();

        if (!CPH.TryGetArg("rawInput", out string rawInput) || string.IsNullOrWhiteSpace(rawInput))
            return true;

        if (!CPH.TryGetArg("userName", out string inputUsername) || string.IsNullOrWhiteSpace(inputUsername))
            return true;

        string botUsername = botInfo.UserLogin.ToLower();
        inputUsername = inputUsername.ToLower();

        // Ignore self
        if (inputUsername == botUsername)
        {
            CPH.LogDebug("Ignoring bot's own message (userName matched bot login).");
            return true;
        }

        // Ignore known bots
        if (knownBots.Contains(inputUsername))
        {
            CPH.LogDebug($"Ignoring known bot: {inputUsername}");
            return true;
        }

        // URL filtering
        string lowerInput = rawInput.ToLower();

        if (lowerInput.Contains("http") ||
            lowerInput.Contains(".com") ||
            lowerInput.Contains(".net") ||
            lowerInput.Contains(".org"))
        {
            CPH.LogDebug("Ignoring message containing URL.");
            return true;
        }

        // Non-English filtering
        int englishCharCount = 0;
        int totalCharCount = 0;

        foreach (char c in rawInput)
        {
            if (char.IsLetter(c))
            {
                totalCharCount++;
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                    englishCharCount++;
            }
        }

        if (totalCharCount > 0)
        {
            double englishRatio = (double)englishCharCount / totalCharCount;
            if (englishRatio < 0.7)
            {
                CPH.LogDebug("Ignoring non-English message.");
                return true;
            }
        }

        // Load transitions on first run
        if (transitions.Count == 0)
            LoadTransitions();

        // Learn the message
        LearnFromChat(rawInput);

        messageCounter++;
        if (messageCounter >= 35)
        {
            string sentence = GenerateSentence();
            if (!string.IsNullOrWhiteSpace(sentence))
            {
                CPH.SendMessage(sentence);
            }
            messageCounter = 0;
        }

        SaveTransitions();

        return true;
    }

    private void LearnFromChat(string message)
    {
        var words = message.Split(' ');

        if (words.Length < 3)
            return;

        for (int i = 0; i < words.Length - 2; i++)
        {
            var key = $"{words[i]}|{words[i + 1]}";
            var nextWord = words[i + 2];

            if (!transitions.ContainsKey(key))
            {
                transitions[key] = new List<string>();
                CPH.LogDebug($"Learning new key: {key}");
            }

            transitions[key].Add(nextWord);
            CPH.LogDebug($"Adding transition: [{key}] -> {nextWord}");
        }
    }

    private string GenerateSentence(int maxWords = 20)
    {
        if (transitions.Count == 0)
            return "";

        var keys = new List<string>(transitions.Keys);
        string currentKey = keys[rng.Next(keys.Count)];
        var parts = currentKey.Split('|');
        string result = $"{parts[0]} {parts[1]}";

        CPH.LogDebug($"Starting generation with key: {currentKey}");

        for (int i = 0; i < maxWords; i++)
        {
            if (!transitions.ContainsKey(currentKey) || transitions[currentKey].Count == 0)
                break;

            string nextWord = transitions[currentKey][rng.Next(transitions[currentKey].Count)];
            result += " " + nextWord;

            CPH.LogDebug($"Transition [{currentKey}] -> {nextWord}");

            currentKey = $"{parts[1]}|{nextWord}";
            parts = currentKey.Split('|');
        }

        return result;
    }

    private void SaveTransitions()
    {
        try
        {
            string folder = Path.GetDirectoryName(saveFilePath);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string json = JsonConvert.SerializeObject(transitions);
            File.WriteAllText(saveFilePath, json);
            CPH.LogDebug("Saved transitions to file.");
        }
        catch (Exception ex)
        {
            CPH.LogError("Failed to save Markov transitions: " + ex.Message);
        }
    }

    private void LoadTransitions()
    {
        try
        {
            if (File.Exists(saveFilePath))
            {
                string json = File.ReadAllText(saveFilePath);
                transitions = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
                CPH.LogDebug($"Loaded {transitions.Count} transitions.");
            }
            else
            {
                CPH.LogDebug("No existing Markov brain file found.");
            }
        }
        catch (Exception ex)
        {
            CPH.LogError("Failed to load Markov transitions: " + ex.Message);
        }
    }
}
