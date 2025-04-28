// --------------------------------------------------------------------------------------------------
// Project: Twitch AI Chatbot (Streamer.bot plugin)
// Description: Sends user prompts to a local GPT4All server and returns AI-generated replies.
// Highlights:
//   - Uses local GPT4All server (no API key needed)
//   - Short, concise, Twitch-friendly AI responses
//   - Automatic character limit handling (max 450 characters)
//   - Robust error handling for AI server downtime
// Author: Ixitxachitl
// Date: 4/25/2025
// -------------------------------------------------------------------------------------------------
using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    public bool Execute()
    {
        if (!CPH.TryGetArg("rawInput", out string userPrompt) || string.IsNullOrWhiteSpace(userPrompt))
        {
            CPH.SendMessage("You need to provide a prompt after !askai!");
            return false;
        }

        var httpClient = new HttpClient();
        var url = "http://localhost:4891/v1/chat/completions";

        var payload = new
        {
            model = "llama3-8b-instruct",
            max_tokens = 130,
            messages = new[]
            {
            	new { role = "system", content = "You are a helpful Twitch Chatbot. Keep responses short and concise." },
                new { role = "user", content = userPrompt }
            }
        };

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = httpClient.PostAsync(url, content).Result;
            var responseString = response.Content.ReadAsStringAsync().Result;

            JObject parsedResponse = JObject.Parse(responseString);

            string reply = parsedResponse["choices"]?[0]?["message"]?["content"]?.ToString();

            if (!string.IsNullOrEmpty(reply))
            {
                if (reply.Length > 450)
                    reply = reply.Substring(0, 450) + "...";

                CPH.SendMessage(reply.Trim());
            }
            else
            {
                CPH.SendMessage("Sorry, no reply from AI.");
                CPH.LogWarn("AI returned no reply. Full response: " + responseString);
            }
        }
        catch (Exception ex)
        {
            CPH.SendMessage("Error contacting AI. Check logs!");
            CPH.LogError("Exception in AI call: " + ex.ToString());
        }

        return true;
    }
}
