using UnityEngine;
using ohc = Radiosity.OdysseyHubClient;

public class QualificationTargetController : MonoBehaviour
{
    [Header("Responsive Distance Settings")]
    [SerializeField]
    [Tooltip("Enable/disable the responsive distance feature - Disabled by default for Qualification Mode")]
    private bool responsiveDistanceEnabled = false;
    
    [SerializeField]
    [Tooltip("Distance scaling ratio - for every 1 unit of tracking distance, move target this many units on Z axis")]
    private float distanceScalingRatio = 1.0f;
    
    [SerializeField]
    [Tooltip("The base Z position when tracking distance is zero")]
    private float baseZPosition = 7.0f;
    
    [SerializeField]
    [Tooltip("Minimum Z position (closest to camera)")]
    private float minZPosition = 1.0f;
    
    [SerializeField]
    [Tooltip("Maximum Z position (farthest from camera)")]
    private float maxZPosition = 10.0f;
    
    [SerializeField]
    [Tooltip("Smoothing factor for position changes (0 = instant, 1 = no movement)")]
    [Range(0f, 0.99f)]
    private float smoothingFactor = 0.5f;
    
    [SerializeField]
    [Tooltip("Use camera position for minimum bound instead of fixed minZPosition")]
    private bool useCameraRelativeMin = false;
    
    [SerializeField]
    [Tooltip("Safety margin from camera when using camera-relative positioning (in meters)")]
    private float cameraMargin = 0.1f;
    
    [SerializeField]
    [Tooltip("Controls how quickly the target approaches its asymptotic limit (higher = faster approach)")]
    [Range(0.5f, 5.0f)]
    private float asymptoteSpeed = 2.0f;
    
    private InputHandlers inputHandlers;
    private AppModeManager appModeManager;
    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private float lastTrackedDistance = 0f;
    private float lastLogTime = 0f;
    private float logInterval = 1f; // Log once per second instead of every frame
    
    void Start()
    {
        inputHandlers = FindObjectOfType<InputHandlers>();
        appModeManager = FindObjectOfType<AppModeManager>();
        
        if (inputHandlers == null)
        {
            Debug.LogError("QualificationTargetController: InputHandlers not found in scene!");
        }
        
        if (appModeManager == null)
        {
            Debug.LogError("QualificationTargetController: AppModeManager not found in scene!");
        }
        
        targetPosition = transform.position;
        targetPosition.z = baseZPosition;
        transform.position = targetPosition;
    }
    
    void Update()
    {
        if (!responsiveDistanceEnabled)
        {
            return;
        }
        
        if (appModeManager != null && appModeManager.GetCurrentMode() != TargetMode.Qualification)
        {
            return;
        }
        
        bool shouldLog = Time.time - lastLogTime > logInterval;
        if (shouldLog)
        {
            lastLogTime = Time.time;
        }
        
        if (inputHandlers != null)
        {
            // For responsive distance, we use the Translation value to get device position
            // but we DON'T require IsTracking to be true (which would move the camera)
            // We just need to check if there's any translation data available
            Vector3 trackingTranslation = inputHandlers.Translation;
            if (trackingTranslation != Vector3.zero || inputHandlers.IsTracking)
            {
                float currentDistance = GetCurrentTrackingDistance();
                
                if (Mathf.Abs(currentDistance - lastTrackedDistance) > 0.001f)
                {
                    float deltaDistance = currentDistance - lastTrackedDistance;
                    float zAdjustment = deltaDistance * distanceScalingRatio;
                    
                    // Asymptotic movement: as user backs away, target approaches camera asymptotically
                    // currentDistance is positive when user backs away from screen
                    float d = currentDistance;
                    float absd = Mathf.Abs(d);
                    
                    // Optionally use camera position for minimum bound
                    float effectiveMinZ = minZPosition;
                    if (useCameraRelativeMin)
                    {
                        GameObject cam = GameObject.Find("FlyCam");
                        if (cam == null) cam = GameObject.Find("Main Camera");
                        if (cam != null)
                        {
                            float cameraZ = cam.transform.position.z;
                            effectiveMinZ = Mathf.Max(minZPosition, cameraZ + cameraMargin);
                        }
                    }
                    
                    // Calculate available range in each direction from base position
                    float towardCameraRange = Mathf.Max(0f, baseZPosition - effectiveMinZ);   // Range moving toward camera
                    float awayCameraRange = Mathf.Max(0f, maxZPosition - baseZPosition);     // Range moving away from camera
                    
                    // Select appropriate range based on direction
                    // When d > 0 (backing away), target moves toward camera (decreasing Z)
                    // When d < 0 (moving closer), target moves away from camera (increasing Z)
                    float range = d >= 0f ? towardCameraRange : awayCameraRange;
                    
                    // Use logarithmic asymptotic function for smooth approach to bounds
                    // This ensures we approach but never reach the min/max positions
                    float scaleFactor = Mathf.Abs(distanceScalingRatio);
                    
                    // Logarithmic mapping that asymptotically approaches 1 as distance increases
                    // The asymptoteSpeed parameter controls how quickly we approach the asymptote
                    float normalized = 0f;
                    
                    if (absd > 0f && range > 0f)
                    {
                        // Use a function that starts linear and becomes asymptotic
                        // tanh provides a smooth S-curve that approaches ±1
                        normalized = Mathf.Min(0.95f, (1f - Mathf.Exp(-asymptoteSpeed * scaleFactor * absd / range)));
                    }
                    
                    // Apply the asymptotic offset
                    // When backing away (d > 0): move toward camera (negative offset from base)
                    // When moving closer (d < 0): move away from camera (positive offset from base)
                    float zOffset = d >= 0f ? -towardCameraRange * normalized : awayCameraRange * normalized;
                    float newZ = baseZPosition + zOffset;
                    
                    targetPosition.z = Mathf.Clamp(newZ, minZPosition, maxZPosition);
                    
                    lastTrackedDistance = currentDistance;
                    
                }
                
                Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothingFactor);
                transform.position = smoothedPosition;
            }
        }
    }
    
    private float GetCurrentTrackingDistance()
    {
        if (inputHandlers == null)
        {
            return 0f;
        }
        
        // Don't check IsTracking here - we just need the translation data
        // Responsive distance should work even when camera tracking is disabled
        Vector3 trackingTranslation = inputHandlers.Translation;
        float distance = trackingTranslation.z;
        
        return Mathf.Abs(distance);
    }
    
    public void SetResponsiveDistanceEnabled(bool enabled)
    {
        responsiveDistanceEnabled = enabled;
        
        if (enabled)
        {
            // Responsive distance needs device tracking data but should NOT move the camera
            // We'll keep tracking disabled for camera movement but still get device data
            // The key is NOT setting inputHandlers.IsTracking = true here!
            
            // When enabling, use the current position as the new base position
            // This prevents the target from jumping when responsive mode is turned on
            baseZPosition = transform.position.z;
            targetPosition = transform.position;
            lastTrackedDistance = 0f;
            Debug.Log($"QualificationTargetController: Responsive mode enabled, starting from position Z={baseZPosition}");
            Debug.Log("QualificationTargetController: Note - NOT enabling IsTracking to prevent camera movement");
        }
        else
        {
            // When disabling, keep the target at its current position
            targetPosition = transform.position;
            Debug.Log("QualificationTargetController: Responsive mode disabled");
        }
    }
    
    public bool IsResponsiveDistanceEnabled()
    {
        return responsiveDistanceEnabled;
    }
    
    public void SetDistanceScalingRatio(float ratio)
    {
        distanceScalingRatio = Mathf.Max(0.1f, ratio);
    }
    
    public float GetDistanceScalingRatio()
    {
        return distanceScalingRatio;
    }
    
    public void ResetToBasePosition()
    {
        targetPosition.z = baseZPosition;
        transform.position = targetPosition;
        lastTrackedDistance = 0f;
    }
    
    public void SetBasePosition(float newBaseZ)
    {
        baseZPosition = newBaseZ;
        targetPosition.z = baseZPosition;
        targetPosition.x = transform.position.x;
        targetPosition.y = transform.position.y;
        transform.position = targetPosition;
        lastTrackedDistance = 0f;
        Debug.Log($"QualificationTargetController: Base position set to Z={baseZPosition}");
    }
}