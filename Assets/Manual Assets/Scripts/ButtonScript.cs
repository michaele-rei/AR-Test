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
    // Call this from your UI Mute Button!
    public void MuteMusic()
    {
        // 1. Search the entire game for the surviving Audio Manager
        GlobalAudioManager audioManager = FindObjectOfType<GlobalAudioManager>();

        // 2. If we found it, tell it to mute!
        if (audioManager != null)
        {
            // Note: Replace "ToggleMute" with whatever your actual mute function is called!
            audioManager.ToggleMute(); 
        }
        else
        {
            Debug.LogWarning("Could not find the GlobalAudioManager in the scene!");
        }
    }
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