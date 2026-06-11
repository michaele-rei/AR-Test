using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; 

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

    [Header("Exercise Mode Clean-Up")]
    public GameObject[] itemsToHideInExerciseMode;

    [Header("Color Drawer System")]
    public GameObject colorFlyoutMenu;  
    private RectTransform colorMenuRect;
    private Vector2 colorMenuOpenPos;

    [Header("Exercise Grading UI")]
    public TMPro.TMP_InputField answerInputField;
    public TMPro.TextMeshProUGUI feedbackText;

    public string currentUnit = "cm";
    private bool isUIVisible = true;
    private bool isSystemUpdating = false;
    private bool isSolidPlaced = false; 

    private RectTransform unitMenuRect;
    private Vector2 unitMenuOpenPos;

    void Start()
    {
        row1Slider.wholeNumbers = false;
        row2Slider.wholeNumbers = false;
        row3Slider.wholeNumbers = false;

        row1Slider.onValueChanged.AddListener(delegate { OnSliderMoved(); });
        row2Slider.onValueChanged.AddListener(delegate { OnSliderMoved(); });
        row3Slider.onValueChanged.AddListener(delegate { OnSliderMoved(); });

        if (row1Input != null) row1Input.onEndEdit.AddListener(delegate { OnInputEdited(1); });
        if (row2Input != null) row2Input.onEndEdit.AddListener(delegate { OnInputEdited(2); });
        if (row3Input != null) row3Input.onEndEdit.AddListener(delegate { OnInputEdited(3); });

        if (unitFlyoutMenu != null) 
        {
            unitMenuRect = unitFlyoutMenu.GetComponent<RectTransform>();
            unitMenuOpenPos = unitMenuRect.anchoredPosition;
            unitFlyoutMenu.SetActive(false);
        }

        ResetUI();

        if (ExerciseManager.isExerciseMode)
        {
            foreach (GameObject item in itemsToHideInExerciseMode)
            {
                if (item != null) item.SetActive(false);
            }
        }

        if (colorFlyoutMenu != null) 
        {
            colorMenuRect = colorFlyoutMenu.GetComponent<RectTransform>();
            colorMenuOpenPos = colorMenuRect.anchoredPosition;
            colorFlyoutMenu.SetActive(false);
        }
    }

    public void SetupUIForShape(string shapeName)
    {
        isSolidPlaced = true; 
        isUIVisible = true;
        
        if (hideableUIGroup != null) 
        {
            hideableUIGroup.SetActive(true);
            CanvasGroup cg = GetOrAddCanvasGroup(hideableUIGroup);
            cg.alpha = 1f;
            hideableUIGroup.transform.localScale = Vector3.one;
        }

        isSystemUpdating = true; 

        // Hide all rows initially to clear out layout from previous shapes
        row1.SetActive(false);
        row2.SetActive(false); 
        row3.SetActive(false);
        if (dashRow2Container != null) dashRow2Container.SetActive(false); 
        if (dashRow3Container != null) dashRow3Container.SetActive(false);

        // --- UPDATED: New Shape Configuration Logic ---
        if (shapeName == "Sphere") 
        { 
            row1Label.text = "Radius:"; 
            row1.SetActive(true); 
        }
        else if (shapeName == "Cylinder" || shapeName == "Cone")
        {
            row1Label.text = "Radius:"; 
            row2Label.text = "Height:";
            row1.SetActive(true); 
            row2.SetActive(true);
            if (dashRow2Container != null) dashRow2Container.SetActive(true);
        }
        else if (shapeName == "Pyramid")
        {
            row1Label.text = "Base:"; 
            row2Label.text = "Height:";
            row1.SetActive(true); 
            row2.SetActive(true);
            if (dashRow2Container != null) dashRow2Container.SetActive(true);
        }
        else if (shapeName == "Cuboid")
        {
            // Cuboid requires all 3 tracking sliders
            row1Label.text = "Length:"; 
            row2Label.text = "Height:"; 
            row3Label.text = "Width:";
            row1.SetActive(true); 
            row2.SetActive(true); 
            row3.SetActive(true);
            if (dashRow2Container != null) dashRow2Container.SetActive(true); 
            if (dashRow3Container != null) dashRow3Container.SetActive(true);
        }
        else if (shapeName == "TriangularPrism")
        {
            // Triangular Prism requires all 3 tracking sliders
            row1Label.text = "Base:"; 
            row2Label.text = "Height:"; 
            row3Label.text = "Length:";
            row1.SetActive(true); 
            row2.SetActive(true); 
            row3.SetActive(true);
            if (dashRow2Container != null) dashRow2Container.SetActive(true); 
            if (dashRow3Container != null) dashRow3Container.SetActive(true);
        }

        SetAbsoluteSliderBoundsAndValue(0.5f);
        OnSliderMoved(); 
        isSystemUpdating = false; 
    }

    public void OnSliderMoved()
    {
        if (!row1Input.isFocused) row1Input.text = row1Slider.value.ToString("F2"); 
        if (!row2Input.isFocused) row2Input.text = row2Slider.value.ToString("F2");
        if (!row3Input.isFocused) row3Input.text = row3Slider.value.ToString("F2");

        if (row1.activeSelf) dashRow1Txt.text = row1Label.text.Replace(":", "") + ": " + row1Slider.value.ToString("F2") + " " + currentUnit;
        if (row2.activeSelf) dashRow2Txt.text = row2Label.text.Replace(":", "") + ": " + row2Slider.value.ToString("F2") + " " + currentUnit;
        if (row3.activeSelf) dashRow3Txt.text = row3Label.text.Replace(":", "") + ": " + row3Slider.value.ToString("F2") + " " + currentUnit;

        if (arController != null)
        {
            float mult = GetMultiplierToMeters(currentUnit);
            arController.UpdateDimensionsFromUI(row1Slider.value * mult, row2Slider.value * mult, row3Slider.value * mult, !isSystemUpdating);
        }
    }

    public void OnInputEdited(int rowNum)
    {
        float newValue;
        if (rowNum == 1 && float.TryParse(row1Input.text, out newValue)) 
        {
            newValue = Mathf.Clamp(newValue, row1Slider.minValue, row1Slider.maxValue);
            row1Slider.value = newValue;
            row1Input.text = newValue.ToString("F2"); 
        }
        else if (rowNum == 2 && float.TryParse(row2Input.text, out newValue)) 
        {
            newValue = Mathf.Clamp(newValue, row2Slider.minValue, row2Slider.maxValue);
            row2Slider.value = newValue;
            row2Input.text = newValue.ToString("F2");
        }
        else if (rowNum == 3 && float.TryParse(row3Input.text, out newValue)) 
        {
            newValue = Mathf.Clamp(newValue, row3Slider.minValue, row3Slider.maxValue);
            row3Slider.value = newValue;
            row3Input.text = newValue.ToString("F2");
        }
    }

    public void UpdateSlidersSilently(float valX_Meters, float valY_Meters, float valZ_Meters)
    {
        isSystemUpdating = true;
        float mult = GetMultiplierToMeters(currentUnit);
        row1Slider.value = valX_Meters / mult;
        row2Slider.value = valY_Meters / mult;
        row3Slider.value = valZ_Meters / mult;
        OnSliderMoved();
        isSystemUpdating = false;
    }

    public void UpdateDashboardVolume(float volumeInMeters)
    {
        float mult = GetMultiplierToMeters(currentUnit);
        float displayVol = volumeInMeters / Mathf.Pow(mult, 3);
        dashVolumeTxt.text = "Volume: " + displayVol.ToString("F2") + " " + currentUnit + "³";
    }

    public void ToggleEyeView()
    {
        if (!isSolidPlaced || hideableUIGroup == null) return; 

        isUIVisible = !isUIVisible;
        CanvasGroup cg = GetOrAddCanvasGroup(hideableUIGroup);

        DOTween.Kill(hideableUIGroup.transform);
        DOTween.Kill(cg);

        if (isUIVisible)
        {
            hideableUIGroup.SetActive(true);
            cg.alpha = 0f;
            hideableUIGroup.transform.localScale = Vector3.one * 0.9f;

            cg.DOFade(1f, 0.3f);
            hideableUIGroup.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }
        else
        {
            cg.DOFade(0f, 0.2f);
            hideableUIGroup.transform.DOScale(0.9f, 0.2f).SetEase(Ease.InBack).OnComplete(() => 
            {
                hideableUIGroup.SetActive(false);
            });

            if (unitFlyoutMenu != null && unitFlyoutMenu.activeSelf) 
            {
                ToggleUnitMenu(); 
            }
        }
    }

    public void ToggleUnitMenu() 
    { 
        if (!isSolidPlaced || unitFlyoutMenu == null) return; 

        bool isOpening = !unitFlyoutMenu.activeSelf;
        CanvasGroup cg = GetOrAddCanvasGroup(unitFlyoutMenu);

        DOTween.Kill(unitMenuRect);
        DOTween.Kill(cg);

        float slideDistanceX = 150f; 

        if (isOpening)
        {
            unitFlyoutMenu.SetActive(true);
            unitMenuRect.anchoredPosition = new Vector2(unitMenuOpenPos.x + slideDistanceX, unitMenuOpenPos.y);
            cg.alpha = 0f;

            unitMenuRect.DOAnchorPos(unitMenuOpenPos, 0.3f).SetEase(Ease.OutBack);
            cg.DOFade(1f, 0.3f);

            if (colorFlyoutMenu != null && colorFlyoutMenu.activeSelf) 
            {
                ToggleColorMenu(); 
            }
        }
        else
        {
            Vector2 closedPos = new Vector2(unitMenuOpenPos.x + slideDistanceX, unitMenuOpenPos.y);
            
            unitMenuRect.DOAnchorPos(closedPos, 0.2f).SetEase(Ease.InBack);
            cg.DOFade(0f, 0.2f).OnComplete(() => 
            {
                unitFlyoutMenu.SetActive(false);
            });
        }
    }

    public void ToggleColorMenu() 
    { 
        if (!isSolidPlaced || colorFlyoutMenu == null) return; 

        bool isOpening = !colorFlyoutMenu.activeSelf;
        CanvasGroup cg = GetOrAddCanvasGroup(colorFlyoutMenu);

        DOTween.Kill(colorMenuRect);
        DOTween.Kill(cg);

        float slideDistanceX = 150f; // Same distance as the unit drawer

        if (isOpening)
        {
            // NEW: If the unit menu is open, close it so they don't overlap!
            if (unitFlyoutMenu != null && unitFlyoutMenu.activeSelf) 
            {
                ToggleUnitMenu(); 
            }

            colorFlyoutMenu.SetActive(true);
            colorMenuRect.anchoredPosition = new Vector2(colorMenuOpenPos.x + slideDistanceX, colorMenuOpenPos.y);
            cg.alpha = 0f;

            colorMenuRect.DOAnchorPos(colorMenuOpenPos, 0.3f).SetEase(Ease.OutBack);
            cg.DOFade(1f, 0.3f);
        }
        else
        {
            Vector2 closedPos = new Vector2(colorMenuOpenPos.x + slideDistanceX, colorMenuOpenPos.y);
            
            colorMenuRect.DOAnchorPos(closedPos, 0.2f).SetEase(Ease.InBack);
            cg.DOFade(0f, 0.2f).OnComplete(() => 
            {
                colorFlyoutMenu.SetActive(false);
            });
        }
    }

    public void SetUnit(string newUnit)
    {
        if (newUnit == currentUnit) return; 

        float currentMult = GetMultiplierToMeters(currentUnit);
        float currentSizeMetersX = row1Slider.value * currentMult;
        float currentSizeMetersY = row2Slider.value * currentMult;
        float currentSizeMetersZ = row3Slider.value * currentMult;

        isSystemUpdating = true; 
        currentUnit = newUnit;

        row1Slider.minValue = -9999f; row1Slider.maxValue = 9999f;
        row2Slider.minValue = -9999f; row2Slider.maxValue = 9999f;
        row3Slider.minValue = -9999f; row3Slider.maxValue = 9999f;

        float newMult = GetMultiplierToMeters(currentUnit);
        row1Slider.value = currentSizeMetersX / newMult;
        row2Slider.value = currentSizeMetersY / newMult;
        row3Slider.value = currentSizeMetersZ / newMult;

        float absoluteMax = 3.0f / newMult;
        float absoluteMin = 0.01f / newMult;

        row1Slider.minValue = absoluteMin; row1Slider.maxValue = absoluteMax;
        row2Slider.minValue = absoluteMin; row2Slider.maxValue = absoluteMax;
        row3Slider.minValue = absoluteMin; row3Slider.maxValue = absoluteMax;

        ToggleUnitMenu(); 
        isSystemUpdating = false;
        OnSliderMoved(); 
        AnimateUnitTexts();
    }

    private void AnimateUnitTexts()
    {
        float punchAmount = 0.2f; 
        float duration = 0.3f;   

        dashRow1Txt.transform.DOKill(true);
        dashRow1Txt.transform.DOPunchScale(Vector3.one * punchAmount, duration, 5, 1);
        
        dashVolumeTxt.transform.DOKill(true);
        dashVolumeTxt.transform.DOPunchScale(Vector3.one * punchAmount, duration, 5, 1);

        if (row2.activeSelf)
        {
            dashRow2Txt.transform.DOKill(true);
            dashRow2Txt.transform.DOPunchScale(Vector3.one * punchAmount, duration, 5, 1);
        }
        if (row3.activeSelf)
        {
            dashRow3Txt.transform.DOKill(true);
            dashRow3Txt.transform.DOPunchScale(Vector3.one * punchAmount, duration, 5, 1);
        }

        if (row1.activeSelf && row1Input != null) 
        {
            row1Input.transform.DOKill(true);
            row1Input.transform.DOPunchScale(Vector3.one * punchAmount, duration, 5, 1);
        }
        if (row2.activeSelf && row2Input != null) 
        {
            row2Input.transform.DOKill(true);
            row2Input.transform.DOPunchScale(Vector3.one * punchAmount, duration, 5, 1);
        }
        if (row3.activeSelf && row3Input != null) 
        {
            row3Input.transform.DOKill(true);
            row3Input.transform.DOPunchScale(Vector3.one * punchAmount, duration, 5, 1);
        }
        if (arController != null)
        {
            arController.Animate3DLabels();
        }
    }

    private void SetAbsoluteSliderBoundsAndValue(float defaultSizeMeters)
    {
        float mult = GetMultiplierToMeters(currentUnit);
        
        row1Slider.minValue = -9999f; row1Slider.maxValue = 9999f;
        row2Slider.minValue = -9999f; row2Slider.maxValue = 9999f;
        row3Slider.minValue = -9999f; row3Slider.maxValue = 9999f;

        row1Slider.value = defaultSizeMeters / mult;
        row2Slider.value = defaultSizeMeters / mult;
        row3Slider.value = defaultSizeMeters / mult;

        float absoluteMax = 3.0f / mult;
        float absoluteMin = 0.01f / mult;

        row1Slider.minValue = absoluteMin; row1Slider.maxValue = absoluteMax;
        row2Slider.minValue = absoluteMin; row2Slider.maxValue = absoluteMax;
        row3Slider.minValue = absoluteMin; row3Slider.maxValue = absoluteMax;
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
        isSolidPlaced = false; 
        isUIVisible = false;
        if (hideableUIGroup != null) hideableUIGroup.SetActive(false);
        if (unitFlyoutMenu != null) unitFlyoutMenu.SetActive(false);
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();
        return cg;
    }
}