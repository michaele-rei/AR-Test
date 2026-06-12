using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ConceptCheckManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject conceptCheckPopup; 
    public TextMeshProUGUI titleText; // E.g., "Cuboid Checkpoint"

    [Header("Question Labels")]
    public TextMeshProUGUI q1Label;
    public TextMeshProUGUI q2Label;
    public TextMeshProUGUI q3Label;

    [Header("Student Input Fields")]
    public TMP_InputField q1Input;
    public TMP_InputField q2Input;
    public TMP_InputField q3Input;

    [Header("Input Backgrounds (For Colors)")]
    public Image q1Bg;
    public Image q2Bg;
    public Image q3Bg;

    private ConceptCheckData currentData;

    // Call this from the Pencil Button!
    public void OpenPencilMenu()
    {
        if (ExercisesDatabase.Instance == null)
        {
            Debug.LogError("ERROR: Cannot find the Database!");
            return;
        }

        string currentShape = ButtonScript.selectedShape;
        Debug.Log("PENCIL CLICKED: Looking for shape: [" + currentShape + "]");
        
        currentData = null; // Reset it just in case

        foreach (var check in ExercisesDatabase.Instance.shapeCheckpoints)
        {
            if (check.shapeType == currentShape)
            {
                Debug.Log("SUCCESS: Found the matching questions for " + currentShape);
                currentData = check;
                break;
            }
        }

        if (currentData != null)
        {
            titleText.text = currentShape + " Concept Check";
            q1Label.text = currentData.q1Text;
            q2Label.text = currentData.q2Text;
            q3Label.text = currentData.q3Text;
            
            q1Input.text = ""; q2Input.text = ""; q3Input.text = "";
            q1Input.interactable = true; q2Input.interactable = true; q3Input.interactable = true;
            q1Bg.color = Color.white; q2Bg.color = Color.white; q3Bg.color = Color.white;

            conceptCheckPopup.SetActive(true);
        }
        else
        {
            Debug.LogWarning("FAILED: Could not find [" + currentShape + "] in the database! Did you type the Shape Type correctly in the Inspector?");
        }
    }

    // Call this from the "Submit" button inside the popup!
    public void GradeAnswers()
    {
        if (currentData == null) return;
        Color correctColor = new Color(0.3f, 0.8f, 0.3f);
        Color wrongColor = new Color(0.9f, 0.4f, 0.4f);

        // Grade Q1 (Volume Math)
        if (float.TryParse(q1Input.text, out float ans1) && Mathf.Abs(ans1 - currentData.q1Answer) <= 0.1f)
        {
            q1Bg.color = correctColor;
            q1Input.interactable = false;
        }
        else q1Bg.color = wrongColor;

        // Grade Q2 (Reverse Math)
        if (float.TryParse(q2Input.text, out float ans2) && Mathf.Abs(ans2 - currentData.q2Answer) <= 0.1f)
        {
            q2Bg.color = correctColor;
            q2Input.interactable = false;
        }
        else q2Bg.color = wrongColor;

        // Grade Q3 (Conceptual Vocabulary)
        if (q3Input.text.Trim().ToLower() == currentData.q3Answer.Trim().ToLower())
        {
            q3Bg.color = correctColor;
            q3Input.interactable = false;
        }
        else q3Bg.color = wrongColor;
    }

    public void CloseMenu()
    {
        conceptCheckPopup.SetActive(false);
    }
}