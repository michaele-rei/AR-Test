using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management; 
using System.Collections;        
using DG.Tweening; // <-- Added DOTween for the smooth UI panels!

public class LearningMenuManager : MonoBehaviour
{
    [Header("Assign Your Panels Here")]
    public GameObject introPanel;
    public GameObject[] shapePanels;

    // Track what is currently on the screen so we can fade it out
    private GameObject currentActivePanel;

    void Start()
    {
        // 1. FORCIBLY CLOSE ALL SHAPE PANELS
        foreach (GameObject panel in shapePanels)
        {
            if (panel != null) panel.SetActive(false);
        }

        // 2. FORCIBLY OPEN INTRO PANEL (And make sure it's visible!)
        if (introPanel != null) 
        {
            introPanel.SetActive(true);
            currentActivePanel = introPanel; // Memorize that this is on screen
            
            // --- THE FIX: Force the alpha and scale back to 100% ---
            CanvasGroup cg = introPanel.GetComponent<CanvasGroup>();
            if (cg != null) 
            {
                cg.alpha = 1f;
            }
            introPanel.transform.localScale = Vector3.one;
        }
    }

    // --- UPGRADED: Smooth Panel Switching ---
    public void OpenPanel(GameObject panelToOpen)
    {
        // Ignore if they click the button for the panel they are already looking at
        if (panelToOpen == null || panelToOpen == currentActivePanel) return;

        if (currentActivePanel != null)
        {
            CanvasGroup currentCG = GetOrAddCanvasGroup(currentActivePanel);
            
            // Shrink and fade out the old panel
            currentCG.DOFade(0f, 0.2f);
            currentActivePanel.transform.DOScale(0.9f, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
            {
                currentActivePanel.SetActive(false);
                AnimatePanelIn(panelToOpen);
            });
        }
        else
        {
            AnimatePanelIn(panelToOpen);
        }
    }

    private void AnimatePanelIn(GameObject nextPanel)
    {
        CanvasGroup nextCG = GetOrAddCanvasGroup(nextPanel);
        
        nextPanel.SetActive(true);
        nextCG.alpha = 0f;
        nextPanel.transform.localScale = Vector3.one * 0.9f;

        // Bounce and fade in the new panel
        nextCG.DOFade(1f, 0.3f);
        nextPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

        currentActivePanel = nextPanel;
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        return cg;
    }

    // --- AR ENGINE LAUNCH WIRING ---
    public void GoToAREnvironment(string shapeName)
    {
        ButtonScript.selectedShape = shapeName; 
        ExerciseManager.isExerciseMode = false;
        
        StartCoroutine(LaunchARSafe());
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
        
        // --- UPGRADED: Use the smooth Scene Curtain instead of the instant snap ---
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