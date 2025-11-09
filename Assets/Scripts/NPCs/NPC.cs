using UnityEngine;

public class NPC : MonoBehaviour
{
    [Header("Identification")]
    public string npcId;         // e.g. "merchant_01"
    public string displayName;   // e.g. "Merchant"
    public Dialogue greetingDialogue;    // set in inspector
    public bool hasAxlePin = false;

    [TextArea(1,2)]
    public string personaLine;
    private NPCMemory mem;

    void Start() {
        if (MemoryManager.I != null) {
            MemoryManager.I.GetOrCreate(npcId, personaLine);
        } else {
            Debug.LogWarning("MemoryManager.I is null. Check script order.");
        }
    }

    public void Interact()
    {
        Debug.Log($"Interacting with {displayName} (id: {npcId})");

        // record debug convo line
        if (MemoryManager.I != null) MemoryManager.I.AddConvoLine(npcId, "Player: interacted (greeting)");

        // if this NPC has axle pin, record it
        if (hasAxlePin && MemoryManager.I != null) {
            MemoryManager.I.AddFact(npcId, new MemoryFact("has_axle_pin", "true", 8));
        }

        // Build a quick memory-based reply (MVP rule-based)
        string playerAction = ""; // optionally set based on context (repair/gave_pin/etc.)
        string reply = MemoryManager.I != null ? MemoryManager.I.BuildReplyFromMemory(npcId, playerAction) : null;

        if (!string.IsNullOrEmpty(reply)) {
            Debug.Log($"{displayName}: {reply}");
        }

        // Then play normal greeting dialogue (if any)
        if (greetingDialogue != null && DialogueManager.Instance != null) {
            DialogueManager.Instance.PlayDialogue(this, greetingDialogue);
        }
    }

}
