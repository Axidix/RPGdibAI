using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea(1, 3)]
    public string text;
    public float duration = 3f;
}

[System.Serializable]
public class Dialogue
{
    public DialogueLine[] lines;
}
