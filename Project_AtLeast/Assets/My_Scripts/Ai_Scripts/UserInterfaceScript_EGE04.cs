using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UserInterfaceScript_EGE04 : MonoBehaviour
{
    public TextMeshProUGUI eventText;
    public ParticleSystem particles;



    public void OnPlayButtonWasClicked()
    {
        eventText.text = "Play button was clicked!";
    }

    public void OnExitButtonWasClicked()
    {
        eventText.text = "Exit button was clicked!";
    }

    public void OnSliderValueChanged(Single receivedVal)
    {
        eventText.text = "Slider Value changed to " + receivedVal;
    }

    public void OnInputFieldChanged(string receivedString)
    {
        eventText.text = "Input field changed to " + receivedString;
    }

    public void ConfettiFunction()
    {
        particles.Play();
    }


}
