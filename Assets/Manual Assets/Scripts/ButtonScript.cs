using UnityEngine;
using UnityEngine.SceneManagement; 
using DG.Tweening; 

public class ButtonScript : MonoBehaviour
{
    [Header("Textbook Pages (Only needed in Textbook Scene)")]
    public GameObject pageCube;
    public GameObject pageSphere;

    [Header("Audio")]
    public AudioSource menuMusic;

    // --- SHARED MEMORY ---
    public static string selectedShape = "Cube"; 

    // --- MAIN MENU FUNCTIONS ---
    public void StartGame()
    {
        // Use this button on the Main Menu to go to the Textbook

        if (menuMusic != null) menuMusic.DOFade(0f, 0.4f);

        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.LoadSceneSmoothly("AR_Learning"); 
        }
        else
        {
            SceneManager.LoadScene("AR_Learning"); // Fallback
        }
    }

    public void QuitApp()
    {
        Application.Quit();
        Debug.Log("App Quitting...");
    }

    // --- TEXTBOOK FUNCTIONS ---
    
    public void GoToSphere()
    {
        // We add this check so it doesn't crash if we use it in the wrong scene
        if (pageCube != null) pageCube.SetActive(false);
        if (pageSphere != null) pageSphere.SetActive(true);
        
        selectedShape = "Sphere";
    }

    public void GoToCube()
    {
        if (pageSphere != null) pageSphere.SetActive(false);
        if (pageCube != null) pageCube.SetActive(true);
        
        selectedShape = "Cube";
    }

    public void BackToMenu()
    {
        // FIX: Removed the .Instance check!
        ExerciseManager.isExerciseMode = false;

        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.LoadSceneSmoothly("MainMenu"); 
        }
        else
        {
            SceneManager.LoadScene("MainMenu"); // Fallback
        }
    }

    // --- AR FUNCTIONS ---
    // --- AR FUNCTIONS ---
    public void LaunchAR()
    {
        // FIX: Added SetLink so it safely cancels if the scene loads mid-fade!
        if (menuMusic != null) 
        {
            menuMusic.DOFade(0f, 0.4f).SetLink(menuMusic.gameObject);
        }

        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.LoadSceneSmoothly("AR_Environment"); 
        }
        else
        {
            SceneManager.LoadScene("AR_Environment"); // Fallback
        }
    }

    public void LaunchExercise()
    {
        // FIX: Added SetLink so it safely cancels if the scene loads mid-fade!
        if (menuMusic != null) 
        {
            menuMusic.DOFade(0f, 0.4f).SetLink(menuMusic.gameObject);
        }
        
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.LoadSceneSmoothly("AR_Exercises"); 
        }
        else
        {
            SceneManager.LoadScene("AR_Exercises"); // Fallback
        }
    }
}