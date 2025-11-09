// RepairStation.cs
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;


public class RepairStation : MonoBehaviour
{
  
    private string playerTag = "Player";
    private bool requireAxlePin = true;
    public float repairDuration = 1.5f;

    [Header("UI")]
    public GameObject repairPanel;           // small UI (inactive by default)
    public TextMeshProUGUI repairText;       // e.g. "Press E to repair" / "Repairing..."
    public UnityEngine.UI.Slider progressBar; // optional progress bar

    [Header("Outcome")]
    public NPC onRepairSpeaker;              // optional NPC to trigger dialogue/response
    public Dialogue repairSuccessDialogue;   // optional Dialogue to play on success

    // internal
    private bool playerInZone = false;
    private bool isRepairing = false;
    public bool carriageRepaired = false;

    void Start()
    {
        if (repairPanel != null) repairPanel.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInZone = true;
        ShowPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInZone = false;
        HidePrompt();
        // optionally cancel ongoing repair if you want:
        // if (isRepairing) StopCoroutine("RepairCoroutine");
    }

    void Update()
    {
        if (!playerInZone || isRepairing || carriageRepaired) return;

        bool pressed = false;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame) pressed = true;
        }

        if (pressed) AttemptRepair();
    }

    void ShowPrompt()
    {
        if (repairPanel != null)
        {
            repairPanel.SetActive(true);
            if (repairText != null)
            {
                if (requireAxlePin && GameState.Instance != null && !GameState.Instance.playerHasAxlePin)
                    repairText.text = "You need an axle pin to repair.";
                else
                    repairText.text = "Press E to repair the carriage.";
            }
        }
    }

    void HidePrompt()
    {
        if (repairPanel != null) repairPanel.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
    }

    void AttemptRepair()
    {
        // check preconditions
        if (requireAxlePin && (GameState.Instance == null || !GameState.Instance.playerHasAxlePin))
        {
            if (repairText != null) repairText.text = "You don't have an axle pin.";
            return;
        }

        // consume pin & start repair
        if (GameState.Instance != null) GameState.Instance.playerHasAxlePin = false;
        isRepairing = true;
        StartCoroutine(RepairCoroutine());
    }

    IEnumerator RepairCoroutine()
    {
        // UI setup
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(true);
            progressBar.maxValue = repairDuration;
            progressBar.value = 0f;
        }
        if (repairText != null) repairText.text = "Repairing...";

        float elapsed = 0f;
        while (elapsed < repairDuration)
        {
            elapsed += Time.deltaTime;
            if (progressBar != null) progressBar.value = elapsed;
            yield return null;
        }

        // finish
        carriageRepaired = true;
        isRepairing = false;
        if (repairText != null) repairText.text = "Repaired!";
        if (progressBar != null) progressBar.gameObject.SetActive(false);

        if (MemoryManager.I != null)
        {
            foreach (var id in MemoryManager.I.GetAllNpcIds())
            {
                MemoryManager.I.AddFact(id, new MemoryFact("carriage_repaired", "player_repaired_carriage", 9));
                Debug.Log($"RepairStation: added carriage_repaired fact to {id}");
            }
        }
        else
        {
            Debug.LogWarning("RepairStation: MemoryManager.I is null; cannot record carriage_repaired fact.");
        }

        // small delay so player sees the text
        yield return new WaitForSeconds(0.6f);
        HidePrompt();

        // notify other systems
        Debug.Log("RepairStation: carriage repaired.");

        if (onRepairSpeaker != null && repairSuccessDialogue != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.PlayDialogue(onRepairSpeaker, repairSuccessDialogue);
        }
    }
}
