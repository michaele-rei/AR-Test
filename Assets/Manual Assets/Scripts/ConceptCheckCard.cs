using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using System.Collections;

public class ConceptCheckCard : MonoBehaviour
{
    [Header("Which Question is this Card?")]
    [Tooltip("Set this to 1, 2, or 3 in the Inspector")]
    [Range(1, 3)] public int questionNumber = 1; 

    [Header("UI Elements")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI problemText;
    public TMP_InputField answerInput;
    public Image inputBackground;
    public Button checkButton;
    public Button viewInARButton;

    [Header("UI Elements")]
    // Add this new slot:
    public TextMeshProUGUI unitLabel;

    [Header("Panel Controllers")]
    public GameObject parentMenuPanel; 
    public GameObject sizeOMeterPanel; 
    public GameObject bottomSlidersPanel; // <-- NEW SLOT

    private ConceptCheckData myData;
    private string activeShape; 
    private Color correctColor = new Color(0.3f, 0.8f, 0.3f);
    private Color wrongColor = new Color(0.9f, 0.4f, 0.4f);

    

    void Start()
    {
        if (checkButton != null) checkButton.onClick.AddListener(GradeAnswer);
        if (viewInARButton != null) viewInARButton.onClick.AddListener(LaunchARView);
    }

    // OnEnable runs every single time this UI card becomes visible on screen!
    void OnEnable()
    {
        // Automatically grab whatever shape the user just clicked!
        activeShape = ButtonScript.selectedShape; 
        LoadDataFromDatabase();
    }

    private void LoadDataFromDatabase()
    {
        if (ExercisesDatabase.Instance == null || string.IsNullOrEmpty(activeShape)) return;

        // Reset the UI for a fresh start
        answerInput.text = "";
        answerInput.interactable = true;
        if (checkButton != null) checkButton.interactable = true;
        if (inputBackground != null) inputBackground.color = Color.white;

        // Find the picked shape in the database
        foreach (var check in ExercisesDatabase.Instance.shapeCheckpoints)
        {
            if (check.shapeType == activeShape)
            {
                myData = check;
                break;
            }
        }

        // Fill out the UI card text automatically
        if (myData != null)
        {
            if (titleText != null) titleText.text = $"Check {questionNumber}";
            
            if (problemText != null)
            {
                if (questionNumber == 1) problemText.text = myData.q1Text;
                else if (questionNumber == 2) problemText.text = myData.q2Text;
                else if (questionNumber == 3) problemText.text = myData.q3Text;
            }
            if (unitLabel != null)
            {
                // Q1 is Volume (Cubed), Q2 and Q3 are standard lengths or words
                if (questionNumber == 1) unitLabel.text = "m³";
                else if (questionNumber == 2) unitLabel.text = "m";
                else unitLabel.text = ""; // Hide the unit for the Vocab question
            }
        }
    }

    private void GradeAnswer()
    {
        if (myData == null) return;
        
        string userInput = answerInput.text.Trim().ToLower();
        bool isCorrect = false;

        // Grade Math Questions (Q1 & Q2)
        if (questionNumber == 1)
        {
            if (float.TryParse(userInput, out float ans) && Mathf.Abs(ans - myData.q1Answer) <= 0.1f) isCorrect = true;
        }
        else if (questionNumber == 2)
        {
            if (float.TryParse(userInput, out float ans) && Mathf.Abs(ans - myData.q2Answer) <= 0.1f) isCorrect = true;
        }
        // Grade Vocabulary Question (Q3)
        else if (questionNumber == 3)
        {
            if (userInput == myData.q3Answer.Trim().ToLower()) isCorrect = true;
        }

        // Apply visual feedback
        if (isCorrect)
        {
            if (inputBackground != null) inputBackground.color = correctColor;
            answerInput.interactable = false;
            if (checkButton != null) checkButton.interactable = false;
        }
        else
        {
            if (inputBackground != null) inputBackground.color = wrongColor;
        }
    }

    private void LaunchARView()
    {
        if (parentMenuPanel != null) parentMenuPanel.SetActive(false);
        if (sizeOMeterPanel != null) sizeOMeterPanel.SetActive(false);
        
        // Hide the bottom sliders!
        if (bottomSlidersPanel != null) bottomSlidersPanel.SetActive(false); 

        ARController ar = FindFirstObjectByType<ARController>();
        if (ar != null && myData != null)
        {
            Vector3 targetScale = Vector3.one; 
            if (questionNumber == 1) targetScale = myData.q1Dimensions;
            else if (questionNumber == 2) targetScale = myData.q2Dimensions;
            
            ar.ForceShapeScale(targetScale);
        }
    }

    IEnumerator LaunchARSafe()
    {
        if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
        {
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            yield return null; 
        }
        
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
        
        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }
        
        if (SceneFader.Instance != null) SceneFader.Instance.LoadSceneSmoothly("AR_Environment"); 
        else SceneManager.LoadScene("AR_Environment"); 
    }
}