using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management; // REQUIRED FOR AR SAFE LAUNCH
using System.Collections;        // REQUIRED FOR COROUTINES

public class ExerciseCard : MonoBehaviour
{
    [Header("Which Exercise is this?")]
    [Tooltip("0 for Exercise 1, 1 for Exercise 2, etc.")]
    public int databaseIndex; 

    [Header("UI Elements")]
    public TMP_InputField answerInput;
    public Button checkButton;
    public Button viewInARButton; 
    public Image checkButtonImage; 

    void Start()
    {
        if (checkButton != null) checkButton.onClick.AddListener(CheckAnswer);
        if (viewInARButton != null) viewInARButton.onClick.AddListener(LaunchARView);
    }

    private void CheckAnswer()
    {
        if (ExercisesDatabase.Instance == null) return;
        
        ExerciseData currentEx = ExercisesDatabase.Instance.allExercises[databaseIndex];
        string userInput = answerInput.text.Trim().ToLower();

        if (string.IsNullOrEmpty(userInput))
        {
            if (ExercisePopupManager.Instance != null)
                ExercisePopupManager.Instance.ShowPopup(false, "Please type an answer first!");
            return;
        }

        bool isCorrect = false;

        if (currentEx.questionType == QuestionType.CalculateVolume)
        {
            if (float.TryParse(userInput, out float userNum))
            {
                if (Mathf.Abs(userNum - currentEx.correctVolume) <= 0.1f) isCorrect = true; 
            }
        }
        else if (currentEx.questionType == QuestionType.IdentifyShape)
        {
            if (userInput == currentEx.correctShapeName.ToLower()) isCorrect = true;
        }

        if (isCorrect)
        {
            if (checkButtonImage != null) checkButtonImage.color = new Color(0.3f, 0.8f, 0.3f);
            answerInput.interactable = false; 
            checkButton.interactable = false; 
            
            if (ExercisePopupManager.Instance != null)
                ExercisePopupManager.Instance.ShowPopup(true, currentEx.solutionText);
        }
        else
        {
            if (ExercisePopupManager.Instance != null)
                ExercisePopupManager.Instance.ShowPopup(false, currentEx.hintText);
        }
    }

    // ==========================================
    // THE SAFE AR LAUNCH PROTOCOL
    // ==========================================
    private void LaunchARView()
    {
        if (ExercisesDatabase.Instance != null)
        {
            ExercisesDatabase.Instance.currentExerciseIndex = databaseIndex;
        }
        
        ExerciseManager.isExerciseMode = true;
        
        // Trigger the safe launch instead of a direct scene load
        StartCoroutine(LaunchARSafe());
    }

    IEnumerator LaunchARSafe()
    {
        // 1. Safely shut down the previous camera pipeline
        if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
        {
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            yield return null; 
        }

        // 2. Boot it up fresh
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }
        
        // 3. Load the scene! (Make sure the name exactly matches your exercise scene)
        // 3. Load the scene! (Changed back to your actual AR scene name!)
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.LoadSceneSmoothly("AR_Environment"); 
        }
        else
        {
            SceneManager.LoadScene("AR_Environment"); 
        }
    }
}