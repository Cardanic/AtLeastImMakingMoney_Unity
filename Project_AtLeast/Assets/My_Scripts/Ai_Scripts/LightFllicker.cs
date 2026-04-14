using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightFllicker : MonoBehaviour
{

    private Light lightOnObject;

    private float targetLightIntensity = 1f;

    private void Start()
    {
        lightOnObject = GetComponent<Light>();
        InvokeRepeating("ChangeTargetLightIntensity", 0.3f, 0.3f);
    }

    private void ChangeTargetLightIntensity()
    {
        targetLightIntensity = Random.Range(0.8f, 1.2f);
    }

    void Update()
    {
        lightOnObject.intensity = Mathf.Lerp(lightOnObject.intensity, targetLightIntensity, Time.deltaTime * 10f);
    }
}
