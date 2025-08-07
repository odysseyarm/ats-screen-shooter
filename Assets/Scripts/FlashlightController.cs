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
                        Vector3 screenPoint = new Vector3(
                            screenPointNormal.x * Screen.width,
                            Screen.height - screenPointNormal.y * Screen.height,
                            10f
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
                            
                            // Use constant intensity - no dynamic adjustment
                            flashlightLight.intensity = intensity;
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
        attachmentPoint = target;
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
}