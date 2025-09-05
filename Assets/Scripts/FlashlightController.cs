using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FlashlightController : MonoBehaviour
{
    [Header("Flashlight Settings")]
    [SerializeField] private float intensity = 600f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float spotAngle = 25f;
    [SerializeField] private float innerSpotAngle = 10f;
    [SerializeField] private Color lightColor = new Color(0.95f, 0.95f, 1f);
    [SerializeField] private AnimationCurve falloffCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    
    [Header("Mounting Position")]
    [SerializeField] private Vector3 mountOffset = new Vector3(0.1f, -0.15f, 0.3f);
    
    [Header("Responsive Distance Settings")]
    [SerializeField]
    [Tooltip("Enable/disable the responsive distance feature for flashlight GameObject movement")]
    private bool responsiveDistanceEnabled = true;
    
    [SerializeField]
    [Tooltip("Distance scaling ratio - for every 1 unit of tracking distance, move flashlight GameObject this many units on Z")]
    private float distanceScalingRatio = 1.0f;
    
    [SerializeField]
    [Tooltip("The base Z position when tracking distance is zero")]
    private float baseZPosition = 0f;
    
    [SerializeField]
    [Tooltip("Minimum Z position")]
    private float minZPosition = -10f;
    
    [SerializeField]
    [Tooltip("Maximum Z position")]
    private float maxZPosition = 10f;
    
    [SerializeField]
    [Tooltip("Smoothing factor for position changes (0 = instant, 1 = no movement)")]
    [Range(0f, 0.99f)]
    private float smoothingFactor = 0.3f;
    
    [Header("References")]
    [SerializeField] private Light flashlightLight;
    
    private bool isEnabled = false;
    private Transform attachmentPoint;
    private Camera targetCamera;
    private InputHandlers inputHandlers;
    private Vector3 targetPosition;
    private Vector3 currentVelocity;
    private float lastTrackedDistance = 0f;
    private Vector3 initialLocalPosition;
    
    void Awake()
    {
        if (flashlightLight == null)
        {
            CreateFlashlight();
        }
        
        SetFlashlightEnabled(false);
    }
    
    void Start()
    {
        inputHandlers = FindObjectOfType<InputHandlers>();
        
        if (inputHandlers == null)
        {
            Debug.LogError("[FlashlightController] InputHandlers not found! Responsive distance will not work.");
        }
        else
        {
        // InputHandlers found, responsive distance ready
        }
        
        GameObject projectionCamera = GameObject.Find("ProjectionPlaneCamera");
        if (projectionCamera != null)
        {
            targetCamera = projectionCamera.GetComponent<Camera>();
        }
        else
        {
            targetCamera = Camera.main;
        }
        
        // Store initial position and set up target position
        initialLocalPosition = transform.localPosition;
        targetPosition = transform.position;
        targetPosition.z = baseZPosition;
        
        // FlashlightController initialized
    }
    
    void Update()
    {
        // Update GameObject position based on responsive distance
        if (responsiveDistanceEnabled && inputHandlers != null)
        {
            // Keep X and Y aligned with camera, only move Z
            if (targetCamera != null)
            {
                targetPosition.x = targetCamera.transform.position.x;
                targetPosition.y = targetCamera.transform.position.y;
            }
            
            if (inputHandlers.IsTracking)
            {
                float currentDistance = GetCurrentTrackingDistance();
                
                if (Mathf.Abs(currentDistance - lastTrackedDistance) > 0.001f)
                {
                    // Inverted relationship: closer to TV = flashlight GameObject moves farther forward on Z
                    float newZ = baseZPosition - (currentDistance * distanceScalingRatio);
                    float unclampedZ = newZ;
                    targetPosition.z = Mathf.Clamp(newZ, minZPosition, maxZPosition);
                    lastTrackedDistance = currentDistance;
                    
                    // Position updated based on tracking distance
                }
            }
            
            // Smooth the GameObject position transition
            Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothingFactor);
            transform.position = smoothedPosition;
            
            // Position smoothing applied
        }
        
        // Handle flashlight aiming (separate from GameObject movement)
        if (isEnabled && inputHandlers != null && targetCamera != null)
        {
            var players = inputHandlers.Players;
            if (players != null)
            {
                foreach (var (index, player) in players)
                {
                    if (player != null && player.point != null)
                    {
                        Vector2 screenPointNormal = player.point;
                        Vector3 screenPoint = new Vector3(
                            screenPointNormal.x * Screen.width,
                            Screen.height - screenPointNormal.y * Screen.height,
                            10f
                        );
                        
                        Ray ray = targetCamera.ScreenPointToRay(screenPoint);
                        
                        if (flashlightLight != null)
                        {
                            // Use the GameObject's current position (which moves with responsive distance)
                            // instead of camera position
                            Vector3 lightPosition = transform.position + 
                                targetCamera.transform.right * mountOffset.x +
                                targetCamera.transform.up * mountOffset.y +
                                targetCamera.transform.forward * mountOffset.z;
                            
                            Vector3 targetPoint;
                            RaycastHit hit;
                            if (Physics.Raycast(ray, out hit, range))
                            {
                                targetPoint = hit.point;
                            }
                            else
                            {
                                targetPoint = ray.origin + ray.direction * range;
                            }
                            
                            flashlightLight.transform.position = lightPosition;
                            Vector3 aimDirection = (targetPoint - lightPosition).normalized;
                            flashlightLight.transform.rotation = Quaternion.LookRotation(aimDirection);
                            
                            flashlightLight.intensity = intensity;
                            
                            // Light position updated
                        }
                        
                        break;
                    }
                }
            }
        }
    }
    
    private void CreateFlashlight()
    {
        GameObject flashlightObj = new GameObject("TacticalFlashlight");
        flashlightObj.transform.SetParent(transform);
        flashlightObj.transform.localPosition = Vector3.zero;
        flashlightObj.transform.localRotation = Quaternion.identity;
        
        flashlightLight = flashlightObj.AddComponent<Light>();
        flashlightLight.type = LightType.Spot;
        flashlightLight.intensity = intensity;
        flashlightLight.range = range;
        flashlightLight.spotAngle = spotAngle;
        flashlightLight.innerSpotAngle = innerSpotAngle;
        flashlightLight.color = lightColor;
        flashlightLight.shadows = LightShadows.Soft;
        flashlightLight.shadowStrength = 0.8f;
        flashlightLight.shadowBias = 0.05f;
        flashlightLight.shadowNormalBias = 0.4f;
        
        flashlightLight.renderMode = LightRenderMode.ForcePixel;
        flashlightLight.bounceIntensity = 0.1f;
        
        UniversalAdditionalLightData lightData = flashlightObj.AddComponent<UniversalAdditionalLightData>();
        
        lightData.usePipelineSettings = false;
        lightData.lightCookieSize = new Vector2(1f, 1f);
        lightData.lightCookieOffset = Vector2.zero;
        
        lightData.softShadowQuality = UnityEngine.Rendering.Universal.SoftShadowQuality.High;
    }
    
    public void AttachToTransform(Transform target)
    {
        // Store the attachment point but DON'T parent to it
        // This allows the flashlight GameObject to move independently
        attachmentPoint = target;
        // Attachment point stored without parenting to allow independent movement
    }
    
    public void ToggleFlashlight()
    {
        bool newState = !isEnabled;
        SetFlashlightEnabled(newState);
    }
    
    public void SetFlashlightEnabled(bool enabled)
    {
        isEnabled = enabled;
        if (flashlightLight != null)
        {
            flashlightLight.enabled = enabled;
        }
        else
        {
            Debug.LogWarning("FlashlightController: flashlightLight is null, cannot set enabled state");
        }
    }
    
    public bool IsEnabled()
    {
        return isEnabled;
    }
    
    public void SetIntensity(float newIntensity)
    {
        intensity = Mathf.Clamp(newIntensity, 0f, 500f);
        if (flashlightLight != null)
        {
            flashlightLight.intensity = intensity;
            flashlightLight.color = lightColor;
        }
    }
    
    public void SetRange(float newRange)
    {
        range = newRange;
        if (flashlightLight != null)
        {
            flashlightLight.range = range;
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
            // Reset GameObject to base Z position when disabling
            targetPosition = transform.position;
            targetPosition.z = baseZPosition;
            // Responsive distance disabled, position reset
        }
        else
        {
            // Responsive distance enabled
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
}