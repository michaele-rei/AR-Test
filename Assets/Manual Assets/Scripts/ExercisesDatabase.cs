using UnityEngine;

public enum QuestionType { CalculateVolume, IdentifyShape }

[System.Serializable]
public class ExerciseData
{
    [Header("Problem Identification")]
    public string exerciseName;
    public QuestionType questionType; 

    [Header("AR Spawning Properties (Desktop Scale)")]
    public string shapeType; 
    public float dimensionX; 
    public float dimensionY; 
    public float dimensionZ; 
    public string unitType;  

    [Header("UI Text Fields")]
    [TextArea(2, 4)] public string problemText; 
    [TextArea(2, 3)] public string hintText;
    [TextArea(3, 5)] public string solutionText;

    [Header("Acceptable Answers")]
    public float correctVolume;     
    public string correctShapeName; 
}

// ==========================================
// NEW: THE 3-PART CONCEPT CHECK BLUEPRINT
// ==========================================
[System.Serializable]
public class ConceptCheckData
{
    public string shapeType; // e.g., "Cuboid", "Cylinder"
    
    [Header("Question 1: Find Volume")]
    [TextArea(2, 3)] public string q1Text;
    public float q1Answer;

    [Header("Question 2: Missing Dimension")]
    [TextArea(2, 3)] public string q2Text;
    public float q2Answer;

    [Header("Question 3: Conceptual")]
    [TextArea(2, 3)] public string q3Text;
    public string q3Answer;
}

public class ExercisesDatabase : MonoBehaviour
{
    public static ExercisesDatabase Instance;

    public int currentExerciseIndex = 0;
    
    [Header("The 15 Main Exercises")]
    public ExerciseData[] allExercises; 

    [Header("The Learning Checkpoints")]
    public ConceptCheckData[] shapeCheckpoints; // <--- NEW ARRAYS GO HERE

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public ExerciseData GetCurrentExercise()
    {
        if (allExercises == null || allExercises.Length == 0) return null;
        return allExercises[currentExerciseIndex];
    }
}