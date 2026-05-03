using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening; // <-- Added DOTween

public class SceneFader : MonoBehaviour
{
    // Singleton pattern so other scripts can talk to it easily
    public static SceneFader Instance { get; private set; }

    [Header("Transition Settings")]
    public Color fadeColor = new Color(0.43f, 0.51f, 0.94f, 1f); // That soft, friendly UI Blue
    public float fadeDuration = 0.4f;

    private Image fadeOverlay;

    void Awake()
    {
        // 1. Establish singleton logic
        if (Instance == null)
        {
            Instance = this;
            // DONT DESTROY ON LOAD: This object will persist across all scene changes!
            DontDestroyOnLoad(gameObject);
            
            // 2. NEW: Generate the entire curtain Canvas through pure code!
            CreateUICurtain();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // This dynamically creates the Canvas, sorts it, and adds the Image overlay. No Unity Editor setup needed!
    private void CreateUICurtain()
    {
        // Create the Canvas GameObject
        GameObject canvasObj = new GameObject("SceneFader_Canvas");
        canvasObj.transform.SetParent(this.transform);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Draws on top of everything!
        
        // Add components to handle screen resizing automatically
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); // Common mobile portrait resolution
        
        canvasObj.AddComponent<GraphicRaycaster>(); // Needed so the Image can block clicks

        // Create the Image GameObject
        GameObject imgObj = new GameObject("Fade_Overlay");
        imgObj.transform.SetParent(canvasObj.transform);
        fadeOverlay = imgObj.AddComponent<Image>();
        
        // Set the color and force it full-screen
        fadeOverlay.color = fadeColor;
        RectTransform rt = fadeOverlay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        // Initialize invisible and non-blocking
        fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        fadeOverlay.raycastTarget = false; 
    }

    // The main function we call from our buttons
    public void LoadSceneSmoothly(string sceneName)
    {
        if (fadeOverlay == null) return;

        // Enable blocking so the user can't double-tap other buttons while transitioning
        fadeOverlay.raycastTarget = true;

        // Fade to solid blue
        fadeOverlay.DOFade(1f, fadeDuration).SetUpdate(true).OnComplete(() =>
        {
            // Once screen is fully covered, load the scene
            SceneManager.LoadScene(sceneName);
            
            // tiny delay to let the new scene initialize (especially for heavy AR scenes)
            DOVirtual.DelayedCall(0.1f, () => 
            {
                // Fade back out to transparent
                fadeOverlay.DOFade(0f, fadeDuration).SetUpdate(true).OnComplete(() => 
                {
                    fadeOverlay.raycastTarget = false; // Allow clicks again
                });
            });
        });
    }
}