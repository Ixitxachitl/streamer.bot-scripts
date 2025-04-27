# Streamer.bot Scripts by Ixitxachitl

A collection of powerful C# scripts for [Streamer.bot](https://streamer.bot/), bringing AI-powered conversations, playful chat humor, automatic translations, dynamic text generation, and live weather updates to your Twitch stream.

## 📜 Available Scripts

### 1. `askai.cs`
- **Purpose**: Lets viewers ask questions that get answered by an **AI chatbot running locally**.
- **Trigger**: `!askai your prompt here`
- **Backend**:
  - Connects to a **local GPT4All server** — compatible with the **GPT4All Desktop App** (listening at `http://localhost:4891/v1/chat/completions`).
  - Default model: `llama3-8b-instruct`.
- **Features**:
  - Short, concise responses for Twitch.
  - Auto-trims replies over 450 characters.
- **Requirements**:
  - GPT4All Desktop App running in server mode.
  - No external API key needed.

### 2. `buttsbot.cs`
- **Purpose**: Randomly and hilariously replaces a noun in a chat message with "butt".
- **Trigger**: Passive — listens to every chat message.
- **Backend**:
  - Uses **NLP Cloud API** for natural language parsing (POS tagging).
- **Features**:
  - 2% chance per message (configurable).
  - Debug mode (always triggers for testing).
  - Skips bot/self-messages.
- **Requirements**:
  - You **must** provide your **NLP Cloud API key** inside the script.
  - [Get an NLP Cloud API key here](https://nlpcloud.io/).

### 3. `markovchain.cs`
- **Purpose**: Learns from Twitch chat to generate funny new sentences using Markov chains.
- **Trigger**: Passive.
- **Behavior**:
  - Listens to user (not bot) messages.
  - Generates a random sentence every 35 messages.
  - Skips non-English or link-heavy messages.
  - Saves/loads its brain from `Documents/StreamerBot/markov_brain.json`.

### 4. `translate.cs`
- **Purpose**: Auto-translates non-English messages into English in real-time.
- **Trigger**: Passive.
- **Backend**:
  - Uses **Google Translate Free API** (`translate.googleapis.com`).
- **Features**:
  - Detects short known words like "oui", "ja", "ciao".
  - Filters spam and redundant translations.
  - Only shows translations from trusted Latin-alphabet languages.
  - Skips bot/self-messages.
- **Requirements**:
  - No API key needed.

### 5. `weather.cs`
- **Purpose**: Provides live weather updates for a given city.
- **Trigger**: `!weather cityname`
- **Backend**:
  - Uses the free **wttr.in** service — no API key required.
- **Features**:
  - Simple, fast, text-based weather retrieval.
  - User specifies the city in chat command.
- **Requirements**:
  - No API key needed.

## ⚡ Setup Instructions

1. Install [Streamer.bot](https://streamer.bot/).
2. In Streamer.bot:
   - Create a new **Action** for each script. Set the trigger for each Action to respond to **Twitch Chat Message** events as needed (e.g., `!askai`, `!weather`, or passive for others).
   - Inside each Action, add a **C# Sub-Action** and paste the corresponding script code.
   - **Important**: For `buttsbot.cs`, you must insert your **NLP Cloud API key** into the script at the marked location (`YOUR_API_KEY_HERE`) before it will function.
3. API Setup:
   - **askai.cs**: Install and run **GPT4All Desktop App** with server mode enabled (default port 4891).
   - **buttsbot.cs**: Ensure your NLP Cloud API key is inserted correctly.

## 🔥 Requirements

| Script            | Needs API Key?            | Extra Setup                         |
|-------------------|----------------------------|-------------------------------------|
| `askai.cs`        | ❌ No                    | GPT4All app running locally         |
| `buttsbot.cs`     | ✅ Yes (NLP Cloud)        | Insert your API key                 |
| `markovchain.cs`  | ❌ No                    | None                                |
| `translate.cs`    | ❌ No                    | None                                |
| `weather.cs`      | ❌ No                    | None                                |

## 📄 License

MIT License — Free to use, modify, and share.

## 🙌 Author

Created with ❤️ by **Ixitxachitl**  
Bringing AI and chaos to Twitch chat, one bot at a time.

