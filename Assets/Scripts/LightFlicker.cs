using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightFlicker : MonoBehaviour
{
    [SerializeField] Light2D light2D;
    [SerializeField] float flickerSpeed = 0.1f;
    [SerializeField] float intensityMin = 1.5f;
    [SerializeField] float intensityMax = 2.2f;


    void Update()
    {
        if (light2D == null) return;
        float duration = Mathf.Max(0.0001f, flickerSpeed);
        float t = Mathf.PingPong(Time.time, duration) / duration; // 0->1 in 'duration' seconds, then back
        light2D.intensity = Mathf.Lerp(intensityMin, intensityMax, t);
    }
}
