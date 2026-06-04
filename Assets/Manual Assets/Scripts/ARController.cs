using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public class ARController : MonoBehaviour
{
    [Header("Spawnable Objects")]
    public GameObject cuboidPrefab;          // UPDATED: Replaced Cube
    public GameObject triangularPrismPrefab; // UPDATED: Replaced Rectangular Prism
    public GameObject pyramidPrefab;
    public GameObject conePrefab;
    public GameObject cylinderPrefab;
    public GameObject spherePrefab;

    [Header("UI & Integration")]
    public ARUIManager uiManager; 

    [Header("AR Components")]
    public GameObject promptText;
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    [Header("Exercise Elements")]
    public GameObject dimensionPrefab;
    public GameObject volumeLabelPrefab;

    [Header("Primary Colors System")]
    public Material matRed;
    public Material matGreen;
    public Material matYellow;
    private Material currentSelectedMaterial;

    [Header("Code-Driven Color System")]
    private Color currentSelectedColor = Color.red;

    private List<GameObject> activeDimensions = new List<GameObject>();
    private GameObject volumeLabelObj;
    private GameObject spawnedObject;
    private string selectedShape;
    private bool isPlaced = false;

    private bool isShowcasing = false;
    private float showcaseTimer = 0f;
    private bool hasSeenShowcase = false; 

    void Start()
    {
        // Default to Red if they haven't picked a color yet
        currentSelectedMaterial = matRed;
        // NEW: Forces AR Foundation to completely reset the camera/simulator pipe on load!
        if (LoaderUtility.GetActiveLoader() != null) { LoaderUtility.GetActiveLoader().Initialize(); }

        selectedShape = string.IsNullOrEmpty(ButtonScript.selectedShape) ? "Cube" : ButtonScript.selectedShape;

        if (ExerciseManager.isExerciseMode) {
            if (promptText != null) promptText.GetComponent<TextMeshProUGUI>().text = "Touch & Hold to place";
        }
    }

    void Update()
    {
        if (isPlaced && spawnedObject != null)
        {
            if (isShowcasing) AnimateShowcase();
            BillboardLabels();
            return;
        }

        if (!isPlaced && Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                if (IsTouchingUI(touch)) return;
                SpawnGhost(touch.position);
            }
            else if (touch.phase == TouchPhase.Moved && spawnedObject != null)
            {
                MoveGhost(touch.position);
            }
            else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && spawnedObject != null)
            {
                FinalizePlacement();
            }
        }
    }

    void BillboardLabels()
    {
        if (spawnedObject == null) return;
        Vector3 dirToCamera = Camera.main.transform.position - spawnedObject.transform.position;
        dirToCamera.y = 0;
        Quaternion textRotation = Quaternion.LookRotation(dirToCamera);
        textRotation *= Quaternion.Euler(0, 180, 0);

        if (volumeLabelObj != null) volumeLabelObj.transform.rotation = textRotation;
        foreach (var dim in activeDimensions)
        {
            if (dim != null)
            {
                TextMeshPro label = dim.GetComponentInChildren<TextMeshPro>();
                if (label != null) label.transform.rotation = textRotation;
            }
        }
    }

    void AnimateShowcase()
    {
        if (spawnedObject == null) return;
        showcaseTimer += Time.deltaTime;
        float valX = 0.5f + Mathf.Sin(showcaseTimer * 1.5f) * 0.2f;
        float valY = 0.5f + Mathf.Cos(showcaseTimer * 1.2f) * 0.2f;
        float valZ = 0.5f + Mathf.Sin(showcaseTimer * 0.8f) * 0.2f;

        ApplyMathAndScale(valX, valY, valZ, true);
        if (uiManager != null) uiManager.UpdateSlidersSilently(valX, valY, valZ);
    }

    void CreateVolumeLabel(string text, float scaleRef, Vector3 localPos)
    {
        if (volumeLabelPrefab == null) return;
        volumeLabelObj = Instantiate(volumeLabelPrefab, spawnedObject.transform);
        
        float absoluteTextSize = 0.025f; 
        Vector3 pScale = spawnedObject.transform.localScale;
        
        float safeX = Mathf.Max(Mathf.Abs(pScale.x), 0.001f);
        float safeY = Mathf.Max(Mathf.Abs(pScale.y), 0.001f);
        float safeZ = Mathf.Max(Mathf.Abs(pScale.z), 0.001f);

        volumeLabelObj.transform.localScale = new Vector3(absoluteTextSize / safeX, absoluteTextSize / safeY, absoluteTextSize / safeZ);

        float worldHeightOffset = (scaleRef / 2f) + 0.08f; 
        volumeLabelObj.transform.position = spawnedObject.transform.position + new Vector3(0, worldHeightOffset, 0);
        
        volumeLabelObj.GetComponent<TextMeshPro>().text = text;
    }

    void SpawnGhost(Vector2 touchPos)
    {
        Pose hitPose = GetPlanePosition(touchPos);
        if (hitPose == Pose.identity) return;

        // UPDATED: Now checks for Cuboid and TriangularPrism
        GameObject prefabToUse = cuboidPrefab; 
        switch (selectedShape)
        {
            case "TriangularPrism": prefabToUse = triangularPrismPrefab; break;
            case "Pyramid": prefabToUse = pyramidPrefab; break;
            case "Cone": prefabToUse = conePrefab; break;
            case "Cylinder": prefabToUse = cylinderPrefab; break;
            case "Sphere": prefabToUse = spherePrefab; break;
            case "Cuboid": prefabToUse = cuboidPrefab; break;
        }

        spawnedObject = Instantiate(prefabToUse, hitPose.position, hitPose.rotation);
        ApplyColorToMesh();

        ChangeSpawnedColor(currentSelectedMaterial);

        Vector3 lookPos = new Vector3(Camera.main.transform.position.x, spawnedObject.transform.position.y, Camera.main.transform.position.z);
        spawnedObject.transform.LookAt(lookPos);
    }

    void MoveGhost(Vector2 touchPos)
    {
        Pose hitPose = GetPlanePosition(touchPos);
        if (hitPose != Pose.identity)
        {
            spawnedObject.transform.position = Vector3.Lerp(spawnedObject.transform.position, hitPose.position, 0.2f);
        }
    }

    void FinalizePlacement()
    {
        isPlaced = true;
        TogglePlaneDetection(false);
        if (promptText != null) promptText.SetActive(false);

        if (ExerciseManager.isExerciseMode)
        {
            SetupExerciseVisuals();
            DoSpawnAnimation(); 
        }
        else
        {
            if (uiManager != null) uiManager.SetupUIForShape(selectedShape);
            DoSpawnAnimation();

            if (!hasSeenShowcase)
            {
                DOVirtual.DelayedCall(0.6f, () => 
                {
                    if (isPlaced) 
                    {
                        isShowcasing = true;
                        showcaseTimer = 0f;
                        hasSeenShowcase = true; 
                    }
                });
            }
            else
            {
                isShowcasing = false;
            }
        }
    }

    void StopShowcase() { if (isShowcasing) isShowcasing = false; }

    public void UpdateDimensionsFromUI(float valX, float valY, float valZ, bool isUserInput)
    {
        if (spawnedObject == null) return;
        if (isUserInput) isShowcasing = false; 
        ApplyMathAndScale(valX, valY, valZ, false);
    }

    void ApplyMathAndScale(float x, float y, float z, bool isWatchMode)
    {
        ClearOldLabels();
        float volume = 0f;
        Vector3 volumeLabelPos = new Vector3(0, 0.5f + 0.2f, 0);

        string u = uiManager != null ? uiManager.currentUnit : "m";
        float mult = uiManager != null ? uiManager.GetMultiplierToMeters(u) : 1f;

        float dX = x / mult; 
        float dY = y / mult; 
        float dZ = z / mult;

        // --- UPDATED: Cuboid Logic ---
        if (selectedShape == "Cuboid")
        {
            spawnedObject.transform.localScale = new Vector3(x, y, z);
            volume = x * y * z;
            Vector3 p = new Vector3(-0.5f, -0.5f, -0.5f);
            
            CreateDimension(p, new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, -0.05f, -0.05f), $"L = {dX:F2} {u}");
            CreateDimension(p, new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.05f, 0, -0.05f), $"H = {dY:F2} {u}");
            CreateDimension(p, new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.05f, -0.05f, 0), $"W = {dZ:F2} {u}");
            
            CreateVolumeLabel($"Vol: {(volume / Mathf.Pow(mult, 3)):F2} {u}³", (x+y+z)/3f, volumeLabelPos);
        }
        // --- UPDATED: Triangular Prism Logic ---
        else if (selectedShape == "TriangularPrism")
        {
            spawnedObject.transform.localScale = new Vector3(x, y, z);
            volume = 0.5f * x * y * z; // Area of triangle base (0.5 * b * h) * length

            Vector3 p = new Vector3(-0.5f, -0.5f, -0.5f);
            
            // Base of the triangle (X-axis)
            CreateDimension(p, new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, -0.05f, -0.05f), $"b = {dX:F2} {u}");
            // Height of the triangle (Y-axis)
            CreateDimension(p, new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.05f, 0, 0), $"h = {dY:F2} {u}");
            // Length of the prism (Z-axis)
            CreateDimension(p, new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.05f, -0.05f, 0), $"L = {dZ:F2} {u}");

            CreateVolumeLabel($"Vol: {(volume / Mathf.Pow(mult, 3)):F2} {u}³", (x+y+z)/3f, volumeLabelPos);
        }
        else if (selectedShape == "Sphere")
        {
            spawnedObject.transform.localScale = Vector3.one * x;
            float r = x / 2f;
            volume = (4f / 3f) * Mathf.PI * Mathf.Pow(r, 3);
            
            CreateDimension(Vector3.zero, new Vector3(0.5f, 0, 0), new Vector3(0, 0.55f, 0), $"r = {(dX/2f):F2} {u}");
            CreateVolumeLabel($"Vol: {(volume / Mathf.Pow(mult, 3)):F2} {u}³", x, volumeLabelPos);
        }
        else if (selectedShape == "Cylinder")
        {
            spawnedObject.transform.localScale = new Vector3(x, y / 2f, x);
            float r = x / 2f;
            volume = Mathf.PI * Mathf.Pow(r, 2) * y;
            CreateDimension(new Vector3(0, 1f, 0), new Vector3(0.5f, 1f, 0), new Vector3(0, 0.1f, 0), $"r = {(dX/2f):F2} {u}");
            CreateDimension(new Vector3(-0.5f, -1f, 0), new Vector3(-0.5f, 1f, 0), new Vector3(-0.1f, 0, 0), $"h = {dY:F2} {u}");
            CreateVolumeLabel($"Vol: {(volume / Mathf.Pow(mult, 3)):F2} {u}³", (x+y)/2f, volumeLabelPos);
        }
        else if (selectedShape == "Cone")
        {
            spawnedObject.transform.localScale = new Vector3(x, y, x);
            float r = x / 2f;
            volume = (1f / 3f) * Mathf.PI * Mathf.Pow(r, 2) * y;
            CreateDimension(new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(0, 0.05f, 0), $"r = {(dX/2f):F2} {u}");
            CreateDimension(new Vector3(0, -0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0.6f, 0, 0), $"h = {dY:F2} {u}");
            CreateVolumeLabel($"Vol: {(volume / Mathf.Pow(mult, 3)):F2} {u}³", (x+y)/2f, volumeLabelPos);
        }
        else if (selectedShape == "Pyramid")
        {
            spawnedObject.transform.localScale = new Vector3(x, y, x);
            volume = (1f / 3f) * Mathf.Pow(x, 2) * y;
            
            Vector3 p = new Vector3(-0.5f, -0.5f, -0.5f); 
            CreateDimension(p, new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, -0.05f, -0.05f), $"b = {dX:F2} {u}"); 
            CreateDimension(p, new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.05f, -0.05f, 0), $"b = {dX:F2} {u}"); 
            CreateDimension(new Vector3(0, -0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0.6f, 0, 0), $"h = {dY:F2} {u}");
            
            CreateVolumeLabel($"Vol: {(volume / Mathf.Pow(mult, 3)):F2} {u}³", (x+y)/2f, volumeLabelPos);
        }

        if (uiManager != null) uiManager.UpdateDashboardVolume(volume);
        BillboardLabels();
    }

    Pose GetPlanePosition(Vector2 touchPos)
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(touchPos, hits, TrackableType.PlaneWithinPolygon)) return hits[0].pose;
        return Pose.identity;
    }

    void TogglePlaneDetection(bool status)
    {
        if (planeManager != null)
        {
            planeManager.enabled = status;
            foreach (var plane in planeManager.trackables) plane.gameObject.SetActive(status);
        }
    }

    public void ResetScene()
    {
        StopShowcase();
        if (spawnedObject != null) { Destroy(spawnedObject); spawnedObject = null; }
        ClearOldLabels();
        isPlaced = false;
        TogglePlaneDetection(true);
        if (promptText != null) promptText.SetActive(true);
        if (uiManager != null) uiManager.ResetUI();
    }

    bool IsTouchingUI(Touch t)
    {
        return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(t.fingerId);
    }

    void SetupExerciseVisuals()
    {
        ClearOldLabels();
        Vector3 defaultLabelPos = new Vector3(0, 0.7f, 0);

        // 1. Give everything a standard AR scale to start
        spawnedObject.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        // 2. Draw the correct lines based on the shape
        if (selectedShape == "Sphere")
        {
            CreateDimension(Vector3.zero, new Vector3(0.5f, 0, 0), new Vector3(0, 0.55f, 0), "Radius");
        }
        else if (selectedShape == "Cylinder")
        {
            spawnedObject.transform.localScale = new Vector3(0.4f, 0.2f, 0.4f); // Slightly squatter cylinder looks better
            CreateDimension(new Vector3(0, 1f, 0), new Vector3(0.5f, 1f, 0), new Vector3(0, 0.1f, 0), "Radius");
            CreateDimension(new Vector3(-0.5f, -1f, 0), new Vector3(-0.5f, 1f, 0), new Vector3(-0.1f, 0, 0), "Height");
        }
        else if (selectedShape == "Cone")
        {
            CreateDimension(new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(0, 0.05f, 0), "Radius");
            CreateDimension(new Vector3(0, -0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0.6f, 0, 0), "Height");
        }
        else if (selectedShape == "TriangularPrism")
        {
            Vector3 p = new Vector3(-0.5f, -0.5f, -0.5f);
            CreateDimension(p, new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, -0.05f, -0.05f), "Base");
            CreateDimension(p, new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.05f, 0, 0), "Height");
            CreateDimension(p, new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.05f, -0.05f, 0), "Length");
        }
        else if (selectedShape == "Pyramid")
        {
            Vector3 p = new Vector3(-0.5f, -0.5f, -0.5f); 
            CreateDimension(p, new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, -0.05f, -0.05f), "Base"); 
            CreateDimension(p, new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.05f, -0.05f, 0), "Base"); 
            CreateDimension(new Vector3(0, -0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0.6f, 0, 0), "Height");
        }
        else // Cuboid
        {
            // FIX: Y is now -0.5f so it starts perfectly at the bottom!
            Vector3 p = new Vector3(-0.5f, -0.5f, -0.5f); 
            CreateDimension(p, new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, -0.05f, -0.05f), "Length");
            CreateDimension(p, new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.05f, 0, -0.05f), "Height");
            CreateDimension(p, new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.05f, -0.05f, 0), "Width");
        }

        // 3. Display a generic prompt instead of the hardcoded answer
        CreateVolumeLabel("Solve for Volume!", 0.4f, defaultLabelPos);
    }

    void CreateDimension(Vector3 start, Vector3 end, Vector3 offset, string text)
    {
        if (dimensionPrefab == null) return;
        GameObject dim = Instantiate(dimensionPrefab, spawnedObject.transform);
        dim.GetComponent<DimensionBuilder>().Configure(start, end, offset, text);
        activeDimensions.Add(dim);
    }

    void ClearOldLabels()
    {
        foreach (var d in activeDimensions) Destroy(d);
        activeDimensions.Clear();
        if (volumeLabelObj != null) Destroy(volumeLabelObj);
    }

    public void ExitAR()
    {
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.LoadSceneSmoothly(ExerciseManager.isExerciseMode ? "AR_Exercises" : "AR_Learning"); 
        }
        else
        {
            SceneManager.LoadScene(ExerciseManager.isExerciseMode ? "AR_Exercises" : "AR_Learning"); // Fallback
        }
    }
    
    void DoSpawnAnimation()
    {
        if (spawnedObject == null) return;

        Vector3 finalScale = spawnedObject.transform.localScale;
        spawnedObject.transform.localScale = Vector3.zero;
        spawnedObject.transform.DOScale(finalScale, 0.5f).SetEase(Ease.OutBack).SetLink(spawnedObject);

        if (volumeLabelObj != null)
        {
            Vector3 volScale = volumeLabelObj.transform.localScale;
            volumeLabelObj.transform.localScale = Vector3.zero;
            volumeLabelObj.transform.DOScale(volScale, 0.4f).SetEase(Ease.OutBack).SetDelay(0.2f).SetLink(volumeLabelObj);
        }

        foreach (var dim in activeDimensions)
        {
            if (dim != null)
            {
                Vector3 dimScale = dim.transform.localScale;
                dim.transform.localScale = Vector3.zero;
                dim.transform.DOScale(dimScale, 0.4f).SetEase(Ease.OutBack).SetDelay(0.1f).SetLink(dim.gameObject);
            }
        }
    }

    // Call this from your UI Buttons to change the color
    public void SetShapeColor(string colorName)
    {
        string cleanedColor = colorName.ToLower().Trim();

        if (cleanedColor == "red") 
            currentSelectedColor = Color.red;
        else if (cleanedColor == "green") 
            currentSelectedColor = Color.green;
        else if (cleanedColor == "yellow") 
            currentSelectedColor = Color.yellow;

        // If an object is already sitting on the table, repaint it instantly
        if (spawnedObject != null)
        {
            ApplyColorToMesh();
        }
    }

    private void ApplyColorToMesh()
    {
        Renderer[] renderers = spawnedObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            // Ensure we do not overwrite the yellow measurement/dimension layout lines!
            if (!(r is LineRenderer)) 
            {
                // Accessing '.material' instantiates a unique runtime copy automatically.
                // We use "_BaseColor" to guarantee compatibility with URP Lit shaders.
                r.material.SetColor("_BaseColor", currentSelectedColor);
            }
        }
    }

    private void ChangeSpawnedColor(Material targetMat)
    {
        if (targetMat == null) return;
        Renderer[] renderers = spawnedObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (!(r is LineRenderer)) // Keep yellow layout lines intact
            {
                r.material = targetMat;
            }
        }
    }

    public void Animate3DLabels()
    {
        if (spawnedObject == null) return;

        float punchIntensity = 0.3f; 
        float duration = 0.3f;

        if (volumeLabelObj != null)
        {
            volumeLabelObj.transform.DOKill(true);
            Vector3 currentScale = volumeLabelObj.transform.localScale;
            volumeLabelObj.transform.DOPunchScale(currentScale * punchIntensity, duration, 5, 1).SetLink(volumeLabelObj);
        }

        if (activeDimensions != null)
        {
            foreach (var dim in activeDimensions)
            {
                if (dim != null)
                {
                    dim.transform.DOKill(true);
                    Vector3 dimScale = dim.transform.localScale;
                    dim.transform.DOPunchScale(dimScale * punchIntensity, duration, 5, 1).SetLink(dim.gameObject);
                }
            }
        }
    }
}