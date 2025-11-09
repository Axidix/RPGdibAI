using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MemoryFact {
    public string key;    // e.g. "helped_repair"
    [TextArea(1,3)]
    public string value;  // short description, <= 120 chars
    public long ts;       // unix ms timestamp
    public int importance; // 0..10 for pruning preference

    public MemoryFact() { }

    public MemoryFact(string k, string v, int imp = 5) {
        key = k ?? "";
        value = v == null ? "" : (v.Length > 120 ? v.Substring(0, 120) : v);
        ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        importance = Mathf.Clamp(imp, 0, 10);
    }
}

[Serializable]
public class NPCMemory {
    public string npcId;
    [TextArea(1,2)]
    public string personaLine; // inspector editable
    public int relationship;   // -5 .. +5
    public string roleState;   // idle|busy|guarding|grateful|suspicious etc.
    [TextArea(1,2)]
    public string goal;
    [TextArea(1,3)]
    public string shortSummary; // auto-updated <= 140 chars
    public long lastInteractionTs;
    public List<MemoryFact> knownFacts = new List<MemoryFact>();
    public List<string> convoLog = new List<string>(); // debug-only, cap default 8

    // Default ctor required for JsonUtility
    public NPCMemory() { }

    public NPCMemory(string id, string persona = "") {
        npcId = id ?? Guid.NewGuid().ToString();
        personaLine = persona ?? "";
        relationship = 0;
        roleState = "idle";
        goal = "";
        shortSummary = "";
        lastInteractionTs = 0;
        knownFacts = new List<MemoryFact>();
        convoLog = new List<string>();
    }
}
