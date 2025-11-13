using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class HFChatTest : MonoBehaviour
{
    public string apiKey = "";
    public string model = "meta-llama/Llama-3.1-8B-Instruct";

    void Start()
    {
        StartCoroutine(CallHF());
    }

    IEnumerator CallHF()
    {
        string url = "https://router.huggingface.co/v1/chat/completions";

        string jsonPayload = JsonUtility.ToJson(new ChatRequest
        {
            model = model,
            messages = new Message[]
            {
                new Message { role = "user", content = "Hello! Say a short greeting." }
            },
            max_tokens = 50
        });

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();

            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + apiKey);

            Debug.Log("Sending request...");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("HF ERROR: " + req.error);
                Debug.LogError("Raw: " + req.downloadHandler.text);
            }
            else
            {
                Debug.Log("HF RESPONSE: " + req.downloadHandler.text);
            }
        }
    }

    [System.Serializable]
    public class ChatRequest
    {
        public string model;
        public Message[] messages;
        public int max_tokens;
    }

    [System.Serializable]
    public class Message
    {
        public string role;
        public string content;
    }
}
