using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

public class ExercisePopupManager : MonoBehaviour
{
    public static ExercisePopupManager Instance;

    [Header("Popup UI Elements")]
    public GameObject popupPanel; // The dim background that covers the screen
    public RectTransform popupBox; // The actual white box that pops out
    public TextMeshProUGUI popupTitle;
    public TextMeshProUGUI popupMessage;

    [Header("Colors")]
    public Color correctColor = new Color(0.3f, 0.8f, 0.3f); // Green
    public Color wrongColor = new Color(0.9f, 0.3f, 0.3f);   // Red

    void Awake()
    {
        Instance = this;
        if (popupPanel != null) popupPanel.SetActive(false);
    }

    public void ShowPopup(bool isCorrect, string solutionOrHint)
    {
        popupPanel.SetActive(true);
        popupBox.localScale = Vector3.zero; // Start tiny for the bounce effect

        if (isCorrect)
        {
            popupTitle.text = "Correct!";
            popupTitle.color = correctColor;
            popupMessage.text = "Solution:\n\n" + solutionOrHint;
        }
        else
        {
            popupTitle.text = "Not Quite!";
            popupTitle.color = wrongColor;
            popupMessage.text = "Hint:\n\n" + solutionOrHint;
        }

        // Smooth DOTween pop-out
        popupBox.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
    }

    public void ClosePopup()
    {
        // Smoothly shrink it before turning it off
        popupBox.DOScale(0f, 0.2f).SetEase(Ease.InBack).OnComplete(() => 
        {
            popupPanel.SetActive(false);
        });
    }
}