using UnityEngine;
using TMPro;

[RequireComponent(typeof(LineRenderer))]
public class DimensionBuilder : MonoBehaviour
{
    public TextMeshPro labelText;
    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        
        // 1. Force the line to look clean, unlit, and exactly 5mm thick
        lineRenderer.startWidth = 0.005f; 
        lineRenderer.endWidth = 0.005f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true; // CRITICAL: This stops the line from getting "fat" when scaled
        
        // 2. Give it a clean, unlit material so it doesn't look dark or muddy
        Material unlitMat = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material = unlitMat;
        lineRenderer.startColor = Color.yellow;
        lineRenderer.endColor = Color.yellow;
    }

    public void Configure(Vector3 localStart, Vector3 localEnd, Vector3 localOffset, string text)
    {
        Transform parentSolid = transform.parent;

        Vector3 worldStart = parentSolid.TransformPoint(localStart + localOffset);
        Vector3 worldEnd = parentSolid.TransformPoint(localEnd + localOffset);

        lineRenderer.SetPosition(0, worldStart);
        lineRenderer.SetPosition(1, worldEnd);

        if (labelText != null)
        {
            labelText.text = text;
            float absoluteTextSize = 0.02f;
            Vector3 pScale = parentSolid.localScale;
            
            // FIX: Safety net! Force the scale to be at least 0.001 so we NEVER divide by zero.
            float safeX = Mathf.Max(Mathf.Abs(pScale.x), 0.001f);
            float safeY = Mathf.Max(Mathf.Abs(pScale.y), 0.001f);
            float safeZ = Mathf.Max(Mathf.Abs(pScale.z), 0.001f);

            labelText.transform.localScale = new Vector3(absoluteTextSize / safeX, absoluteTextSize / safeY, absoluteTextSize / safeZ);

            Vector3 midPoint = Vector3.Lerp(worldStart, worldEnd, 0.5f);
            labelText.transform.position = midPoint + new Vector3(0, 0.02f, 0); 
        }
    }
}