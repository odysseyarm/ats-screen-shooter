using System.Collections.Generic;
using UnityEngine;

public class QualificationModeManager : MonoBehaviour
{
    [Header("Target Configuration")]
    [SerializeField] private List<GameObject> b27Targets = new List<GameObject>();
    
    [Header("Materials")]
    [SerializeField] private Material b27TargetMaterialDay;
    [SerializeField] private Material b27TargetMaterialNight;
    
    private LightingModeManager lightingModeManager;
    private Dictionary<Renderer, Material> originalMaterials = new Dictionary<Renderer, Material>();
    
    void Awake()
    {
        // Validate material assignments
        if (b27TargetMaterialDay == null || b27TargetMaterialNight == null)
        {
            Debug.LogError($"QualificationModeManager: Critical - Materials not assigned in Inspector! Day: {b27TargetMaterialDay}, Night: {b27TargetMaterialNight}");
        }
        
        // Store the Inspector-assigned list if it exists
        List<GameObject> inspectorList = null;
        if (b27Targets != null && b27Targets.Count > 0)
        {
            inspectorList = new List<GameObject>(b27Targets);
            Debug.Log($"QualificationModeManager: Found {inspectorList.Count} B27 targets from Inspector");
        }
        
        // Try to auto-populate from scene if no Inspector assignment
        if (inspectorList == null || inspectorList.Count == 0)
        {
            FindB27TargetsInScene();
        }
        else
        {
            b27Targets = inspectorList;
        }
        
        // Store original materials using sharedMaterial to avoid creating instances
        foreach (var target in b27Targets)
        {
            if (target != null)
            {
                Renderer renderer = GetTargetRenderer(target);
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    originalMaterials[renderer] = renderer.sharedMaterial;
                    Debug.Log($"QualificationModeManager: Stored original material '{renderer.sharedMaterial.name}' for {target.name}");
                }
            }
        }
    }
    
    void Start()
    {
        // Find the LightingModeManager
        lightingModeManager = FindObjectOfType<LightingModeManager>();
        if (lightingModeManager == null)
        {
            Debug.LogError("QualificationModeManager: LightingModeManager not found!");
        }
        
        // Clean up the list - remove any null entries
        int removedCount = b27Targets?.RemoveAll(t => t == null) ?? 0;
        if (removedCount > 0)
        {
            Debug.LogWarning($"QualificationModeManager: Removed {removedCount} null targets from list");
        }
        
        if (b27Targets == null || b27Targets.Count == 0)
        {
            Debug.LogError($"QualificationModeManager: No valid B27 targets available!");
            enabled = false;
            return;
        }
        
        Debug.Log($"QualificationModeManager: Starting with {b27Targets.Count} valid B27 targets");
        
        // Apply initial Day mode materials after a short delay
        StartCoroutine(ApplyInitialMaterials());
    }
    
    private System.Collections.IEnumerator ApplyInitialMaterials()
    {
        // Wait for scene to be fully loaded
        yield return new WaitForSeconds(0.1f);
        
        // Apply Day mode materials on startup
        Debug.Log("QualificationModeManager: Applying initial Day mode materials");
        UpdateTargetMaterials(false);
    }
    
    private void FindB27TargetsInScene()
    {
        b27Targets.Clear();
        
        // Find all GameObjects in the scene
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            // Look for Body1 objects that are part of B27 targets
            if (obj.name == "Body1")
            {
                Transform parent = obj.transform.parent;
                while (parent != null)
                {
                    if (parent.name.Contains("B27 Paper Target") || parent.name.Contains("PaperTarget"))
                    {
                        // Make sure it has a renderer and isn't already in our list
                        Renderer renderer = obj.GetComponent<Renderer>();
                        if (renderer != null && !b27Targets.Contains(obj))
                        {
                            b27Targets.Add(obj);
                            Debug.Log($"QualificationModeManager: Auto-detected B27 target: {obj.name} under {parent.name}");
                        }
                        break;
                    }
                    parent = parent.parent;
                }
            }
        }
        
        Debug.Log($"QualificationModeManager: Auto-detected {b27Targets.Count} B27 targets in scene");
    }
    
    private Renderer GetTargetRenderer(GameObject target)
    {
        // First check if the target itself has a renderer
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null) return renderer;
        
        // Otherwise look for Body1 child
        Transform body1 = target.transform.Find("Body1");
        if (body1 != null)
        {
            renderer = body1.GetComponent<Renderer>();
            if (renderer != null) return renderer;
        }
        
        // Last resort - check all children for Body1
        foreach (Transform child in target.GetComponentsInChildren<Transform>())
        {
            if (child.name == "Body1")
            {
                renderer = child.GetComponent<Renderer>();
                if (renderer != null) return renderer;
            }
        }
        
        return null;
    }
    
    public void UpdateTargetMaterials(bool isDarkMode)
    {
        if (b27TargetMaterialDay == null || b27TargetMaterialNight == null)
        {
            Debug.LogError($"QualificationModeManager: Materials not assigned! Day: {b27TargetMaterialDay}, Night: {b27TargetMaterialNight}");
            return;
        }
        
        Material materialToUse = isDarkMode ? b27TargetMaterialNight : b27TargetMaterialDay;
        string modeText = isDarkMode ? "NIGHT" : "DAY";
        
        Debug.Log($"QualificationModeManager: Starting update of {b27Targets.Count} targets to {modeText} mode");
        Debug.Log($"QualificationModeManager: Using material: {materialToUse.name} (Instance ID: {materialToUse.GetInstanceID()})");
        
        int updatedCount = 0;
        int failedCount = 0;
        foreach (var target in b27Targets)
        {
            if (target != null)
            {
                Renderer renderer = GetTargetRenderer(target);
                if (renderer != null)
                {
                    // Store previous material info for debugging
                    string previousMaterialName = renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "NULL";
                    
                    // Use sharedMaterial to avoid creating instances
                    renderer.sharedMaterial = materialToUse;
                    
                    // Force the renderer to update by reassigning the materials array
                    Material[] mats = renderer.sharedMaterials;
                    if (mats.Length > 0)
                    {
                        mats[0] = materialToUse;
                        renderer.sharedMaterials = mats;
                    }
                    
                    updatedCount++;
                    Debug.Log($"QualificationModeManager: Updated {target.name} from '{previousMaterialName}' to '{materialToUse.name}'");
                    
                    // Verify the material was actually applied
                    if (renderer.sharedMaterial != materialToUse)
                    {
                        Debug.LogError($"QualificationModeManager: Material verification failed for {target.name}! Expected: {materialToUse.name}, Got: {renderer.sharedMaterial?.name ?? "NULL"}");
                    }
                }
                else
                {
                    failedCount++;
                    Debug.LogWarning($"QualificationModeManager: No renderer found for target {target.name}");
                }
            }
            else
            {
                failedCount++;
                Debug.LogWarning("QualificationModeManager: Null target in list");
            }
        }
        
        Debug.Log($"QualificationModeManager: Material update complete - Updated: {updatedCount}, Failed: {failedCount}");
        
        // Force a refresh of the rendering
        #if !UNITY_EDITOR
        if (updatedCount > 0)
        {
            Debug.Log("QualificationModeManager: Forcing renderer refresh in build");
            foreach (var target in b27Targets)
            {
                if (target != null)
                {
                    Renderer renderer = GetTargetRenderer(target);
                    if (renderer != null)
                    {
                        renderer.enabled = false;
                        renderer.enabled = true;
                    }
                }
            }
        }
        #endif
    }
    
    public void RefreshMaterials()
    {
        if (lightingModeManager != null)
        {
            bool isDarkMode = lightingModeManager.GetCurrentMode() == LightingModeManager.LightingMode.Dark;
            UpdateTargetMaterials(isDarkMode);
        }
    }
    
    void OnDestroy()
    {
        // Optionally restore original materials using sharedMaterial
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
            {
                kvp.Key.sharedMaterial = kvp.Value;
            }
        }
    }
}