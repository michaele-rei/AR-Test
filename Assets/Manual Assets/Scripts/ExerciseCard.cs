using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ExerciseCard : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField answerInput;
    public Button checkButton;
    public Button viewInARButton; 
    public Image checkButtonImage; 

    [Header("Exercise Settings (Fill these in!)")]
    [Tooltip("Can be a number like '30' or a word like 'cylinder'")]
    public string correctAnswer; 
    [TextArea(3, 5)] public string solutionText;
    [TextArea(3, 5)] public string hintText;
    
    [Header("AR Settings")]
    public string arShapeName; // "Cuboid", "Cylinder", "Sphere", etc.

    void Start()
    {
        // Automatically hook up the buttons when the app starts
        if (checkButton != null) checkButton.onClick.AddListener(CheckAnswer);
        if (viewInARButton != null) viewInARButton.onClick.AddListener(LaunchARView);
    }

    private void CheckAnswer()
    {
        // 1. Clean up what they typed (remove accidental spaces and make it lowercase)
        string userInput = answerInput.text.Trim().ToLower();
        string expected = correctAnswer.Trim().ToLower();

        if (string.IsNullOrEmpty(userInput))
        {
            ExercisePopupManager.Instance.ShowPopup(false, "Please type an answer first!");
            return;
        }

        bool isCorrect = false;

        // 2. Try testing it as a Math Problem first (Allows for a 0.05 margin of error for Pi rounding)
        if (float.TryParse(userInput, out float userNum) && float.TryParse(expected, out float expectedNum))
        {
            if (Mathf.Abs(userNum - expectedNum) <= 0.05f) 
            {
                isCorrect = true; 
            }
        }
        // 3. If it's not a math problem, check if they typed the right word
        else if (userInput == expected)
        {
            isCorrect = true;
        }

        // 4. Trigger the results
        if (isCorrect)
        {
            checkButtonImage.color = new Color(0.3f, 0.8f, 0.3f); // Light up Green
            answerInput.interactable = false; // Lock the text box
            checkButton.interactable = false; // Lock the check button
            
            ExercisePopupManager.Instance.ShowPopup(true, solutionText);
        }
        else
        {
            ExercisePopupManager.Instance.ShowPopup(false, hintText);
        }
    }

    private void LaunchARView()
    {
        // Tell the AR Controller which shape to load
        ButtonScript.selectedShape = arShapeName; 
        
        // Ensure we tell the app we ARE in exercise mode
        ExerciseManager.isExerciseMode = true;
        
        // Launch the AR Scene smoothly
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.LoadSceneSmoothly("AR_Environment"); 
        }
        else
        {
            SceneManager.LoadScene("AR_Environment"); // Fallback
        }
    }
}