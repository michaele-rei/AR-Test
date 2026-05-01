using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

[RequireComponent(typeof(Button))]
public class BouncyButtonTween : MonoBehaviour
{
    [Header("Bounce Settings")]
    public float expandAmount = 1.03f; // Expands by 5%
    public float duration = 0.13f;     // Quick, snappy timing

    [Header("What happens after the bounce?")]
    public UnityEvent onAnimationComplete;

    private Button btn;
    private Vector3 originalScale;

    void Start()
    {
        btn = GetComponent<Button>();
        originalScale = transform.localScale;

        // Listen for the click automatically
        btn.onClick.AddListener(PlayBounce);
    }

    void PlayBounce()
    {
        // 1. Disable the button so the user can't spam-click it
        btn.interactable = false;

        // 2. Create an animation sequence
        Sequence bounceSeq = DOTween.Sequence();
        
        // 3. Expand out slightly
        bounceSeq.Append(transform.DOScale(originalScale * expandAmount, duration).SetEase(Ease.OutQuad));
        
        // 4. Snap back to original size
        bounceSeq.Append(transform.DOScale(originalScale, duration).SetEase(Ease.InQuad));
        
        // 5. When finished, re-enable the button and fire the actual panel transition
        bounceSeq.OnComplete(() =>
        {
            btn.interactable = true;
            onAnimationComplete.Invoke(); 
        });
    }
}