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
        StopEncounter();
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
            Debug.Log("BanditEncounter: success - axle pin acquired.");

            // optional: play short victory dialogue
            if (DialogueManager.Instance != null && npc != null)
            {
                // If you want a special victory dialogue, use a separate Dialogue on the NPC.
                DialogueManager.Instance.PlayDialogue(npc, npc.greetingDialogue);
                EventHelpers.RecordAxlePinTaken(npc.npcId);
            }
        }
        else
        {
            if (messageText != null) messageText.text = "Attack failed — the bandit escapes!";
            Debug.Log("BanditEncounter: attack failed.");
        }

        // auto-close after a short delay so player sees the message
        Invoke(nameof(StopEncounter), 1.4f);
    }
}
