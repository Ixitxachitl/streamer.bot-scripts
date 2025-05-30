// --------------------------------------------------------------------------------------------------
// Project: Live Weather Fetcher (Streamer.bot plugin)
// Description: Provides current weather information for a given city on request.
// Highlights:
//   - Uses the free wttr.in weather service (no API key needed)
//   - Simple and fast text-based responses
//   - Supports any city input from Twitch chat
//   - Lightweight with error handling for invalid cities
// Author: Ixitxachitl
// Date: 4/25/2025
// --------------------------------------------------------------------------------------------------
using System;
using System.Net.Http;
using System.Threading.Tasks;

public class CPHInline
{
    private static readonly HttpClient httpClient = new HttpClient();

    public bool Execute()
    {
        return ExecuteAsync().GetAwaiter().GetResult();
    }

    private async Task<bool> ExecuteAsync()
    {
        if (!CPH.TryGetArg("rawInput", out string city) || string.IsNullOrWhiteSpace(city))
        {
            CPH.SendMessage("Usage: !weather cityname");
            return false;
        }

        try
        {
            string url = $"https://wttr.in/{Uri.EscapeDataString(city)}?format=3";
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("StreamerBotWeatherScript/1.0");

            string weather = await httpClient.GetStringAsync(url);

            if (!string.IsNullOrWhiteSpace(weather))
            {
                CPH.SendMessage($"Weather for {weather}");
            }
            else
            {
                CPH.SendMessage($"Could not retrieve weather for {city}.");
            }
        }
        catch (Exception ex)
        {
            CPH.LogError("Weather lookup failed: " + ex.Message);
            CPH.SendMessage("Error getting weather, try again later!");
        }

        return true;
    }
}
