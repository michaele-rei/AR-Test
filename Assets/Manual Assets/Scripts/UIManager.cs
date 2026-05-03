using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening; // <-- Added DOTween

public class UIManager : MonoBehaviour
{
    [Header("Panels to Control")]
    public GameObject[] allPanels;

    // We need to remember which panel is currently on the screen
    private GameObject currentActivePanel; 

    void Start()
    {
        // When the game starts, look through the list and find the panel that is already turned on
        foreach (GameObject panel in allPanels)
        {
            if (panel != null && panel.activeSelf)
            {
                currentActivePanel = panel;
                
                // --- THE FIX: Force the alpha and scale back to 100% ---
                CanvasGroup cg = panel.GetComponent<CanvasGroup>();
                if (cg != null) 
                {
                    cg.alpha = 1f;
                }
                panel.transform.localScale = Vector3.one;

                break; // Stop searching once we find the active one
            }
        }
    }

    // 1. UPGRADED NAVIGATION WIRING (Now with animations!)
    public void ShowPanel(GameObject panelToShow)
    {
        // If it's already the active panel, or the panel is missing, do nothing
        if (panelToShow == null || panelToShow == currentActivePanel) return;

        // If we have a panel currently on screen, fade it out first
        if (currentActivePanel != null)
        {
            CanvasGroup currentCG = GetOrAddCanvasGroup(currentActivePanel);
            
            // Shrink and fade out
            currentCG.DOFade(0f, 0.2f);
            currentActivePanel.transform.DOScale(0.9f, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
            {
                currentActivePanel.SetActive(false);
                
                // Once the old panel is gone, animate the new one in
                AnimatePanelIn(panelToShow);
            });
        }
        else
        {
            // If the screen was completely empty, just pop the new panel in
            AnimatePanelIn(panelToShow);
        }
    }

    // Helper function to animate the new panel popping in
    private void AnimatePanelIn(GameObject nextPanel)
    {
        CanvasGroup nextCG = GetOrAddCanvasGroup(nextPanel);
        
        // Prepare it (invisible and slightly small)
        nextPanel.SetActive(true);
        nextCG.alpha = 0f;
        nextPanel.transform.localScale = Vector3.one * 0.9f;

        // Fade and pop it in!
        nextCG.DOFade(1f, 0.3f);
        nextPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

        // Update the system's memory
        currentActivePanel = nextPanel;
    }

    // Helper function so we don't have to write GetComponent 100 times
    private CanvasGroup GetOrAddCanvasGroup(GameObject panel)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        return cg;
    }

    // 2. AR LAUNCH WIRING (Left completely untouched!)
    // 2. AR LAUNCH WIRING
    public void ViewInAR(string shapeName)
    {
        ButtonScript.selectedShape = shapeName; 
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.LoadSceneSmoothly("AR_Learning");
        }
        else
        {
            SceneManager.LoadScene("AR_Learning");
        }
    }
}