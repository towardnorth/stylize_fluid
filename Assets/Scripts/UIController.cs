using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public FluidMoodController fluidMoodController;
    public TMP_InputField moodInputfield;
    public Button submitButton;
    // Start is called before the first frame update
    void Start()
    {
        submitButton.onClick.AddListener(OnSubmitMood);
    }

    // Update is called once per frame
    private void OnSubmitMood()
    {
        string mood =moodInputfield.text;
        fluidMoodController.RequestFluidParams(mood);
    }
}
