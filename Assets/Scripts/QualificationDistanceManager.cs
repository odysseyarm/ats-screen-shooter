using UnityEngine;
using System.Collections;

public class QualificationDistanceManager : MonoBehaviour
{
    [Header("Distance Settings")]
    [Tooltip("Preset shooting distances in yards")]
    public float[] presetDistancesYards = new float[] { 3f, 7f, 15f };
    
    [Tooltip("Current selected distance index")]
    private int currentDistanceIndex = 0;
    
    [Header("True-Size Mode")]
    [Tooltip("Whether true-size rendering is enabled")]
    private bool trueSizeEnabled = false;
    
    [Header("Conversion Settings")]
    [Tooltip("Conversion factor from yards to meters")]
    private const float YARDS_TO_METERS = 0.9144f;
    
    [Header("Camera Control")]
    [Tooltip("Base Z offset for the camera (distance from screen plane)")]
    [SerializeField]
    private float baseCameraOffset = 1.0f; // This should match distance_offset in InputHandlers
    
    [Header("Smoothing")]
    [Tooltip("Time in seconds to smooth camera movement")]
    [SerializeField]
    private float smoothingTime = 0.5f;
    
    private InputHandlers inputHandlers;
    private AppModeManager appModeManager;
    private Apt.Unity.Projection.BasicMovement basicMovement;
    private GameObject projectionCamera;
    
    private Vector3 targetTranslation;
    private Vector3 currentTranslation;
    private Vector3 deviceTrackingOffset = Vector3.zero;  // Store the gun/device tracking offset
    private Vector3 lastKnownRealTracking = Vector3.zero; // Store the last real tracking position
    private Coroutine smoothingCoroutine;
    private bool isInitialized = false;
    
    void Start()
    {
        
        // Find required components
        inputHandlers = FindObjectOfType<InputHandlers>();
        if (inputHandlers == null)
        {
            Debug.LogError("QualificationDistanceManager: InputHandlers not found!");
            enabled = false;
            return;
        }
        
        appModeManager = FindObjectOfType<AppModeManager>();
        if (appModeManager == null)
        {
            Debug.LogError("QualificationDistanceManager: AppModeManager not found!");
            enabled = false;
            return;
        }
        
        // Find the projection camera and BasicMovement component
        // Try FlyCam first (the actual camera being used)
        projectionCamera = GameObject.Find("FlyCam");
        if (projectionCamera == null)
        {
            // Fallback to Main Camera if FlyCam not found
            projectionCamera = GameObject.Find("Main Camera");
        }
        
        if (projectionCamera != null)
        {
            basicMovement = projectionCamera.GetComponent<Apt.Unity.Projection.BasicMovement>();
            if (basicMovement == null)
            {
                Debug.LogWarning($"QualificationDistanceManager: BasicMovement component not found on {projectionCamera.name}");
            }
        }
        else
        {
            Debug.LogWarning("QualificationDistanceManager: Neither FlyCam nor Main Camera found");
        }
        
        // Initialize with the first preset distance
        currentTranslation = Vector3.zero;
        targetTranslation = Vector3.zero;
        
        isInitialized = true;
        
        // Don't apply any distance on start - wait for explicit enable
        // TSR should only activate when user toggles it on
    }
    
    void OnEnable()
    {
        if (isInitialized && trueSizeEnabled && appModeManager != null && appModeManager.GetCurrentMode() == TargetMode.Qualification)
        {
            EnableTrueSizeMode();
        }
    }
    
    void OnDisable()
    {
        DisableTrueSizeMode();
    }
    
    /// <summary>
    /// Sets the shooting distance by index (0, 1, 2 for 3yd, 7yd, 15yd)
    /// </summary>
    public void SetDistanceByIndex(int index)
    {
        if (index < 0 || index >= presetDistancesYards.Length)
        {
            Debug.LogWarning($"QualificationDistanceManager: Invalid distance index {index}");
            return;
        }
        
        currentDistanceIndex = index;
        
        // Only apply distance if TSR is actually enabled
        if (trueSizeEnabled)
        {
            ApplyCurrentDistance();
        }
    }
    
    /// <summary>
    /// Sets the shooting distance in yards
    /// </summary>
    public void SetDistanceYards(float yards)
    {
        // Find the closest preset distance
        int closestIndex = 0;
        float closestDiff = Mathf.Abs(presetDistancesYards[0] - yards);
        
        for (int i = 1; i < presetDistancesYards.Length; i++)
        {
            float diff = Mathf.Abs(presetDistancesYards[i] - yards);
            if (diff < closestDiff)
            {
                closestDiff = diff;
                closestIndex = i;
            }
        }
        
        SetDistanceByIndex(closestIndex);
    }
    
    /// <summary>
    /// Gets the current shooting distance in yards
    /// </summary>
    public float GetCurrentDistanceYards()
    {
        if (currentDistanceIndex >= 0 && currentDistanceIndex < presetDistancesYards.Length)
        {
            return presetDistancesYards[currentDistanceIndex];
        }
        return 0f;
    }
    
    /// <summary>
    /// Gets the current shooting distance in meters
    /// </summary>
    public float GetCurrentDistanceMeters()
    {
        return GetCurrentDistanceYards() * YARDS_TO_METERS;
    }
    
    /// <summary>
    /// Enables or disables true-size rendering mode
    /// </summary>
    public void SetTrueSizeEnabled(bool enabled)
    {
        trueSizeEnabled = enabled;
        
        if (enabled && appModeManager != null && appModeManager.GetCurrentMode() == TargetMode.Qualification)
        {
            // Apply current distance when enabling
            ApplyCurrentDistance();
        }
        else if (!enabled)
        {
            // Disable true-size mode
            DisableTrueSizeMode();
        }
    }
    
    /// <summary>
    /// Gets whether true-size rendering is enabled
    /// </summary>
    public bool IsTrueSizeEnabled()
    {
        return trueSizeEnabled;
    }
    
    /// <summary>
    /// Updates the device tracking offset (gun movement)
    /// Call this from InputHandlers when device tracking updates
    /// </summary>
    public void UpdateDeviceTracking(Vector3 deviceOffset)
    {
        if (!trueSizeEnabled || !isInitialized)
            return;
            
        deviceTrackingOffset = deviceOffset;
        // Combine device tracking with distance offset
        Vector3 combinedTranslation = currentTranslation + deviceTrackingOffset;
        
        if (inputHandlers != null)
        {
            inputHandlers.Translation = combinedTranslation;
        }
    }
    
    /// <summary>
    /// Applies the current distance setting
    /// </summary>
    private void ApplyCurrentDistance()
    {
        if (!isInitialized || appModeManager == null)
            return;
        
        // Only apply if true-size mode is enabled
        if (!trueSizeEnabled)
            return;
        
        // Only apply if we're in Qualification Mode
        if (appModeManager.GetCurrentMode() != TargetMode.Qualification)
            return;
        
        float distanceMeters = GetCurrentDistanceMeters();
        
        // The camera needs to move BACKWARDS (negative Z) by the shooting distance
        // This simulates the shooter standing that far from the target
        targetTranslation = new Vector3(0, 0, -distanceMeters + baseCameraOffset);
        
        
        // Enable tracking mode and apply translation
        EnableTrueSizeMode();
        
        // Start smooth transition
        if (smoothingCoroutine != null)
        {
            StopCoroutine(smoothingCoroutine);
        }
        smoothingCoroutine = StartCoroutine(SmoothTransition());
    }
    
    /// <summary>
    /// Enables the true-size rendering mode
    /// </summary>
    private void EnableTrueSizeMode()
    {
        if (inputHandlers == null)
            return;
        
        // Force tracking mode on to enable the projection system
        // This is the key - it tells BasicMovement to use our translation values
        inputHandlers.IsTracking = true;
        
    }
    
    /// <summary>
    /// Disables the true-size rendering mode
    /// </summary>
    private void DisableTrueSizeMode()
    {
        if (inputHandlers == null)
            return;
        
        // Set target to home position (zero)
        targetTranslation = Vector3.zero;
        deviceTrackingOffset = Vector3.zero;
        
        // Stop any ongoing smooth transitions
        if (smoothingCoroutine != null)
        {
            StopCoroutine(smoothingCoroutine);
            smoothingCoroutine = null;
        }
        
        // Start smooth transition back to home
        // The transition coroutine will handle all the cleanup
        smoothingCoroutine = StartCoroutine(SmoothTransitionToHome());
    }
    
    /// <summary>
    /// Checks if there's actual device tracking happening (helmet/gun)
    /// </summary>
    private bool HasActiveDeviceTracking()
    {
        // Check if any actual devices are being tracked
        // This prevents us from interfering with real device tracking
        if (inputHandlers != null && inputHandlers.Players != null)
        {
            foreach (var (_, player) in inputHandlers.Players)
            {
                if (player != null && player.device != null)
                {
                    // Check if this is a helmet device
                    var device = new Radiosity.OdysseyHubClient.uniffi.Device(player.device);
                    if (inputHandlers.appConfig.Data.helmet_uuids.Contains(device.Uuid()))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }
    
    /// <summary>
    /// Smoothly transitions the camera position
    /// </summary>
    private IEnumerator SmoothTransition()
    {
        float elapsedTime = 0;
        Vector3 startTranslation = currentTranslation;
        
        while (elapsedTime < smoothingTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / smoothingTime;
            
            // Use smooth step for nicer transition
            t = t * t * (3f - 2f * t);
            
            currentTranslation = Vector3.Lerp(startTranslation, targetTranslation, t);
            
            // Apply the translation to InputHandlers (combined with device offset)
            if (inputHandlers != null)
            {
                Vector3 combinedTranslation = currentTranslation + deviceTrackingOffset;
                inputHandlers.Translation = combinedTranslation;
                lastKnownRealTracking = combinedTranslation;
            }
            
            yield return null;
        }
        
        // Ensure we end at exactly the target position
        currentTranslation = targetTranslation;
        if (inputHandlers != null)
        {
            Vector3 combinedTranslation = currentTranslation + deviceTrackingOffset;
            inputHandlers.Translation = combinedTranslation;
            lastKnownRealTracking = combinedTranslation;
        }
        
        smoothingCoroutine = null;
    }
    
    /// <summary>
    /// Smoothly transitions the camera back to home position
    /// </summary>
    private IEnumerator SmoothTransitionToHome()
    {
        float elapsedTime = 0;
        Vector3 startTranslation = currentTranslation;
        
        while (elapsedTime < smoothingTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / smoothingTime;
            
            // Use smooth step for nicer transition
            t = t * t * (3f - 2f * t);
            
            currentTranslation = Vector3.Lerp(startTranslation, Vector3.zero, t);
            
            // Apply the translation to InputHandlers
            if (inputHandlers != null)
            {
                // Check if other systems need tracking during our transition
                bool responsiveDistanceActive = false;
                QualificationTargetController targetController = FindObjectOfType<QualificationTargetController>();
                if (targetController != null)
                {
                    responsiveDistanceActive = targetController.IsResponsiveDistanceEnabled();
                }
                
                bool hasActiveDevice = HasActiveDeviceTracking();
                
                // Only update translation if we're still managing it
                if (!hasActiveDevice)
                {
                    if (responsiveDistanceActive)
                    {
                        // Keep X and Y from responsive distance, only reset Z
                        Vector3 currentTrans = inputHandlers.Translation;
                        inputHandlers.Translation = new Vector3(currentTrans.x, currentTrans.y, currentTranslation.z);
                    }
                    else
                    {
                        inputHandlers.Translation = currentTranslation;
                    }
                    lastKnownRealTracking = inputHandlers.Translation;
                }
            }
            
            yield return null;
        }
        
        // Ensure we end at exactly home position
        currentTranslation = Vector3.zero;
        targetTranslation = Vector3.zero;
        
        // Final cleanup
        bool finalResponsiveDistanceActive = false;
        QualificationTargetController finalTargetController = FindObjectOfType<QualificationTargetController>();
        if (finalTargetController != null)
        {
            finalResponsiveDistanceActive = finalTargetController.IsResponsiveDistanceEnabled();
        }
        
        bool finalHasActiveDevice = HasActiveDeviceTracking();
        
        if (!finalHasActiveDevice && !finalResponsiveDistanceActive)
        {
            // We can safely disable tracking and reset translation
            inputHandlers.IsTracking = false;
            inputHandlers.Translation = Vector3.zero;
        }
        else if (finalResponsiveDistanceActive && !finalHasActiveDevice)
        {
            // Responsive distance is active but no real device
            // Remove only our Z-axis contribution
            Vector3 currentTrans = inputHandlers.Translation;
            inputHandlers.Translation = new Vector3(currentTrans.x, currentTrans.y, 0);
        }
        
        smoothingCoroutine = null;
    }
    
    /// <summary>
    /// Called when the mode changes to/from Qualification
    /// </summary>
    public void OnModeChanged(TargetMode newMode)
    {
        if (newMode == TargetMode.Qualification)
        {
            // Entering Qualification Mode - only apply if TSR is enabled
            if (trueSizeEnabled)
            {
                ApplyCurrentDistance();
            }
        }
        else
        {
            // Leaving Qualification Mode - always disable TSR
            if (trueSizeEnabled)
            {
                // Force disable TSR when leaving qualification mode
                SetTrueSizeEnabled(false);
            }
            DisableTrueSizeMode();
        }
    }
    
    void Update()
    {
        // Keep applying our translation if we're in Qualification Mode and true-size is enabled
        // This ensures our values aren't overridden by other systems
        if (isInitialized && 
            trueSizeEnabled &&
            appModeManager != null && 
            appModeManager.GetCurrentMode() == TargetMode.Qualification &&
            inputHandlers != null)
        {
            // Maintain our tracking state and translation
            if (!inputHandlers.IsTracking)
            {
                inputHandlers.IsTracking = true;
            }
            
            // IMPORTANT: Don't fight with the real tracking system!
            // Instead of overwriting the translation, we should detect when it's been
            // changed by the real tracking and update our offset accordingly.
            
            Vector3 currentRealTracking = inputHandlers.Translation;
            
            // Check if the tracking has been updated by another system (like OdysseyHubClient)
            if (Vector3.Distance(currentRealTracking, lastKnownRealTracking) > 0.001f &&
                Vector3.Distance(currentRealTracking, currentTranslation + deviceTrackingOffset) > 0.001f)
            {
                // The tracking has been updated by another system
                // Extract the device movement from the new tracking position
                // Assume the Z component should maintain our distance offset
                deviceTrackingOffset = new Vector3(
                    currentRealTracking.x,
                    currentRealTracking.y,
                    currentRealTracking.z - currentTranslation.z
                );
                
                lastKnownRealTracking = currentRealTracking;
                
            }
            else if (smoothingCoroutine == null)
            {
                // Only update if we're not smoothing and the values don't match what we expect
                Vector3 desiredTranslation = currentTranslation + deviceTrackingOffset;
                
                if (Vector3.Distance(currentRealTracking, desiredTranslation) > 0.001f)
                {
                    inputHandlers.Translation = desiredTranslation;
                    lastKnownRealTracking = desiredTranslation;
                }
            }
        }
    }
}
