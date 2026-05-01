using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ARController : MonoBehaviour
{
    [Header("Spawnable Objects")]
    public GameObject cubePrefab;
    public GameObject rectangularPrismPrefab;
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

    private List<GameObject> activeDimensions = new List<GameObject>();
    private GameObject volumeLabelObj;
    private GameObject spawnedObject;
    private string selectedShape;
    private bool isPlaced = false;

    private bool isShowcasing = false;
    private float showcaseTimer = 0f;
    
    // ISSUE 3 FIX: A memory lock so we don't repeat the showcase!
    private bool hasSeenShowcase = false; 

    void Start()
    {
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
        volumeLabelObj.transform.localPosition = localPos;
        volumeLabelObj.GetComponent<TextMeshPro>().text = text;
        if (scaleRef < 0.1f) scaleRef = 0.1f;
        volumeLabelObj.transform.localScale = Vector3.one * (1f / scaleRef) * 0.125f;
    }

    void SpawnGhost(Vector2 touchPos)
    {
        Pose hitPose = GetPlanePosition(touchPos);
        if (hitPose == Pose.identity) return;

        GameObject prefabToUse = cubePrefab;
        switch (selectedShape)
        {
            case "RectangularPrism": prefabToUse = rectangularPrismPrefab; break;
            case "Pyramid": prefabToUse = pyramidPrefab; break;
            case "Cone": prefabToUse = conePrefab; break;
            case "Cylinder": prefabToUse = cylinderPrefab; break;
            case "Sphere": prefabToUse = spherePrefab; break;
        }

        spawnedObject = Instantiate(prefabToUse, hitPose.position, hitPose.rotation);
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
        }
        else
        {
            // ISSUE 3 FIX: Only play the showcase if they haven't seen it yet
            if (!hasSeenShowcase)
            {
                isShowcasing = true;
                showcaseTimer = 0f;
                hasSeenShowcase = true; // Lock it for future placements
            }
            else
            {
                isShowcasing = false;
            }

            if (uiManager != null) uiManager.SetupUIForShape(selectedShape);
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

        // ISSUE 2 FIX: Pull the actual unit and multiplier from the UI to display realistically on the AR object
        string u = uiManager != null ? uiManager.currentUnit : "m";
        float mult = uiManager != null ? uiManager.GetMultiplierToMeters(u) : 1f;

        float dX = x / mult; 
        float dY = y / mult; 
        float dZ = z / mult;

        if (selectedShape == "Cube")
        {
            spawnedObject.transform.localScale = Vector3.one * x;
            volume = Mathf.Pow(x, 3);
            Vector3 p = new Vector3(-0.5f, -0.5f, -0.5f);
            CreateDimension(p, new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, -0.05f, -0.05f), $"s = {dX:F2}{u}");
            CreateVolumeLabel($"Vol: {(volume / Mathf.Pow(mult, 3)):F2}{u}³", x, volumeLabelPos);
        }
        else if (selectedShape == "Sphere")
        {
            spawnedObject.transform.localScale = Vector3.one * x;
            float r = x / 2f;
            volume = (4f / 3f) * Mathf.PI * Mathf.Pow(r, 3);
            CreateDimension(Vector3.zero, new Vector3(0.5f, 0, 0), Vector3.zero, $"r = {(dX/2f):F2}{u}");
            CreateVolumeLabel($"Vol: {(volume / Mathf.Pow(mult, 3)):F2}{u}³", x, volumeLabelPos);
        }
        else if (selectedShape == "Cylinder")
        {
            spawnedObject.transform.localScale = new Vector3(x, y / 2f, x);
            float r = x / 2f;
            volume = Mathf.PI * Mathf.Pow(r, 2) * y;
            CreateDimension(new Vector3(0, 1f, 0), new Vector3(0.5f, 1f, 0), new Vector3(0, 0.1f, 0), $"r = {(dX/2f):F2}{u}");
            CreateDimension(new Vector3(-0.5f, -1f, 0), new Vector3(-0.5f, 1f, 0), new Vector3(-0.1f, 0, 0), $"h = {dY:F2}{u}");
            CreateVolumeLabel($"Vol: {(volume / Mathf.Pow(mult, 3)):F2}{u}³", (x+y)/2f, volumeLabelPos);
        }
        else if (selectedShape == "Cone")
        {
            spawnedObject.transform.localScale = new Vector3(x, y, x);
            float r = x / 2f;
            volume = (1f / 3f) * Mathf.PI * Mathf.Pow(r, 2) * y;
            CreateDimension(new Vector3(0, 0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(0, 0.05f, 0), $"r = {(dX/2f):F2}{u}");
            CreateDimension(new Vector3(0, -0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0.6f, 0, 0), $"h = {dY:F2}{u}");
            CreateVolumeLabel($"Vol: {(volume / Mathf.Pow(mult, 3)):F2}{u}³", (x+y)/2f, volumeLabelPos);
        }
        else if (selectedShape == "Pyramid")
        {
            spawnedObject.transform.localScale = new Vector3(x, y, x);
            volume = (1f / 3f) * Mathf.Pow(x, 2) * y;
            CreateDimension(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, 0.02f, -0.1f), $"b = {dX:F2}{u}");
            CreateDimension(new Vector3(0, -0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(0.6f, 0, 0), $"h = {dY:F2}{u}");
            CreateVolumeLabel($"Vol: {(volume / Mathf.Pow(mult, 3)):F2}{u}³", (x+y)/2f, volumeLabelPos);
        }
        else // Rectangular Prism
        {
            spawnedObject.transform.localScale = new Vector3(x, y, z);
            volume = x * y * z;
            Vector3 p = new Vector3(-0.5f, -0.5f, -0.5f);
            CreateDimension(p, new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0, -0.05f, -0.05f), $"L={dX:F1}{u}");
            CreateDimension(p, new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.05f, 0, -0.05f), $"H={dY:F1}{u}");
            CreateDimension(p, new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.05f, -0.05f, 0), $"W={dZ:F1}{u}");
            CreateVolumeLabel($"Vol: {(volume / Mathf.Pow(mult, 3)):F2}{u}³", (x+y+z)/3f, volumeLabelPos);
        }

        if (uiManager != null) uiManager.UpdateDashboardVolume(volume);
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

        if (selectedShape == "Sphere")
        {
            spawnedObject.transform.localScale = Vector3.one * 0.6f;
            CreateDimension(Vector3.zero, new Vector3(0.5f, 0, 0), Vector3.zero, "r = 0.3m");
            CreateVolumeLabel("Volume ≈ 0.11m³", 0.6f, defaultLabelPos);
        }
        else
        {
            spawnedObject.transform.localScale = new Vector3(0.4f, 0.6f, 0.4f);
            Vector3 p = new Vector3(-0.5f, 0, -0.5f);
            CreateDimension(p, new Vector3(0.5f, 0, -0.5f), new Vector3(0, -0.05f, -0.05f), "L = 2m");
            CreateDimension(p, new Vector3(-0.5f, 1, -0.5f), new Vector3(-0.05f, 0, -0.05f), "H = ?");
            CreateDimension(p, new Vector3(-0.5f, 0, 0.5f), new Vector3(-0.05f, -0.05f, 0), "W = 2m");
            CreateVolumeLabel("Volume = 12m³", 0.5f, defaultLabelPos);
        }
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
        SceneManager.LoadScene(ExerciseManager.isExerciseMode ? "AR_Exercises" : "AR_Learning");
    }
}