// --------------------------------------------------------------------------------------------------
// Project: Smart NLP Buttsbot (Streamer.bot plugin)
// Description: Randomly replaces a noun in Twitch chat messages with the word "butt".
// Highlights:
//   - Uses NLP Cloud API for part-of-speech tagging
//   - 2% chance per message to trigger (configurable)
//   - Debug mode for always-on replacements
//   - Skips bot/self messages to avoid loops
// Author: Ixitxachitl
// Date: 4/25/2025
// --------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Text; // Needed for Encoding

public class CPHInline
{
    private static readonly Random rng = new Random();
    private static readonly bool debugMode = false; // Set to true for debug mode (always trigger)
    private static readonly string nlpCloudApiKey = "YOUR_API_KEY_HERE"; // Replace this with your NLP Cloud API Key

    public bool Execute()
    {
        return ExecuteAsync().GetAwaiter().GetResult();
    }

    public async Task<bool> ExecuteAsync()
    {
        var botInfo = CPH.TwitchGetBot();
        string botUsername = botInfo.UserLogin.ToLower();
        string inputUsername = args["userName"].ToString().ToLower();

        if (inputUsername == botUsername)
        {
            CPH.LogDebug("Ignoring bot's own message (userName matched bot login).");
            return true;
        }

        string inputText = args["rawInput"].ToString();
        string userName = args["userName"].ToString();

        CPH.LogDebug($"Received message from {userName}: {inputText}");

        if (!debugMode && rng.NextDouble() > 0.02)
        {
            CPH.LogDebug("Message skipped (did not trigger random 2% chance).");
            return true;
        }

        CPH.LogDebug("Message triggered! Analyzing text...");

        List<string> words = await AnalyzeTextAsync(inputText);

        if (words == null || words.Count == 0)
        {
            CPH.LogDebug("No nouns or adjectives detected in message.");
            return true;
        }

        string wordToReplace = words[rng.Next(words.Count)];

        CPH.LogDebug($"Replacing word: {wordToReplace}");

        bool plural = wordToReplace.EndsWith("s", StringComparison.OrdinalIgnoreCase);
        string replacement = plural ? "butts" : "butt";

        string funnyText = ReplaceAllOccurrences(inputText, wordToReplace, replacement);

        CPH.LogDebug($"Sending modified message: {funnyText}");

        CPH.SendMessage(funnyText, true, true);

        return true;
    }

    private async Task<List<string>> AnalyzeTextAsync(string text)
    {
        try
        {
            var url = "https://api.nlpcloud.io/v1/en_core_web_lg/dependencies";
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Headers.Add("Authorization", $"Token {nlpCloudApiKey}");

            string jsonBody = $"{{ \"text\": \"{EscapeJson(text)}\" }}";
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
            request.ContentLength = bodyBytes.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(bodyBytes, 0, bodyBytes.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                string jsonResponse = await reader.ReadToEndAsync();

                CPH.LogDebug($"NLP Cloud raw response: {jsonResponse}");

                var json = JObject.Parse(jsonResponse);
                var wordsList = new List<string>();

                var words = json["words"] as JArray;
                if (words != null)
                {
                    foreach (var wordObj in words)
                    {
                        string word = wordObj.Value<string>("text") ?? "";
                        string tag = wordObj.Value<string>("tag") ?? "";
                        if (tag.StartsWith("NN") || tag.StartsWith("JJ")) // Nouns or adjectives
                        {
                            wordsList.Add(word);
                            CPH.LogDebug($"Detected word: {word}");
                        }
                    }
                }

                return wordsList;
            }
        }
        catch (Exception ex)
        {
            CPH.LogWarn($"Buttbot Analyze Error: {ex.Message}");
            return null;
        }
    }

    private string ReplaceAllOccurrences(string source, string find, string replace)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            source,
            $"\\b{System.Text.RegularExpressions.Regex.Escape(find)}\\b",
            replace,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
    }

    private string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
