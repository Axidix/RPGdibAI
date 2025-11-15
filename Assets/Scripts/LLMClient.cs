using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using System.Linq;


public class LLMClient : MonoBehaviour {
    public static LLMClient I { get; private set; }

    [Header("HuggingFace Inference API")]
    [Tooltip("Model name on HF.")]
    public string model = "google/gemma-2-9b-it";   // or Qwen/Qwen2-1.5B-Instruct
    [Tooltip("Hugging Face API token (set in inspector)")]
    public string apiKey = "";
    [Tooltip("Temperature (0.0 - 1.0)")]
    public float temperature = 0.7f;
    [Tooltip("seconds")]
    public int timeoutSeconds = 6;

    void Awake() {
        if (I != null && I != this) {
            Destroy(this);
            return;
        }
        I = this;
    }

    // Generate a reply for npc/playerAction. onComplete called with (replyText, success)
    public void GenerateReply(string npcId, string playerAction, Action<string, bool> onComplete) {
        StartCoroutine(GenerateCoroutine(npcId, playerAction, onComplete));
    }

    IEnumerator GenerateCoroutine(string npcId, string playerAction, Action<string, bool> onComplete) {
        if (string.IsNullOrEmpty(apiKey)) {
            onComplete?.Invoke("LLM API key missing.", false);
            yield break;
        }

        // build compact prompt from memory + context
        string prompt = BuildPrompt(npcId, playerAction);

        bool useChatPayload = true;

        string url = "https://router.huggingface.co/v1/chat/completions";

        string bodyJson;

        if (useChatPayload) {
            // chat-style payload
            JSONObject payload = new JSONObject();
            payload["model"] = model;

            JSONArray messages = new JSONArray();
            {
                JSONObject sys = new JSONObject();
                sys["role"] = "system";
                sys["content"] =
                    "You are an NPC in a small survival scenario. " +
                    "A merchant's cart is broken; the player and mercenary are " +
                    "stranded until the axle pin is recovered from a nearby bandit. " +
                    "Always reply in natural dialogue as if you were the character. " +
                    "No quotation marks. No narration. No book-style writing.";
            }
            {
                JSONObject usr = new JSONObject();
                usr["role"] = "user";
                usr["content"] = prompt;
                messages.Add(usr);
            }
            payload["messages"] = messages;

            payload["max_tokens"] = 60;
            payload["temperature"] = temperature;

            bodyJson = payload.ToString();
        }
        else {
            // text-generation-style payload
            JSONObject payload = new JSONObject();
            payload["inputs"] = prompt;
            JSONObject parameters = new JSONObject();
            parameters["max_new_tokens"] = 60;
            parameters["temperature"] = temperature;
            payload["parameters"] = parameters;

            bodyJson = payload.ToString();
        }

        byte[] body = Encoding.UTF8.GetBytes(bodyJson);

        using (UnityWebRequest uwr = new UnityWebRequest(url, "POST")) {
            uwr.uploadHandler = new UploadHandlerRaw(body);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.SetRequestHeader("Authorization", "Bearer " + apiKey);
            uwr.timeout = timeoutSeconds;

            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success) {
                Debug.LogWarning($"LLMClient: request failed: {uwr.error}");
                onComplete?.Invoke(null, false);
                yield break;
            }

            string resp = uwr.downloadHandler.text;
            string generated = ParseGeneratedTextSimpleJSON(resp);

            if (string.IsNullOrEmpty(generated)) {
                Debug.LogWarning("LLMClient: couldn't parse structured response, returning raw text.");
                generated = resp;
            }
            generated = CleanLLMOutput(generated);
            onComplete?.Invoke(generated.Trim(), true);
        }
    }

    string BuildPrompt(string npcId, string playerAction)
    {
        var sb = new StringBuilder();
        var mem = MemoryManager.I?.GetOrCreate(npcId);
        string persona = mem?.personaLine ?? "";
        int timesTalked = mem?.consecutiveInteractions ?? 1;

        // NPC memory block
        string snippet = MemoryManager.I?.GetPromptSnippet(npcId) ?? "";

        // ===== WORLD STATE BASED ON FACTS =====
        bool repaired = mem?.knownFacts.Any(f => f.key == "carriage_repaired") ?? false;
        bool hasAxlePin = mem?.knownFacts.Exists(f => f.key == "gave_axle_pin") ?? false;
        bool banditSeen = mem?.knownFacts.Any(f => f.key == "saw_bandit") ?? false;

        sb.AppendLine("World State:");
        if (repaired) sb.AppendLine("- The carriage has already been repaired.");
        else sb.AppendLine("- The carriage is still broken.");
        if (hasAxlePin) sb.AppendLine("- The player holds the axle pin needed to repair the carriage.");
        if (banditSeen) sb.AppendLine("- Someone spotted a bandit earlier.");

        // ===== NPC IDENTITY =====
        sb.AppendLine($"You are an NPC (id:{npcId}).");
        if (!string.IsNullOrEmpty(persona))
            sb.AppendLine($"Personality: {persona}");
        if (!string.IsNullOrEmpty(mem?.roleLine))
            sb.AppendLine($"Role: {mem.roleLine}");

        // ===== NPC MEMORY =====
        if (!string.IsNullOrEmpty(snippet))
            sb.AppendLine($"Memory: {snippet}");

        // ===== PLAYER ACTION =====
        if (!string.IsNullOrEmpty(playerAction))
            sb.AppendLine($"Recent player action: {playerAction}");

        // ===== ANNOYANCE =====
        if (timesTalked >= 2)
        {
            sb.AppendLine($"The player has spoken to you {timesTalked} time(s) in a row.");
            string mood = timesTalked switch
            {
                2 => "slightly puzzled the player already came back.",
                3 => "a bit impatient from repeated questions.",
                4 => "visibly annoyed by the repeated interruptions.",
                _ => "very irritated after being spoken to so many times in a row."
            };

            sb.AppendLine($"Your tone should reflect that you are {mood} But you still respond based on the current situation and world events.");
        }

        sb.AppendLine(
            "Your answer MUST strongly reflect your personality, your role, and the exact current world situation, even if it feels exaggerated or caricatural."
        );
        sb.AppendLine(
            "Respond in 1–2 short sentences. No narration, no quotation marks—just what the NPC says."
        );

        Debug.Log($"LLMClient: built prompt:\n{sb.ToString()}");
        return sb.ToString();
    }

    string ParseGeneratedTextSimpleJSON(string rawJson) {
        if (string.IsNullOrEmpty(rawJson)) return null;
        try {
            JSONNode token = JSON.Parse(rawJson);

            // 1) choices array (chat / openai-like)
            if (token.HasKey("choices")) {
                JSONArray choices = token["choices"].AsArray;
                if (choices != null && choices.Count > 0) {
                    JSONNode first = choices[0];
                    if (first.HasKey("message") && first["message"].HasKey("content")) {
                        return first["message"]["content"];
                    }
                    if (first.HasKey("delta") && first["delta"].HasKey("content")) {
                        return first["delta"]["content"];
                    }
                    if (first.HasKey("text")) {
                        return first["text"];
                    }
                }
            }

            // 2) If root is array
            if (token.IsArray) {
                JSONArray arr = token.AsArray;
                if (arr.Count > 0 && arr[0].HasKey("generated_text")) {
                    return arr[0]["generated_text"];
                }
            }

            // 3) top-level generated_text
            if (token.HasKey("generated_text")) {
                return token["generated_text"];
            }

            // 4) fallback – maybe result/text/content
            if (token.HasKey("result")) {
                return token["result"];
            }
            if (token.HasKey("text")) {
                return token["text"];
            }
            if (token.HasKey("content")) {
                return token["content"];
            }

            return null;
        } catch (Exception e) {
            Debug.LogWarning("ParseGeneratedTextSimpleJSON: JSON parse error: " + e.Message);
            return null;
        }
    }

    string CleanLLMOutput(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw;

        // Extract first quoted dialog if it exists
        int first = raw.IndexOf('"');
        int last = raw.LastIndexOf('"');

        if (first != -1 && last > first)
        {
            return raw.Substring(first + 1, last - first - 1).Trim();
        }

        // Otherwise remove all quotes and narration-like patterns
        raw = raw.Replace("\"", "");

        // Remove book-style patterns like: He said, or She whispered.
        raw = System.Text.RegularExpressions.Regex.Replace(
            raw,
            @"\b(he|she|they|I)(\s+\w+ed\b).*", // matches "He murmured..." etc.
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );

        return raw.Trim();
    }

}
