using System.Collections;
using UnityEngine;

public class IncreaseEmissionIntensity : MonoBehaviour
{

    [SerializeField] private Light eyeLightOne;
    [SerializeField] private Light eyeLightTwo;

    [SerializeField] private float intensityModifier;
    [SerializeField] private float maxIntensity;

    public bool active;
    public bool increase;

    private void Update()
    {
        if (active)
        {
            ChangeIntensity();
        }
    }

    private void ChangeIntensity()
    {
        if (increase)
        {
            if (eyeLightOne.intensity <= maxIntensity)
            {
                eyeLightOne.intensity += intensityModifier * Time.deltaTime;
                eyeLightTwo.intensity += intensityModifier * Time.deltaTime;
            }
        }
        else
        {
            eyeLightOne.intensity -= intensityModifier * 2 * Time.deltaTime;
            eyeLightTwo.intensity -= intensityModifier * 2 * Time.deltaTime;

            if (eyeLightOne.intensity <= 0f)
            {
                active = false;
            }
        }
    }
}
