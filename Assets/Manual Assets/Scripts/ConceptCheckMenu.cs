using UnityEngine;
using TMPro;

public class ConceptCheckMenu : MonoBehaviour
{
    [Header("Main Menu UI")]
    public TextMeshProUGUI mainTitleText;
    
    [Header("Panel Controllers")]
    public GameObject sizeOMeterPanel; 
    public GameObject bottomSlidersPanel; // <-- NEW SLOT

    void OnEnable()
    {
        string activeShape = ButtonScript.selectedShape;
        if (mainTitleText != null && !string.IsNullOrEmpty(activeShape))
        {
            mainTitleText.text = activeShape + " Concept Check";
        }
    }

    // Call this from your Orange Back Button!
    public void CloseMenuAndReset()
    {
        ARController ar = FindFirstObjectByType<ARController>();
        if (ar != null) ar.ForceShapeScale(Vector3.one);
        
        if (sizeOMeterPanel != null) sizeOMeterPanel.SetActive(true);
        
        // Turn the bottom sliders back on!
        if (bottomSlidersPanel != null) bottomSlidersPanel.SetActive(true); 

        gameObject.SetActive(false);
    }
}