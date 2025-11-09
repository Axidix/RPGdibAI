using System.Collections.Generic;
using UnityEngine;

// Call these functions from your existing bandit/loot/repair code.

public static class EventHelpers
{
    static bool banditWitnessesRecorded = false;
    public static void RecordBanditWitnesses()
    {  
        if (banditWitnessesRecorded) return; // only once per bandit encounter
        banditWitnessesRecorded = true;
        if (MemoryManager.I != null)
        {
            foreach (var id in MemoryManager.I.GetAllNpcIds())
            {
                MemoryManager.I.AddFact(id, new MemoryFact("saw_bandit", "player_defeated_bandit", 9));
                MemoryManager.I.AdjustRelationship(id, +1);
                Debug.Log($"BanditEncounter: added saw_bandit fact to {id}");
            }
        }
        else
        {
            Debug.LogWarning("BanditEncounter: MemoryManager.I is null; cannot record witnesses.");
        }
    }

    // 2) Record that an NPC gave the axle pin to player (call when player obtains it)
    public static void RecordAxlePinTaken(string fromNpcId)
    {
        if (MemoryManager.I == null || string.IsNullOrEmpty(fromNpcId)) return;
        MemoryManager.I.AddFact(fromNpcId, new MemoryFact("has_axle_pin", "false", 9));
        MemoryManager.I.AddFact(fromNpcId, new MemoryFact("gave_axle_pin", "player_took_axle_pin", 9));
        MemoryManager.I.AdjustRelationship(fromNpcId, -1); // optional tweak
    }

    // 3) Record that player gave axle pin to NPC (call when transferring to an NPC)
    public static void RecordAxlePinGiven(string toNpcId)
    {
        if (MemoryManager.I == null || string.IsNullOrEmpty(toNpcId)) return;
        MemoryManager.I.AddFact(toNpcId, new MemoryFact("gave_axle_pin", "player_gave_axle_pin", 9));
        MemoryManager.I.AddFact(toNpcId, new MemoryFact("has_axle_pin", "true", 9));
        MemoryManager.I.AdjustRelationship(toNpcId, +2);
    }
}
