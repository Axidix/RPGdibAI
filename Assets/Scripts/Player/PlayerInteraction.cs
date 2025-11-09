using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;


public class PlayerInteraction : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI promptText;             // assign "Press E to talk" Text (or TMPro)

    [Header("Settings")]
    public string npcTag = "NPC";         // tag used for NPCs
    public KeyCode interactKey = KeyCode.E;

    // internal
    private readonly List<NPC> nearbyNpcs = new List<NPC>();

    void Start()
    {
        if (promptText != null) promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (nearbyNpcs.Count > 0)
        {
            // show prompt
            if (promptText != null) promptText.gameObject.SetActive(true);

            // interact on key press with nearest NPC
            if (Keyboard.current.eKey.wasPressedThisFrame)  
            {
                NPC target = GetNearestNPC();
                if (target != null) target.Interact();
            }
        }
        else
        {
            if (promptText != null) promptText.gameObject.SetActive(false);
        }
    }

    private NPC GetNearestNPC()
    {
        NPC nearest = null;
        float bestDistSqr = float.MaxValue;
        Vector2 myPos = transform.position;

        foreach (var npc in nearbyNpcs)
        {
            if (npc == null) continue;
            float d = (npc.transform.position - (Vector3)myPos).sqrMagnitude;
            if (d < bestDistSqr)
            {
                bestDistSqr = d;
                nearest = npc;
            }
        }
        return nearest;
    }

    // Using 2D triggers:
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(npcTag))
        {
            NPC npc = other.GetComponent<NPC>();
            if (npc != null && !nearbyNpcs.Contains(npc))
            {
                nearbyNpcs.Add(npc);
                //Debug.Log("NPC added to nearby list");
            }

        }
        //Debug.Log(nearbyNpcs.Count + " NPC(s) nearby. One got added.");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(npcTag))
        {
            NPC npc = other.GetComponentInParent<NPC>();
            if (npc != null)
            {
                nearbyNpcs.Remove(npc);
                // If the player leaves this NPC's zone, stop its dialogue if it's playing
                DialogueManager.Instance.StopIfCurrentSpeaker(npc);
            }
        }
    }
}
