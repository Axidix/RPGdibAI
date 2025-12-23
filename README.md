# Context-Aware NPC Dialogue System Powered by LLMs

This project demonstrates a complete pipeline for generating adaptive, context-sensitive NPC dialogue inside a Unity 2D game, using lightweight LLMs. The system combines gameplay events, persistent NPC memory, personality profiles, and dynamic prompt construction to produce dialogues that change meaningfully across playthroughs.

The goal is to turn simple NPCs into reactive characters influenced by world events, prior interactions, and player-defined personalities, so as to fully immerse the player in the fictive world.

### Gameplay GIF  
![Gif demo](demo/gif_demo.gif)

The GIF illustrates one of the system’s core features: NPC mood evolution based on repeated interactions, which directly affects the generated dialogue.

### Game story

A carriage and its escort are trapped in a dark, gloomy forest. One wheel is broken and need urgent repair for them to achieve their journey. The player is a mercenary, hired by a merchant along another mercenary. The goal is to find how to repair the carriage to complete your mission.  
Beware of those who lurks in the shadow...

---

## Demo

<video width="640" height="360" controls>
    <source src="demo/demo_rpgai.mp4" type="video/mp4">
    Your browser does not support the video tag.
</video>

If this is not working, see at https://axidix.github.io/RPGdibAI/

---

## System Overview

Below is the technicak diagram summarizing how gameplay logic, memory, and AI inference communicate to produce adaptive interactions.

![System Diagram](demo/diagram_rpgai.png)

---

## Core Features

### NPC Memory System

Each NPC maintains a structured memory including:

- Personality (defined by the player at the beginning of the game)
- NPC role in the scenario
- World events encountered (carriage repaired, bandit defeated, axle pin found)
- Interaction counters (annoyance level)
- Relationship indicators
- Short auto-summarized snippets used during prompt construction

Memory allows NPCs to react based on the unfolding narrative and previous interactions with the player instead of generating isolated lines.

---

### Runtime LLM Dialogue

Dialogue is dynamically generated at runtime:

1. The current NPC is queried with their memory, personality, role, and mood.
2. A prompt tailored to the exact situation is constructed.
3. The system calls a lightweight LLM through HuggingFace Inference.
4. The trimmed result is displayed in the Unity UI.
5. The NPC’s memory is updated based on what was said.

The output is fully contextual: the same NPC speaks differently depending on personality, the world, and the player's actions.

---

### Player-Defined Personalities

At the start of the game, the player chooses a personality line for each NPC.  
This creates strong variation across playthroughs. The sets used in the demo were:

Set A:

- Warm and talkative merchant
- Clumsy bandit with a guilty conscience
- Cheerful mercenary who softens quickly

Set B:

- Bitter, suspicious merchant
- Cruel bandit who enjoys intimidation
- Cold mercenary who only respects strength

These personalities directly influence every generated line.

---

### Unity Gameplay Integration

The dialogue system is embedded into actual game events:

- Encountering the bandit
- Retrieving the axle pin
- Repairing the carriage
- Talking to party members
- Repeated interactions influencing annoyance

Game events modify the world state, which then modifies the prompt and, consequently, the generated dialogue.

---

## Technical Breakdown

### Game Logic Manager

Handles core gameplay events such as the bandit encounter, the repair station, and world-state updates. It emits narrative signals that the Memory Manager consumes.

### Memory Manager

Manages persistent memory for each NPC:

- Retrieves stored information
- Updates memory after events
- Produces short snippets used by the prompt builder

This allows for evolving NPC behavior across the demo.

### Dynamic Prompt Builder

Builds the context provided to the LLM:

- World state
- Personality
- Role
- Memory snippet
- Last player action
- Mood and repeated-interaction data
- High-level system instructions

The output is a prompt specific to the exact situation, ensuring context-aware dialogue.

### LLM Client

Sends API requests to HuggingFace Inference, receives generated text, trims unnecessary tokens, and forwards the result to the dialogue system.

### NPC Dialogue System

Displays dialogue in the UI and updates the NPC’s conversation log for future interactions.

---

## Future Work

### Emotion System Expansion

Introduce more nuanced emotional states (fear, trust, excitement, suspicion) with temporal smoothing and mood decay.

### Long-Term Memory Compression

Replace long conversation logs with:

- Periodic summarization
- Topic clustering
- Retrieval-augmented memory (RAG)

This will reduce prompt size and improve long-term coherence.

### Inter-NPC Conversations

Allow NPCs to react to each other’s dialogue, not only the player’s interactions.  
Enrich the world by producing NPC interactions without player action (e.g NPCs starting conversation randomly between them, calling the player to make a request)

### Local LLM Execution

Optional support for:

- GGUF models
- On-device inference
- Unity Barracuda integration

This would remove API dependence. However, performance would have to be studied.

### Spoken dialogues

Leverage AI tools to produce voice adapted to the NPC's personality and lines, to deepen player immersion.
