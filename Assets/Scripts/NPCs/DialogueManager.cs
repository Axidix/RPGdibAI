using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    public TextMeshProUGUI dialogueText;    // assign DialogueText UI
    public TextMeshProUGUI speakerText;     // optional: assign SpeakerName UI
    public GameObject dialoguePanel;        // background panel to enable/disable

    private Coroutine currentDialogueCoroutine;
    private NPC currentSpeakerNPC;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PlayDialogue(NPC speaker, Dialogue dialogue)
    {
        // If already playing and it's the same NPC, ignore or restart
        if (currentSpeakerNPC == speaker && currentDialogueCoroutine != null)
            return;

        StopDialogue(); // stop whatever was playing
        currentSpeakerNPC = speaker;
        currentDialogueCoroutine = StartCoroutine(PlayDialogueCoroutine(dialogue));
    }

    public void StopDialogue()
    {
        if (currentDialogueCoroutine != null)
        {
            StopCoroutine(currentDialogueCoroutine);
            currentDialogueCoroutine = null;
        }
        currentSpeakerNPC = null;
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    private IEnumerator PlayDialogueCoroutine(Dialogue dialogue)
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        foreach (var line in dialogue.lines)
        {
            if (speakerText != null) speakerText.text = line.speaker;
            if (dialogueText != null) dialogueText.text = line.text;

            // optionally: start audio here (if line.audio != null) and stop it if interrupted

            float elapsed = 0f;
            while (elapsed < line.duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // before continuing, check if we've been interrupted (currentSpeakerNPC null)
            if (currentDialogueCoroutine == null || currentSpeakerNPC == null)
            {
                // interrupted
                yield break;
            }
        }

        // finished
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        currentDialogueCoroutine = null;
        currentSpeakerNPC = null;
    }

    // optional helper used by NPC triggers:
    public void StopIfCurrentSpeaker(NPC npc)
    {
        if (currentSpeakerNPC == npc) StopDialogue();
    }
}
