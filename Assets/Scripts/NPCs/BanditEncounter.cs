// BanditEncounter.cs
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class BanditEncounter : MonoBehaviour
{
    [Header("UI")]
    public GameObject encounterPanel;       // small UI panel (inactive by default)
    public TextMeshProUGUI messageText;     // TMP text inside the panel

    [Header("Settings")]
    [Range(0f,1f)] public float successRate = 0.7f;

    // references
    private NPC npc;                        // cached NPC component
    private bool playerInZone = false;
    private bool encounterActive = false;

    void Awake()
    {
        npc = GetComponent<NPC>();
        if (encounterPanel != null) encounterPanel.SetActive(false);
    }

    // If your NPC already has a trigger collider: use these enter/exit callbacks
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInZone = true;
        EventHelpers.RecordBanditWitnesses();
        StartEncounter();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInZone = false;
        Invoke(nameof(StopEncounter), 2.0f);
    }

    void Update()
    {
        if (!encounterActive || !playerInZone) return;

        bool pressed = false;

        
        if (Keyboard.current != null)
        {
            if (Keyboard.current.fKey.wasPressedThisFrame) pressed = true;
        }

        if (pressed) ResolveAttack();
    }

    void StartEncounter()
    {
        encounterActive = true;
        if (encounterPanel != null)
        {
            encounterPanel.SetActive(true);
            if (messageText != null) messageText.text = "A bandit appears — Press F to attack";
        }
    }

    void StopEncounter()
    {
        encounterActive = false;
        if (encounterPanel != null) encounterPanel.SetActive(false);
        // If the bandit's dialogue was playing, stop it as player left:
        if (DialogueManager.Instance != null && npc != null)
            DialogueManager.Instance.StopIfCurrentSpeaker(npc);
    }

    void ResolveAttack()
    {
        bool success = Random.value <= successRate;

        if (success)
        {
            if (npc != null) npc.hasAxlePin = false;                 // mark bandit lost the pin
            if (GameState.Instance != null) GameState.Instance.playerHasAxlePin = true;

            if (messageText != null) messageText.text = "You win! The bandit drops an axle pin.";
            //Debug.Log("BanditEncounter: success - axle pin acquired.");
            if (MemoryManager.I != null)
            {
                MemoryManager.I.ResetAllInteractionCounters();
                //Debug.Log("[BanditEncounter] Bandit defeated → reset all NPC interaction counters.");
            }
            
            if (DialogueManager.Instance != null && npc != null)    // === AI-GENERATED DEFEAT LINE ===
            {
                // If we have an LLM and a key, ask it for a defeat line
                if (LLMClient.I != null && !string.IsNullOrEmpty(LLMClient.I.apiKey))
                {
                    string playerAction = "You just lost a combat against the player, who takes the axle pin.";
                    LLMClient.I.GenerateReply(npc.npcId, playerAction, (reply, ok) =>
                    {
                        string finalText;
                        if (ok && !string.IsNullOrEmpty(reply))
                        {
                            //Debug.Log($"{npc.displayName} (bandit defeat AI): {reply}");
                            finalText = reply;
                        }
                        else
                        {
                            finalText = "Tch... you got lucky this time!";
                            //Debug.Log($"{npc.displayName} (bandit defeat fallback): {finalText}");
                        }
                        DialogueLine line = new DialogueLine
                        {
                            speaker = npc.displayName,
                            text = finalText,
                            duration = 3f
                        };
                        Dialogue aiDialogue = new Dialogue{lines = new DialogueLine[] { line }};
                        DialogueManager.Instance.PlayDialogue(npc, aiDialogue);
                    });
                }
                else
                {
                    // No LLM → keep old predefined dialogue
                    DialogueManager.Instance.PlayDialogue(npc, npc.greetingDialogue);
                }
            }
        }
        else
        {
            if (messageText != null) messageText.text = "Attack failed — the bandit escapes!";
            //Debug.Log("BanditEncounter: attack failed.");
        }

        // auto-close after a short delay so player sees the message
        Invoke(nameof(StopEncounter), 4.0f);
    }
}
