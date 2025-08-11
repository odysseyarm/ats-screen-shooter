using UnityEngine;
using ohc = Radiosity.OdysseyHubClient;

public class QualificationTargetController : MonoBehaviour
{
    [Header("Responsive Distance Settings")]
    [SerializeField]
    [Tooltip("Enable/disable the responsive distance feature")]
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
            
            if (inputHandlers.IsTracking)
            {
                float currentDistance = GetCurrentTrackingDistance();
                
                if (Mathf.Abs(currentDistance - lastTrackedDistance) > 0.001f)
                {
                    float deltaDistance = currentDistance - lastTrackedDistance;
                    float zAdjustment = deltaDistance * distanceScalingRatio;
                    
                    // Invert the relationship: closer to TV = target moves farther away
                    float newZ = baseZPosition - (currentDistance * distanceScalingRatio);
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
        if (inputHandlers == null || !inputHandlers.IsTracking)
        {
            return 0f;
        }
        
        Vector3 trackingTranslation = inputHandlers.Translation;
        float distance = trackingTranslation.z;
        
        return Mathf.Abs(distance);
    }
    
    public void SetResponsiveDistanceEnabled(bool enabled)
    {
        responsiveDistanceEnabled = enabled;
        
        if (!enabled)
        {
            targetPosition.z = baseZPosition;
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
}