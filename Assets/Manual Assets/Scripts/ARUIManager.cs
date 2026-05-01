using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ARUIManager : MonoBehaviour
{
    [Header("AR Controller Link")]
    public ARController arController; 

    [Header("UI Groups")]
    public GameObject hideableUIGroup; 
    public GameObject unitFlyoutMenu;  

    [Header("Left Dashboard Containers")]
    public GameObject dashRow1Container; 
    public GameObject dashRow2Container;
    public GameObject dashRow3Container;

    [Header("Left Dashboard Texts")]
    public TextMeshProUGUI dashRow1Txt; 
    public TextMeshProUGUI dashRow2Txt;
    public TextMeshProUGUI dashRow3Txt;
    public TextMeshProUGUI dashVolumeTxt;

    [Header("Bottom Panel Rows")]
    public GameObject row1;
    public GameObject row2;
    public GameObject row3;

    [Header("Row 1: Main Dimension")]
    public TextMeshProUGUI row1Label;
    public Slider row1Slider;
    public TMP_InputField row1Input;

    [Header("Row 2: Secondary Dimension")]
    public TextMeshProUGUI row2Label;
    public Slider row2Slider;
    public TMP_InputField row2Input;

    [Header("Row 3: Tertiary Dimension")]
    public TextMeshProUGUI row3Label;
    public Slider row3Slider;
    public TMP_InputField row3Input;

    public string currentUnit = "cm";
    private bool isUIVisible = true;
    private bool isSystemUpdating = false;

    void Start()
    {
        // ISSUE 1 FIX: Expand the sliders beyond 1! (Defaulting to 500cm max)
        row1Slider.maxValue = 500f;
        row2Slider.maxValue = 500f;
        row3Slider.maxValue = 500f;

        row1Slider.onValueChanged.AddListener(delegate { OnSliderMoved(); });
        row2Slider.onValueChanged.AddListener(delegate { OnSliderMoved(); });
        row3Slider.onValueChanged.AddListener(delegate { OnSliderMoved(); });

        if (row1Input != null) row1Input.onEndEdit.AddListener(delegate { OnInputEdited(1); });
        if (row2Input != null) row2Input.onEndEdit.AddListener(delegate { OnInputEdited(2); });
        if (row3Input != null) row3Input.onEndEdit.AddListener(delegate { OnInputEdited(3); });

        if (unitFlyoutMenu != null) unitFlyoutMenu.SetActive(false);
        ResetUI();
    }

    public void SetupUIForShape(string shapeName)
    {
        isUIVisible = true;
        if (hideableUIGroup != null) hideableUIGroup.SetActive(true);

        isSystemUpdating = true; 

        row2.SetActive(false); 
        row3.SetActive(false);
        if (dashRow2Container != null) dashRow2Container.SetActive(false); 
        if (dashRow3Container != null) dashRow3Container.SetActive(false);

        if (shapeName == "Cube") { row1Label.text = "Side:"; row1.SetActive(true); }
        else if (shapeName == "Sphere") { row1Label.text = "Radius:"; row1.SetActive(true); }
        else if (shapeName == "Cylinder" || shapeName == "Cone")
        {
            row1Label.text = "Radius:"; row2Label.text = "Height:";
            row1.SetActive(true); row2.SetActive(true);
            if (dashRow2Container != null) dashRow2Container.SetActive(true);
        }
        else if (shapeName == "Pyramid")
        {
            row1Label.text = "Base:"; row2Label.text = "Height:";
            row1.SetActive(true); row2.SetActive(true);
            if (dashRow2Container != null) dashRow2Container.SetActive(true);
        }
        else if (shapeName == "RectangularPrism")
        {
            row1Label.text = "Length:"; row2Label.text = "Height:"; row3Label.text = "Width:";
            row1.SetActive(true); row2.SetActive(true); row3.SetActive(true);
            if (dashRow2Container != null) dashRow2Container.SetActive(true); 
            if (dashRow3Container != null) dashRow3Container.SetActive(true);
        }

        OnSliderMoved(); 
        isSystemUpdating = false; 
    }

    public void OnSliderMoved()
    {
        if (!row1Input.isFocused) row1Input.text = row1Slider.value.ToString("F1");
        if (!row2Input.isFocused) row2Input.text = row2Slider.value.ToString("F1");
        if (!row3Input.isFocused) row3Input.text = row3Slider.value.ToString("F1");

        dashRow1Txt.text = row1Label.text.Replace(":", "") + ": " + row1Slider.value.ToString("F1") + " " + currentUnit;
        if (row2.activeSelf) dashRow2Txt.text = row2Label.text.Replace(":", "") + ": " + row2Slider.value.ToString("F1") + " " + currentUnit;
        if (row3.activeSelf) dashRow3Txt.text = row3Label.text.Replace(":", "") + ": " + row3Slider.value.ToString("F1") + " " + currentUnit;

        if (arController != null)
        {
            // ISSUE 2 FIX: Convert the UI values to precise real-world meters for the AR engine
            float mult = GetMultiplierToMeters(currentUnit);
            arController.UpdateDimensionsFromUI(row1Slider.value * mult, row2Slider.value * mult, row3Slider.value * mult, !isSystemUpdating);
        }
    }

    public void OnInputEdited(int rowNum)
    {
        float newValue;
        if (rowNum == 1 && float.TryParse(row1Input.text, out newValue)) row1Slider.value = newValue;
        else if (rowNum == 2 && float.TryParse(row2Input.text, out newValue)) row2Slider.value = newValue;
        else if (rowNum == 3 && float.TryParse(row3Input.text, out newValue)) row3Slider.value = newValue;
    }

    public void UpdateSlidersSilently(float valX_Meters, float valY_Meters, float valZ_Meters)
    {
        isSystemUpdating = true;
        // Convert the AR engine's meters back into the user's selected UI unit
        float mult = GetMultiplierToMeters(currentUnit);
        row1Slider.value = valX_Meters / mult;
        row2Slider.value = valY_Meters / mult;
        row3Slider.value = valZ_Meters / mult;
        OnSliderMoved();
        isSystemUpdating = false;
    }

    public void UpdateDashboardVolume(float volumeInMeters)
    {
        // Convert cubic meters back to the UI unit (cm³, in³)
        float mult = GetMultiplierToMeters(currentUnit);
        float displayVol = volumeInMeters / Mathf.Pow(mult, 3);
        dashVolumeTxt.text = "Volume: " + displayVol.ToString("F2") + " " + currentUnit + "³";
    }

    public void ToggleEyeView()
    {
        isUIVisible = !isUIVisible;
        if (hideableUIGroup != null) hideableUIGroup.SetActive(isUIVisible);
        if (unitFlyoutMenu != null) unitFlyoutMenu.SetActive(false); 
    }

    public void ToggleUnitMenu() { if (unitFlyoutMenu != null) unitFlyoutMenu.SetActive(!unitFlyoutMenu.activeSelf); }

    // --- ISSUE 2 FIX: THE REALISTIC UNIT ENGINE ---
    public void SetUnit(string newUnit)
    {
        if (newUnit == currentUnit) return; 

        float oldMult = GetMultiplierToMeters(currentUnit);
        float newMult = GetMultiplierToMeters(newUnit);
        float conversion = oldMult / newMult;

        isSystemUpdating = true; // Lock AR updates while we mathematically shift the sliders

        // Convert the max boundaries (e.g., 500 cm becomes 5 m)
        row1Slider.maxValue *= conversion;
        row2Slider.maxValue *= conversion;
        row3Slider.maxValue *= conversion;

        // Convert the actual slider positions
        row1Slider.value *= conversion;
        row2Slider.value *= conversion;
        row3Slider.value *= conversion;

        currentUnit = newUnit;
        ToggleUnitMenu(); 
        isSystemUpdating = false;
        OnSliderMoved(); // Push new accurate numbers
    }

    public float GetMultiplierToMeters(string unit)
    {
        if (unit == "cm") return 0.01f;
        if (unit == "m") return 1.0f;
        if (unit == "in") return 0.0254f;
        return 1.0f;
    }

    public void ResetUI()
    {
        isUIVisible = false;
        if (hideableUIGroup != null) hideableUIGroup.SetActive(false);
        if (unitFlyoutMenu != null) unitFlyoutMenu.SetActive(false);
    }
}