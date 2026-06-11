using UnityEngine;

public enum QuestionType { CalculateVolume, IdentifyShape }

[System.Serializable]
public class ExerciseData
{
    [Header("Problem Identification")]
    public string exerciseName;
    public QuestionType questionType; // Math vs Vocabulary choice

    [Header("AR Spawning Properties (Desktop Scale)")]
    public string shapeType; // Must match exactly: Cuboid, Cylinder, Sphere, TriangularPrism, Cone, or Pyramid
    public float dimensionX; // Length or Diameter or Base width
    public float dimensionY; // Height
    public float dimensionZ; // Width or Length
    public string unitType;  // "m", "cm", "in"

    [Header("UI Text Fields")]
    [TextArea(2, 4)]
    public string problemText; 
    [TextArea(2, 3)]
    public string hintText;
    [TextArea(3, 5)]
    public string solutionText;

    [Header("Acceptable Answers")]
    public float correctVolume;     // Used if CalculateVolume
    public string correctShapeName; // Used if IdentifyShape
}

public class ExercisesDatabase : MonoBehaviour
{
    public static ExercisesDatabase Instance;

    [Header("Current Active Progression")]
    public int currentExerciseIndex = 0;
    
    [Header("The 15 Exercises")]
    public ExerciseData[] allExercises; 

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keeps it alive across scenes
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