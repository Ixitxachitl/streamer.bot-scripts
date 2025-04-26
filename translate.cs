// --------------------------------------------------------------------------------------------------
// Project: Twitch Chat Translator (Streamer.bot plugin)
// Description: Automatically detects and translates non-English Twitch chat messages into English.
// Highlights:
//   - Uses free Google Translate API
//   - Auto-detects language or forces known short words (like "oui" → French)
//   - Skips identical input/output to avoid spam
//   - Supports trusted Latin-based languages only
//   - Fully regex-free cleaning for reliability
// Author: Ixitxachitl
// Date: 4/25/2025
// --------------------------------------------------------------------------------------------------
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

public class CPHInline
{
    private static readonly HttpClient client = new HttpClient();

    public bool Execute()
    {
        // Get incoming chat message and username
        string inputText = args["rawInput"].ToString();
        string userName = args["userName"].ToString().ToLower();

        // Skip if the bot itself sent the message
        var botInfo = CPH.TwitchGetBot();
        if (userName == botInfo.UserLogin.ToLower())
            return true;

        // Get the user's display name or fallback to username
        string displayName = args.ContainsKey("userDisplayName") ? args["userDisplayName"].ToString() : args["userName"].ToString();
        string trimmedInput = inputText.Trim().ToLower();
        string forcedLang = null;

        // Map known short words to specific languages
        var knownWords = new Dictionary<string, string>
        {
            { "si", "es" }, { "oui", "fr" }, { "no", "es" },
            { "ciao", "it" }, { "ja", "de" }, { "non", "fr" }
        };

        // If the input matches a known short word, force the language
        if (trimmedInput.Length <= 5 && knownWords.ContainsKey(trimmedInput))
            forcedLang = knownWords[trimmedInput];

        // Translate the input text
        (string translatedText, string sourceLang) = TranslateText(inputText, forcedLang).Result;

        if (string.IsNullOrWhiteSpace(translatedText))
            return true;

        bool forced = forcedLang != null;

        // Clean the input and translation for safe comparison
        string cleanedInput = CleanTextForComparison(inputText);
        string cleanedTranslation = CleanTextForComparison(translatedText);

        // Skip if input and output are identical after cleaning
        if (cleanedInput.Equals(cleanedTranslation, StringComparison.OrdinalIgnoreCase))
            return true;

        // Skip if translation is English and no language was forced
        if (!forced && sourceLang == "en")
            return true;

        // Only allow trusted Latin alphabet languages
        var trustedLatinLangs = new HashSet<string> { "es", "it", "pt", "de", "fr", "nl", "ro", "pl", "sv", "no", "da" };
        if (IsLatinAlphabet(inputText) && !trustedLatinLangs.Contains(sourceLang))
            return true;

        // Send translated message back to Twitch chat
        string fullLanguageName = GetLanguageDisplayName(sourceLang);
        CPH.SendMessage($"[Translated from {fullLanguageName}] {displayName}: {translatedText}", true, true);

        return true;
    }

    // Translate a text using Google Translate free API
    private async Task<(string, string)> TranslateText(string text, string forcedSourceLang = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (null, null);

        try
        {
            string slParam = forcedSourceLang ?? "auto";
            string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={slParam}&tl=en&dt=t&q={Uri.EscapeDataString(text)}";

            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            string response = await client.GetStringAsync(url);

            JArray json = JArray.Parse(response);
            string translatedText = "";

            if (json != null && json.Count > 0 && json[0] is JArray translationParts)
            {
                foreach (var part in translationParts)
                {
                    if (part is JArray segment && segment.Count > 0 && segment[0] != null)
                        translatedText += segment[0].ToString();
                }
            }

            string sourceLang = json.Count > 2 && json[2] != null ? json[2].ToString() : "unknown";
            return (translatedText.Trim(), sourceLang);
        }
        catch
        {
            return (null, null);
        }
    }

    // Get the full English display name for a language code
    private string GetLanguageDisplayName(string isoCode)
    {
        if (string.IsNullOrEmpty(isoCode))
            return "Unknown";

        var manualMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "iw", "Hebrew" },
            { "zh-cn", "Chinese (Simplified)" },
            { "zh-tw", "Chinese (Traditional)" },
            { "fil", "Filipino" }
        };

        if (manualMap.TryGetValue(isoCode, out string mapped))
            return mapped;

        try
        {
            var culture = new CultureInfo(isoCode);
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(culture.EnglishName);
        }
        catch
        {
            return isoCode.ToUpperInvariant();
        }
    }

    // Check if the text mainly uses Latin alphabet characters
    private bool IsLatinAlphabet(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return true;

        int nonLatinCount = 0, letterCount = 0;

        foreach (char c in text)
        {
            if (char.IsLetter(c))
            {
                letterCount++;
                if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= 0x00C0 && c <= 0x024F)))
                    nonLatinCount++;
            }
        }

        if (letterCount == 0)
            return true;

        return (double)nonLatinCount / letterCount < 0.2;
    }

    // Clean text by removing unwanted characters and normalizing spaces
    private string CleanTextForComparison(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        var cleanedChars = new List<char>();
        bool lastWasSpace = false;

        foreach (char c in text.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                cleanedChars.Add(c);
                lastWasSpace = false;
            }
            else if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace)
                {
                    cleanedChars.Add(' ');
                    lastWasSpace = true;
                }
            }
        }

        return new string(cleanedChars.ToArray()).Trim();
    }
}
