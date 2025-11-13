using UnityEngine;

public class NPC : MonoBehaviour
{
    [Header("Identification")]
    public string npcId;         // e.g. "merchant_01"
    public string displayName;   // e.g. "Merchant"
    public Dialogue greetingDialogue;    // fallback dialogue
    public bool hasAxlePin = false;

    [TextArea(1,2)]
    public string personaLine;
    [TextArea(1,2)]
    public string roleLine;


    private NPCMemory mem;

    void Start()
    {
        if (MemoryManager.I != null)
        {
            MemoryManager.I.GetOrCreate(npcId, personaLine, roleLine);
        }
        else
        {
            Debug.LogWarning("MemoryManager.I is null. Check script execution order.");
        }
    }

    public void Interact()
    {
        Debug.Log($"[NPC] Interacting with {displayName} (id: {npcId})");

        var memObj = MemoryManager.I?.GetMemory(npcId);
        if (memObj != null)
        {
            memObj.consecutiveInteractions++;
        }
        Debug.Log($"[NPC] {displayName} has been talked to {memObj.consecutiveInteractions} times in a row.");

        // memory logging
        if (MemoryManager.I != null)
        {
            MemoryManager.I.AddConvoLine(npcId, "Player: interacted (greeting)");

            if (hasAxlePin)
                MemoryManager.I.AddFact(npcId, new MemoryFact("has_axle_pin", "true", 8));
        }

        string playerAction = ""; // can be expanded later (fight, repair, etc.)

        // No API key? Go to fallback immediately
        if (LLMClient.I == null || string.IsNullOrEmpty(LLMClient.I.apiKey))
        {
            Debug.LogWarning("[NPC] No API key → fallback dialogue.");
            PlayFallbackDialogue(npcId, playerAction);
            return;
        }

        // Request AI-generated reply
        Debug.Log("[NPC] Sending request to LLM...");
        LLMClient.I.GenerateReply(npcId, playerAction, (reply, ok) =>
        {
            if (!ok || string.IsNullOrEmpty(reply))
            {
                Debug.LogWarning("[NPC] AI failed → using fallback.");
                PlayFallbackDialogue(npcId, playerAction);
                return;
            }

            // Debug: show AI reply
            Debug.Log($"{displayName} (AI): {reply}");
            MemoryManager.I?.AddConvoLine(npcId, $"NPC: {reply}");

            // Display reply in DialogueManager
            DialogueLine aiLine = new DialogueLine
            {
                speaker = displayName,
                text = reply,
                duration = 4f
            };

            Dialogue aiDialogue = new Dialogue
            {
                lines = new DialogueLine[] { aiLine }
            };

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.PlayDialogue(this, aiDialogue);
            }
        });
    }

    private void PlayFallbackDialogue(string npcId, string playerAction)
    {
        string fallback = MemoryManager.I != null ?
            MemoryManager.I.BuildReplyFromMemory(npcId, playerAction) :
            "They nod.";

        Debug.Log($"{displayName} (fallback): {fallback}");

        DialogueLine fallbackLine = new DialogueLine
        {
            speaker = displayName,
            text = fallback,
            duration = 3f
        };

        Dialogue fallbackDialogue = new Dialogue
        {
            lines = new DialogueLine[] { fallbackLine }
        };

        if (DialogueManager.Instance != null)
        {
            if (greetingDialogue != null)
            {
                // OPTIONAL: play the defined greeting if it exists
                DialogueManager.Instance.PlayDialogue(this, fallbackDialogue);
            }
            else
            {
                DialogueManager.Instance.PlayDialogue(this, fallbackDialogue);
            }
        }
    }
}
