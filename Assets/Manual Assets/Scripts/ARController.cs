using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.EventSystems; 

public class ARController : MonoBehaviour
{
    [Header("Spawnable Objects")]
    public GameObject cuboidPrefab;
    public GameObject triangularPrismPrefab;
    public GameObject pyramidPrefab;
    public GameObject conePrefab;
    public GameObject cylinderPrefab;
    public GameObject spherePrefab;

    [Header("UI & Integration")]
    public ARUIManager uiManager; 
    [Tooltip("Drag EVERY UI panel here that should be HIDDEN until placement (Colors, Sliders, Pencil, etc.)")]
    public GameObject[] uiPanelsToHideOnStart; // <-- NEW: Array list for multiple UI parts!

    [Header("AR Components")]
    public GameObject promptText;
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    [Header("Exercise Elements")]
    public GameObject dimensionPrefab;
    public GameObject volumeLabelPrefab;

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
    
    public bool isConceptCheckActive = false; 

    void Start()
    {
        // Hide EVERYTHING in the list at startup!
        foreach (GameObject panel in uiPanelsToHideOnStart)
        {
            if (panel != null) panel.SetActive(false);
        }

        ARSession arSession = FindFirstObjectByType<ARSession>();
        if (arSession != null) 
        {
            arSession.Reset();
        }

        isPlaced = false;
        isShowcasing = false;
        hasSeenShowcase = false;
        isConceptCheckActive = false;
        ClearOldLabels();
        
        if (spawnedObject != null) 
        {
            Destroy(spawnedObject);
            spawnedObject = null;
        }

        if (LoaderUtility.GetActiveLoader() != null) 
        { 
            LoaderUtility.GetActiveLoader().Initialize(); 
        }

        if (ExerciseManager.isExerciseMode && ExercisesDatabase.Instance != null)
        {
            ExerciseData currentEx = ExercisesDatabase.Instance.GetCurrentExercise();
            if (currentEx != null) selectedShape = currentEx.shapeType;
            
            if (promptText != null) promptText.GetComponent<TextMeshProUGUI>().text = "Touch & Hold to place";
        }
        else
        {
            selectedShape = string.IsNullOrEmpty(ButtonScript.selectedShape) ? "Cube" : ButtonScript.selectedShape;
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

            if (touch.phase == TouchPhase.Began && IsPointerOverUI()) return; 

            if (touch.phase == TouchPhase.Began)
            {
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

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId);
        }

        if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0))
        {
            return EventSystem.current.IsPointerOverGameObject();
        }

        return false;
    }

    void BillboardLabels()
    {
        if (spawnedObject == null) return;
        
        Vector3 dirToCamera = Camera.main.transform.position - spawnedObject.transform.position;
        dirToCamera.y = 0;
        Quaternion textRotation = Quaternion.LookRotation(dirToCamera);
        textRotation *= Quaternion.Euler(0, 180, 0);

        Vector3 parentScale = spawnedObject.transform.localScale;
        
        float safeX = Mathf.Max(Mathf.Abs(parentScale.x), 0.001f);
        float safeY = Mathf.Max(Mathf.Abs(parentScale.y), 0.001f);
        float safeZ = Mathf.Max(Mathf.Abs(parentScale.z), 0.001f);
        
        float textWorldSize = 0.035f; 
        
        Vector3 counterScale = new Vector3(textWorldSize / safeX, textWorldSize / safeY, textWorldSize / safeZ);

        if (volumeLabelObj != null) 
        {
            volumeLabelObj.transform.rotation = textRotation;
            volumeLabelObj.transform.localScale = counterScale;
        }

        foreach (var dim in activeDimensions)
        {
            if (dim != null)
            {
                TextMeshPro label = dim.GetComponentInChildren<TextMeshPro>();
                if (label != null) 
                {
                    label.transform.rotation = textRotation;
                    label.transform.localScale = counterScale; 
                }
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
        if (spawnedObject != null) return; 

        Vector3 planePosition;
        
        if (GetPlanePosition(touchPos, out planePosition))
        {
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

            spawnedObject = Instantiate(prefabToUse, planePosition, Quaternion.identity);
            ApplyColorToMesh();

            Vector3 lookPos = new Vector3(Camera.main.transform.position.x, spawnedObject.transform.position.y, Camera.main.transform.position.z);
            spawnedObject.transform.LookAt(lookPos);
        }
    }
    
    void MoveGhost(Vector2 touchPos)
    {
        Vector3 planePosition;
        
        if (GetPlanePosition(touchPos, out planePosition))
        {
            spawnedObject.transform.position = Vector3.Lerp(spawnedObject.transform.position, planePosition, 0.2f);
        }
    }

    private bool GetPlanePosition(Vector2 touchPos, out Vector3 position)
    {
        position = Vector3.zero;

        if (raycastManager == null)
        {
            raycastManager = GetComponent<ARRaycastManager>();
            if (raycastManager == null) raycastManager = FindFirstObjectByType<ARRaycastManager>();
            if (raycastManager == null)
            {
                Debug.LogError("AR_ERROR: ARRaycastManager is missing from the scene!");
                return false;
            }
        }

        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(touchPos, hits, TrackableType.PlaneWithinPolygon))
        {
            if (hits.Count > 0)
            {
                position = hits[0].pose.position;
                return true;
            }
        }

        return false;
    }

    void FinalizePlacement()
    {
        isPlaced = true;
        TogglePlaneDetection(false);
        if (promptText != null) promptText.SetActive(false);
        
        // SHOW EVERYTHING: Turn all the UI panels back on!
        foreach (GameObject panel in uiPanelsToHideOnStart)
        {
            if (panel != null) panel.SetActive(true);
        }

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
                    if (isPlaced && !isConceptCheckActive) 
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
        
        if (isConceptCheckActive) return; 

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
        else if (selectedShape == "TriangularPrism")
        {
            spawnedObject.transform.localScale = new Vector3(x, y, z);
            volume = 0.5f * x * y * z; 

            Vector3 p = new Vector3(-0.5f, -0.5f, -0.5f);
            CreateDimension(p, new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, -0.05f, -0.05f), $"b = {dX:F2} {u}");
            CreateDimension(p, new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.05f, 0, 0), $"h = {dY:F2} {u}");
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
            spawnedObject.transform.localScale = new Vector3(x, y, z);
            volume = (1f / 3f) * x * z * y; 
            
            Vector3 p = new Vector3(-0.5f, -0.5f, -0.5f); 
            CreateDimension(p, new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, -0.05f, -0.05f), $"b = {dX:F2} {u}"); 
            CreateDimension(p, new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.05f, -0.05f, 0), $"b = {dZ:F2} {u}"); 
            CreateDimension(new Vector3(0, -0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0.6f, 0, 0), $"h = {dY:F2} {u}");
            
            CreateVolumeLabel($"Vol: {(volume / Mathf.Pow(mult, 3)):F2} {u}³", (x+y+z)/3f, volumeLabelPos);
        }

        if (uiManager != null && !isConceptCheckActive) uiManager.UpdateDashboardVolume(volume);
        BillboardLabels();
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
        hasSeenShowcase = false; 
        isConceptCheckActive = false;
        
        // HIDE EVERYTHING: The object is gone, turn the list off!
        foreach (GameObject panel in uiPanelsToHideOnStart)
        {
            if (panel != null) panel.SetActive(false);
        }
        
        TogglePlaneDetection(true);
        if (promptText != null) promptText.SetActive(true);
        if (uiManager != null) uiManager.ResetUI();
    }

    void SetupExerciseVisuals()
    {
        ClearOldLabels();
        Vector3 defaultLabelPos = new Vector3(0, 0.7f, 0);

        if (ExercisesDatabase.Instance == null) return;
        ExerciseData currentEx = ExercisesDatabase.Instance.GetCurrentExercise();
        if (currentEx == null) return;

        // 2. Convert desktop real-world measurements to Unity world meters
        float conversionFactor = 1f; 
        if (currentEx.unitType == "cm") conversionFactor = 0.01f;
        else if (currentEx.unitType == "in") conversionFactor = 0.0254f;

        // ==========================================
        // MAKE IT BIGGER (HUMAN SIZED)
        // ==========================================
        // Multiply the realistic scale by a massive factor. 
        // 20f turns a tiny 5cm block into a massive 1-meter tall object!
        float giantMultiplier = 20f; 
        conversionFactor *= giantMultiplier;

        float x = currentEx.dimensionX * conversionFactor;
        float y = currentEx.dimensionY * conversionFactor;
        float z = currentEx.dimensionZ * conversionFactor;
        string u = currentEx.unitType;

        if (selectedShape == "Cuboid")
        {
            spawnedObject.transform.localScale = new Vector3(x, y, z);
            Vector3 p = new Vector3(-0.5f, -0.5f, -0.5f);
            CreateDimension(p, new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, -0.05f, -0.05f), $"L = {currentEx.dimensionX} {u}");
            CreateDimension(p, new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.05f, 0, -0.05f), $"H = {currentEx.dimensionY} {u}");
            CreateDimension(p, new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.05f, -0.05f, 0), $"W = {currentEx.dimensionZ} {u}");
        }
        else if (selectedShape == "TriangularPrism")
        {
            spawnedObject.transform.localScale = new Vector3(x, y, z);
            Vector3 p = new Vector3(-0.5f, -0.5f, -0.5f);
            CreateDimension(p, new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, -0.05f, -0.05f), $"b = {currentEx.dimensionX} {u}");
            CreateDimension(p, new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.05f, 0, 0), $"h = {currentEx.dimensionY} {u}");
            CreateDimension(p, new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.05f, -0.05f, 0), $"L = {currentEx.dimensionZ} {u}");
        }
        else if (selectedShape == "Sphere")
        {
            spawnedObject.transform.localScale = Vector3.one * x;
            CreateDimension(Vector3.zero, new Vector3(0.5f, 0, 0), new Vector3(0, 0.55f, 0), $"r = {(currentEx.dimensionX / 2f):F2} {u}");
        }   
        else if (selectedShape == "Cylinder")
        {
            spawnedObject.transform.localScale = new Vector3(x, y / 2f, x);
            CreateDimension(new Vector3(0, 1f, 0), new Vector3(0.5f, 1f, 0), new Vector3(0, 0.1f, 0), $"r = {currentEx.dimensionX} {u}");
            CreateDimension(new Vector3(-0.5f, -1f, 0), new Vector3(-0.5f, 1f, 0), new Vector3(-0.1f, 0, 0), $"h = {currentEx.dimensionY} {u}");
        }
        else if (selectedShape == "Cone")
        {
            spawnedObject.transform.localScale = new Vector3(x, y, x);
            CreateDimension(new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(0, 0.05f, 0), $"r = {currentEx.dimensionX} {u}");
            CreateDimension(new Vector3(0, -0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0.6f, 0, 0), $"h = {currentEx.dimensionY} {u}");
        }
        else if (selectedShape == "Pyramid")
        {
            spawnedObject.transform.localScale = new Vector3(x, y, z);
            Vector3 p = new Vector3(-0.5f, -0.5f, -0.5f); 
            CreateDimension(p, new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, -0.05f, -0.05f), $"b = {currentEx.dimensionX} {u}"); 
            CreateDimension(p, new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.05f, -0.05f, 0), $"b = {currentEx.dimensionZ} {u}"); 
            CreateDimension(new Vector3(0, -0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0.6f, 0, 0), $"h = {currentEx.dimensionY} {u}");
        }

        if (promptText != null)
        {
            promptText.SetActive(false);
            promptText.GetComponent<TextMeshProUGUI>().text = currentEx.problemText;
        }

        CreateVolumeLabel("Solve the Problem!", Mathf.Max(x, Mathf.Max(y, z)), defaultLabelPos);
        BillboardLabels();
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
            SceneManager.LoadScene(ExerciseManager.isExerciseMode ? "AR_Exercises" : "AR_Learning");
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

    public void SetShapeColor(string colorName)
    {
        string cleanedColor = colorName.ToLower().Trim();

        if (cleanedColor == "red") currentSelectedColor = Color.red;
        else if (cleanedColor == "green") currentSelectedColor = Color.green;
        else if (cleanedColor == "yellow") currentSelectedColor = Color.yellow;

        if (spawnedObject != null) ApplyColorToMesh();
    }

    private void ApplyColorToMesh()
    {
        Renderer[] renderers = spawnedObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (!(r is LineRenderer)) 
            {
                if (r.material.HasProperty("_BaseColor"))
                    r.material.SetColor("_BaseColor", currentSelectedColor);
                else
                    r.material.color = currentSelectedColor; 
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

    public void ForceShapeScale(Vector3 newScale)
    {
        if (spawnedObject != null)
        {
            isConceptCheckActive = true; 
            isShowcasing = false; 
            ApplyMathAndScale(newScale.x, newScale.y, newScale.z, false);
        }
    }

    public void ResetFromConceptCheck()
    {
        isConceptCheckActive = false; 
        
        if (spawnedObject != null)
        {
            ApplyMathAndScale(1f, 1f, 1f, false);
            if (uiManager != null) uiManager.UpdateSlidersSilently(1f, 1f, 1f);
        }
    }
}