using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

/// <summary>
/// MemoryManager: singleton in-Unity memory store for NPCs.
/// Drop this on a GameObject in the scene (e.g., "Managers").
/// </summary>
public class MemoryManager : MonoBehaviour {
    public static MemoryManager I { get; private set; }

    [Header("Persistence")]
    public string saveFileName = "npc_memories.json";
    public bool autosave = true;
    [Header("Dev / Persistence")]
    public bool resetOnStart = true;   // set true during demo so json is fresh each run

    public float autosaveInterval = 10f;

    [Header("Limits")]
    public int maxFactsPerNpc = 6;
    public int maxConvoLog = 8;
    public int maxSummaryLength = 140;

    // internal storage
    Dictionary<string, NPCMemory> memories = new Dictionary<string, NPCMemory>();

    // convenience
    string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);
    float saveTimer = 0f;

    void Awake() {
        if (I != null && I != this) { Destroy(this); return; }
        I = this;
        LoadAll();

        if (resetOnStart) {
            // remove saved file and clear loaded memories to start fresh
            try {
                if (File.Exists(SavePath)) {
                    File.Delete(SavePath);
                    Debug.Log("MemoryManager: deleted existing save file for fresh run: " + SavePath);
                }
            } catch (Exception e) {
                Debug.LogWarning("MemoryManager: failed to delete save file: " + e.Message);
            }
            memories.Clear();
            Debug.Log("MemoryManager: cleared in-memory memories (resetOnStart=true).");
        }

        // subscribe to GameState events if present (optional)
        try {
            GameState.OnGlobalEvent += HandleGlobalEvent;
        } catch { /* ignore if GameState missing */ }
    }


    void OnDestroy() {
        try { GameState.OnGlobalEvent -= HandleGlobalEvent; } catch { }
    }

    void Update() {
        if (!autosave) return;
        saveTimer += Time.deltaTime;
        if (saveTimer >= autosaveInterval) { SaveAll(); saveTimer = 0f; }
    }

    #region Persistence
    [Serializable]
    private struct NpcMemoryList { public List<NPCMemory> items; }

    public void LoadAll() {
        try {
            if (!File.Exists(SavePath)) return;
            string json = File.ReadAllText(SavePath, Encoding.UTF8);
            var list = JsonUtility.FromJson<NpcMemoryList>(json);
            if (list.items != null) memories = list.items.ToDictionary(m => m.npcId, m => m);
            Debug.Log($"MemoryManager: loaded {memories.Count} memories from {SavePath}");
        } catch (Exception e) {
            Debug.LogWarning("MemoryManager.LoadAll failed: " + e.Message);
        }
    }

    public void SaveAll() {
        try {
            var list = new NpcMemoryList { items = new List<NPCMemory>(memories.Values) };
            string json = JsonUtility.ToJson(list, true);
            File.WriteAllText(SavePath, json, Encoding.UTF8);
            // Debug.Log("MemoryManager: saved " + memories.Count);
        } catch (Exception e) {
            Debug.LogWarning("MemoryManager.SaveAll failed: " + e.Message);
        }
    }
    #endregion

    #region Accessors
    public NPCMemory GetOrCreate(string npcId, string persona = "", string role = "")
    {
        if (string.IsNullOrEmpty(npcId)) npcId = Guid.NewGuid().ToString();
        if (memories.TryGetValue(npcId, out var mem))
        {
            if (!string.IsNullOrEmpty(persona) && string.IsNullOrEmpty(mem.personaLine))
                mem.personaLine = persona;
            if (!string.IsNullOrEmpty(role) && string.IsNullOrEmpty(mem.roleLine))
                mem.roleLine = role;
            return mem;
        }
        var nm = new NPCMemory(npcId, persona, role);
        memories[npcId] = nm;
        return nm;
    }

    public NPCMemory GetMemory(string npcId)
    {
        memories.TryGetValue(npcId, out var mem);
        return mem;
    }

    public bool TryGet(string npcId, out NPCMemory mem) {
        return memories.TryGetValue(npcId, out mem);
    }

    public List<string> GetAllNpcIds() {
        return new List<string>(memories.Keys);
    }

    public List<NPCMemory> GetAllMemories() {
        return new List<NPCMemory>(memories.Values);
    }
    #endregion

    #region Mutators
    public void AddFact(string npcId, MemoryFact fact) {
        if (fact == null) return;
        var mem = GetOrCreate(npcId);
        // deduplicate by exact key
        var existing = mem.knownFacts.FirstOrDefault(f => f.key == fact.key);
        if (existing != null) {
            existing.value = fact.value;
            existing.ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            existing.importance = Mathf.Max(existing.importance, fact.importance);
        } else {
            mem.knownFacts.Insert(0, fact); // newest first
        }

        if (mem.knownFacts.Count > maxFactsPerNpc) PruneFacts(mem);

        mem.lastInteractionTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        UpdateShortSummary(mem);
    }

    public void AddConvoLine(string npcId, string line) {
        if (string.IsNullOrEmpty(line)) return;
        var mem = GetOrCreate(npcId);
        mem.convoLog.Insert(0, $"{DateTime.UtcNow:HH:mm} {line}");
        if (mem.convoLog.Count > maxConvoLog) mem.convoLog.RemoveRange(maxConvoLog, mem.convoLog.Count - maxConvoLog);
        mem.lastInteractionTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public void AdjustRelationship(string npcId, int delta) {
        var mem = GetOrCreate(npcId);
        mem.relationship = Mathf.Clamp(mem.relationship + delta, -5, 5);
        UpdateShortSummary(mem);
    }

    public void ResetAllInteractionCounters()
    {
        foreach (var kvp in memories)
        {
            if (kvp.Value != null)
                kvp.Value.consecutiveInteractions = 0;
        }

        Debug.Log("[MemoryManager] All NPC interaction counters reset.");
    }
    public void SetRoleState(string npcId, string role) {
        var mem = GetOrCreate(npcId);
        mem.roleState = role ?? mem.roleState;
        UpdateShortSummary(mem);
    }

    public void SetGoal(string npcId, string goal) {
        var mem = GetOrCreate(npcId);
        mem.goal = string.IsNullOrEmpty(goal) ? mem.goal : (goal.Length > 120 ? goal.Substring(0, 120) : goal);
        UpdateShortSummary(mem);
    }

    public void RemoveNpc(string npcId) {
        if (string.IsNullOrEmpty(npcId)) return;
        if (memories.ContainsKey(npcId)) memories.Remove(npcId);
    }
    #endregion

    #region Utilities
    void PruneFacts(NPCMemory mem) {
        mem.knownFacts = mem.knownFacts
            .OrderByDescending(f => f.importance)
            .ThenByDescending(f => f.ts)
            .Take(maxFactsPerNpc)
            .ToList();
    }

    void UpdateShortSummary(NPCMemory mem) {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(mem.personaLine)) parts.Add(Truncate(mem.personaLine, 40));
        parts.Add($"rel:{mem.relationship:+#;-#;0}");
        if (!string.IsNullOrEmpty(mem.roleState)) parts.Add(Truncate(mem.roleState, 16));
        if (!string.IsNullOrEmpty(mem.goal)) parts.Add($"goal:{Truncate(mem.goal, 30)}");
        if (mem.knownFacts.Count > 0) parts.Add(Truncate(mem.knownFacts[0].value, 40));
        string summary = string.Join("; ", parts);
        if (summary.Length > maxSummaryLength) summary = summary.Substring(0, maxSummaryLength);
        mem.shortSummary = summary;
    }

    string Truncate(string s, int max) {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Length <= max ? s : s.Substring(0, max - 3) + "...";
    }

    public string GetPromptSnippet(string npcId) {
        if (!memories.TryGetValue(npcId, out var mem)) return "";
        var sb = new StringBuilder();
        sb.AppendLine($"NPC:{mem.npcId} | {Truncate(mem.personaLine, 80)}");
        sb.AppendLine($"Relationship:{mem.relationship} Role:{Truncate(mem.roleState, 20)} Goal:{Truncate(mem.goal,60)}");
        if (mem.knownFacts.Count > 0) sb.AppendLine($"TopFact: {Truncate(mem.knownFacts[0].value,120)}");
        sb.AppendLine($"Summary:{Truncate(mem.shortSummary, maxSummaryLength)}");
        return sb.ToString();
    }
    #endregion

    #region Simple MVP reply builder
    /// <summary>
    /// Minimal deterministic reply builder for MVP. Use in NPC.Interact() for instant replies.
    /// </summary>
    public string BuildReplyFromMemory(string npcId, string playerAction) {
        var mem = GetOrCreate(npcId);
        // priority facts
        if (mem.knownFacts.Any(f => f.key == "saw_bandit")) {
            return "Stay by the fire. I saw someone in the trees.";
        }
        if (mem.knownFacts.Any(f => f.key == "gave_axle_pin")) {
            return "Thanks for the pin. We'll make do.";
        }
        if (!string.IsNullOrEmpty(playerAction) && playerAction.ToLower().Contains("repair")) {
            if (mem.relationship >= 1) return "Good work. We're one step closer.";
            else return "You're fixing it? Well, do it proper.";
        }
        if (mem.relationship <= -3) return "I don't trust you.";
        if (!string.IsNullOrEmpty(mem.personaLine)) return Truncate(mem.personaLine, 80);
        return "They nod.";
    }
    #endregion

    #region Broadcast handler (optional)
    void HandleGlobalEvent(string key, string val) {
        // this is purposely simple: small facts for MVP
        if (key == "repair_progress") {
            foreach (var id in GetAllNpcIds()) AddFact(id, new MemoryFact("helped_repair", $"repair_stage:{val}", 3));
        } else if (key == "weather") {
            foreach (var id in GetAllNpcIds()) AddFact(id, new MemoryFact("weather", val, 1));
        }
    }
    #endregion
}
