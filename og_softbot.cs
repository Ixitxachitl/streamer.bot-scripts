// --------------------------------------------------------------------------------------------------
// Project: og_softbot (Streamer.bot plugin)
// Description: Randomly detects adjective+noun pairs in Twitch chat and posts "I'd clap that adjective noun!" or "I'd clap those adjective noun!" if plural.
// Highlights:
//   - Uses NLP Cloud API for part-of-speech tagging
//   - 2% chance per message to trigger (configurable)
//   - Debug mode available to always trigger for testing
//   - Detects plural nouns and adjusts phrasing accordingly
//   - Skips bot/self messages to avoid loops
// Author: Ixitxachitl
// Date: 4/28/2025
// --------------------------------------------------------------------------------------------------

using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Text;

public class CPHInline
{
    private static readonly Random rng = new Random();
    private static readonly bool debugMode = false; // Set to true for debug mode (always trigger)
    private static readonly string nlpCloudApiKey = "YOUR_API_KEY_HERE"; // Replace with your NLP Cloud API Key

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

        (string adjective, string noun, bool isPlural) = await FindAdjectiveNounPairAsync(inputText);

        if (string.IsNullOrEmpty(adjective) || string.IsNullOrEmpty(noun))
        {
            CPH.LogDebug("No valid adjective+noun pair found.");
            return true;
        }

        string article = isPlural ? "those" : "that";
        string response = $"I'd clap {article} {adjective.ToLower()} {noun.ToLower()}!";

        CPH.LogDebug($"Sending message: {response}");
        CPH.SendMessage(response, true, true);

        return true;
    }

    private async Task<(string, string, bool)> FindAdjectiveNounPairAsync(string text)
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
                var words = json["words"] as JArray;

                if (words == null || words.Count == 0)
                    return (null, null, false);

                for (int i = 0; i < words.Count - 1; i++)
                {
                    var currentWord = words[i];
                    var nextWord = words[i + 1];

                    string currentTag = currentWord.Value<string>("tag") ?? "";
                    string nextTag = nextWord.Value<string>("tag") ?? "";

                    if (currentTag.StartsWith("JJ") && nextTag.StartsWith("NN"))
                    {
                        string adjective = currentWord.Value<string>("text") ?? "";
                        string noun = nextWord.Value<string>("text") ?? "";
                        bool isPlural = nextTag == "NNS" || nextTag == "NNPS";
                        return (adjective, noun, isPlural);
                    }
                }

                return (null, null, false);
            }
        }
        catch (Exception ex)
        {
            CPH.LogWarn($"ClapThatBot Analyze Error: {ex.Message}");
            return (null, null, false);
        }
    }

    private string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
