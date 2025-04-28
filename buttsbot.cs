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
        // Prevent the bot from reacting to its own messages
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

        List<string> nouns = await AnalyzeTextAsync(inputText);

        if (nouns == null || nouns.Count == 0)
        {
            CPH.LogDebug("No nouns detected in message.");
            return true;
        }

        string nounToReplace = nouns[rng.Next(nouns.Count)];

        CPH.LogDebug($"Replacing noun: {nounToReplace}");

        string funnyText = ReplaceFirstOccurrence(inputText, nounToReplace, "butt");

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
                var nounsList = new List<string>();

                var words = json["words"] as JArray;
                if (words != null)
                {
                    foreach (var wordObj in words)
                    {
                        string word = wordObj.Value<string>("text") ?? "";
                        string tag = wordObj.Value<string>("tag") ?? "";
                        if (tag.StartsWith("NN")) // NN, NNS, NNP, NNPS
                        {
                            nounsList.Add(word);
                            CPH.LogDebug($"Detected noun: {word}");
                        }
                    }
                }

                return nounsList;
            }
        }
        catch (Exception ex)
        {
            CPH.LogWarn($"Buttbot Analyze Error: {ex.Message}");
            return null;
        }
    }

    private string ReplaceFirstOccurrence(string source, string find, string replace)
    {
        int place = source.IndexOf(find, StringComparison.OrdinalIgnoreCase);
        if (place == -1)
            return source;

        string result = source.Substring(0, place) + replace + source.Substring(place + find.Length);
        return result;
    }

    private string Escape(string s)
    {
        return s.Replace("%", "%25").Replace("&", "%26").Replace("=", "%3D").Replace("+", "%2B").Replace(" ", "+");
    }

    private string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
