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
        light2D.intensity = Mathf.Lerp(light2D.intensity,
            Random.Range(intensityMin, intensityMax),
            flickerSpeed);
    }
}
