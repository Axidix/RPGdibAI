using UnityEngine;
using TMPro;

public class PersonaSetup : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    public TMP_InputField banditInput;
    public TMP_InputField merchantInput;
    public TMP_InputField mercenaryInput;

    [Header("NPC Ids")]
    public string banditId = "bandit_01";
    public string merchantId = "merchant_01";
    public string mercenaryId = "mercenary_01";

    // Player should NOT move until this is true
    public static bool ControlsEnabled = false;

    void Start()
    {
        // Make sure panel is visible and controls are blocked at start
        ControlsEnabled = false;
        if (panel != null) panel.SetActive(true);
    }

    public void StartGame()
    {
        // Store persona lines for each NPC
        if (MemoryManager.I != null)
        {
            MemoryManager.I.SetPersonaLine(banditId, banditInput.text);
            MemoryManager.I.SetPersonaLine(merchantId, merchantInput.text);
            MemoryManager.I.SetPersonaLine(mercenaryId, mercenaryInput.text);
        }

        // ✅ Now allow player control
        ControlsEnabled = true;

        // Hide the panel and allow gameplay
        if (panel != null) panel.SetActive(false);

        Debug.Log("Persona Setup Complete. Controls enabled.");
    }
}
