using UnityEngine;
using UnityEngine.EventSystems;

public class BreathingLogo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Idle Breathing Settings")]
    public float breathSpeed = 2f;      
    public float breathAmount = 0.05f;  

    [Header("Hover Settings")]
    public float hoverScaleMultiplier = 1.15f; 
    public float hoverTransitionSpeed = 10f;   

    private Vector3 originalScale;
    private float targetBaseScale = 1f;
    private float currentBaseScale = 1f;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        currentBaseScale = Mathf.Lerp(currentBaseScale, targetBaseScale, Time.deltaTime * hoverTransitionSpeed);

        float breath = Mathf.Sin(Time.time * breathSpeed) * breathAmount;

        transform.localScale = originalScale * (currentBaseScale + breath);
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        targetBaseScale = hoverScaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetBaseScale = 1f;
    }
}