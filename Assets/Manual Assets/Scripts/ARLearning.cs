using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management; // Essential for the Engine
using System.Collections;        // Essential for Coroutines

public class LearningMenuManager : MonoBehaviour
{
    [Header("Assign Your Panels Here")]
    public GameObject introPanel;
    public GameObject[] shapePanels;

    void Start()
    {
        // 1. FORCIBLY OPEN INTRO PANEL
        if (introPanel != null) introPanel.SetActive(true);

        // 2. F ORCIBLY CLOSE ALL SHAPE PANELS
        foreach (GameObject panel in shapePanels)
        {
            if (panel != null) panel.SetActive(false);
        }
    }

    public void OpenPanel(GameObject panelToOpen)
    {
        introPanel.SetActive(false);
        foreach (GameObject p in shapePanels)
        {
            if (p != null) p.SetActive(false);
        }

        if (panelToOpen != null) panelToOpen.SetActive(true);
    }

    public void GoToAREnvironment(string shapeName)
    {
        // 1. Tell the AR Controller which shape to load
        ButtonScript.selectedShape = shapeName; 
        
        // 2. Ensure we tell the app we are NOT in exercise mode!
        ExerciseManager.isExerciseMode = false;
        
        // 3. Start the AR Engine instead of just loading the scene
        StartCoroutine(LaunchARSafe());
    }

    // --- THE IGNITION SWITCH ---
    IEnumerator LaunchARSafe()
    {
        // If an engine is already running, clear it.
        if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
        {
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            yield return null; 
        }

        // Start fresh
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }
        
        // Now that the engine is running, load the scene
        SceneManager.LoadScene("AR_Environment"); 
    }
}