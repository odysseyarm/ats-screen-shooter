using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FlashlightController : MonoBehaviour
{
    [Header("Flashlight Settings")]
    [SerializeField] private float intensity = 5000f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float spotAngle = 20f;
    [SerializeField] private float innerSpotAngle = 8f;
    [SerializeField] private Color lightColor = new Color(0.95f, 0.95f, 1f);
    [SerializeField] private AnimationCurve falloffCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f); // Falloff over distance
    
    [Header("Mounting Position")]
    [SerializeField] private Vector3 mountOffset = new Vector3(0.1f, -0.15f, 0.3f); // Right, down, forward from camera
    
    [Header("References")]
    [SerializeField] private Light flashlightLight;
    
    private bool isEnabled = false;
    private Transform attachmentPoint;
    private Camera targetCamera;
    private InputHandlers inputHandlers;
    
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
        
        GameObject projectionCamera = GameObject.Find("ProjectionPlaneCamera");
        if (projectionCamera != null)
        {
            targetCamera = projectionCamera.GetComponent<Camera>();
        }
        else
        {
            targetCamera = Camera.main;
        }
    }
    
    void Update()
    {
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
                        // Convert normalized coordinates to screen coordinates
                        Vector3 screenPoint = new Vector3(
                            screenPointNormal.x * Screen.width,
                            Screen.height - screenPointNormal.y * Screen.height,
                            10f // Distance from camera for ray calculation
                        );
                        
                        Ray ray = targetCamera.ScreenPointToRay(screenPoint);
                        
                        if (flashlightLight != null)
                        {
                            Vector3 lightPosition = targetCamera.transform.position + 
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
        
        UniversalAdditionalLightData lightData = flashlightObj.AddComponent<UniversalAdditionalLightData>();
        
        // Use inverse square falloff for realistic light attenuation
        lightData.usePipelineSettings = false;
        lightData.lightCookieSize = new Vector2(1f, 1f);
        lightData.lightCookieOffset = Vector2.zero;
    }
    
    public void AttachToTransform(Transform target)
    {
        attachmentPoint = target;
    }
    
    public void ToggleFlashlight()
    {
        SetFlashlightEnabled(!isEnabled);
    }
    
    public void SetFlashlightEnabled(bool enabled)
    {
        isEnabled = enabled;
        if (flashlightLight != null)
        {
            flashlightLight.enabled = enabled;
        }
    }
    
    public bool IsEnabled()
    {
        return isEnabled;
    }
    
    public void SetIntensity(float newIntensity)
    {
        intensity = newIntensity;
        if (flashlightLight != null)
        {
            flashlightLight.intensity = intensity;
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
}