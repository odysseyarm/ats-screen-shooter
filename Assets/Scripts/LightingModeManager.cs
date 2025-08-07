using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Gaia;

public class LightingModeManager : MonoBehaviour
{
    public enum LightingMode
    {
        Normal,
        Dark
    }
    
    // Static variable to persist across scene loads
    private static LightingMode persistedMode = LightingMode.Normal;
    
    [Header("Settings")]
    [SerializeField] private LightingMode currentMode = LightingMode.Normal;
    [SerializeField] private float darkModeAmbientIntensity = 0.02f;
    [SerializeField] private float normalModeAmbientIntensity = 1f;
    [SerializeField] private Color darkModeAmbientColor = new Color(0.012f, 0.012f, 0.018f); // Near black with hint of blue
    [SerializeField] private Color normalModeAmbientColor = Color.white;
    [SerializeField] private float darkModeLightIntensityMultiplier = 0.025f; // Lights at 2.5% intensity in dark mode
    
    [Header("References")]
    [SerializeField] private FlashlightController flashlightController;
    
    [Header("Target Materials")]
    [SerializeField] private Material b27TargetMaterial; // Drag B27TargetMaterial here in Inspector
    
    private List<Light> sceneLights = new List<Light>();
    private Dictionary<Light, float> originalIntensities = new Dictionary<Light, float>();
    private Color originalAmbientLight;
    private float originalAmbientIntensity;
    private AmbientMode originalAmbientMode;
    
    // Gaia components
    private SceneProfile gaiaSceneProfile;
    private int originalLightingProfileIndex = -1;
    private int dayProfileIndex = -1;
    private int nightProfileIndex = -1;
    
    void Awake()
    {
        if (flashlightController == null)
        {
            Debug.LogError("LightingModeManager: FlashlightController not assigned! Please assign it in the Inspector.");
        }
        
        StoreOriginalLightingSettings();
        FindSceneLights();
    }
    
    void OnDestroy()
    {
        // Reset material to white when exiting play mode or destroying the manager
        if (b27TargetMaterial != null)
        {
            b27TargetMaterial.SetColor("_BaseColor", Color.white);
            b27TargetMaterial.SetColor("_Color", Color.white);
        }
    }
    
    
    void Start()
    {
        if (flashlightController != null)
        {
            GameObject projectionCamera = GameObject.Find("ProjectionPlaneCamera");
            if (projectionCamera != null)
            {
                flashlightController.AttachToTransform(projectionCamera.transform);
            }
            else
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    flashlightController.AttachToTransform(mainCam.transform);
                }
            }
        }
        
        CheckForGaiaSceneLighting();
        
        // Always attempt to restore the persisted lighting mode after scene load
        // Even if modes match, we need to reapply settings for the new scene
        StartCoroutine(RestorePersistedMode());
    }
    
    private void StoreOriginalLightingSettings()
    {
        originalAmbientLight = RenderSettings.ambientLight;
        originalAmbientIntensity = RenderSettings.ambientIntensity;
        originalAmbientMode = RenderSettings.ambientMode;
        normalModeAmbientColor = originalAmbientLight;
        normalModeAmbientIntensity = originalAmbientIntensity;
    }
    
    private void CheckForGaiaSceneLighting()
    {
        GaiaGlobal gaiaGlobal = GaiaGlobal.Instance;
        if (gaiaGlobal != null && gaiaGlobal.SceneProfile != null)
        {
            gaiaSceneProfile = gaiaGlobal.SceneProfile;
            
            if (gaiaSceneProfile.m_lightingProfiles == null || gaiaSceneProfile.m_lightingProfiles.Count == 0)
            {
                
                var lightingProfiles = UnityEngine.Resources.FindObjectsOfTypeAll<GaiaLightingProfile>();
                GaiaLightingProfile lightingProfile = null;
                
                foreach (var profile in lightingProfiles)
                {
                    if (profile.name.Contains("Gaia Lighting System Profile"))
                    {
                        lightingProfile = profile;
                        break;
                    }
                }
                
                if (lightingProfile == null && lightingProfiles.Length > 0)
                {
                    lightingProfile = lightingProfiles[0];
                }
                
                #if UNITY_EDITOR
                if (lightingProfile == null)
                {
                    lightingProfile = UnityEditor.AssetDatabase.LoadAssetAtPath<GaiaLightingProfile>("Assets/Procedural Worlds/Gaia/Lighting/Gaia Lighting System Profile.asset");
                }
                #endif
                
                if (lightingProfile != null && lightingProfile.m_lightingProfiles != null)
                {
                    gaiaSceneProfile.m_lightingProfiles = lightingProfile.m_lightingProfiles;
                    gaiaSceneProfile.m_masterSkyboxMaterial = lightingProfile.m_masterSkyboxMaterial;
                }
            }
            
            originalLightingProfileIndex = gaiaSceneProfile.m_selectedLightingProfileValuesIndex;
            
            dayProfileIndex = -1;
            nightProfileIndex = -1;
            
            for (int i = 0; i < gaiaSceneProfile.m_lightingProfiles.Count; i++)
            {
                var profile = gaiaSceneProfile.m_lightingProfiles[i];
                if (profile == null) continue;
                
                string profileName = profile.m_typeOfLighting;
                if (!string.IsNullOrEmpty(profileName))
                {
                    string profileNameLower = profileName.ToLower();
                    if (profileNameLower.Contains("day") || profileNameLower.Contains("noon"))
                    {
                        dayProfileIndex = i;
                    }
                    else if (profileNameLower.Contains("night") || profileNameLower.Contains("midnight"))
                    {
                        nightProfileIndex = i;
                    }
                }
            }
        }
    }
    
    private void FindSceneLights()
    {
        sceneLights.Clear();
        originalIntensities.Clear();
        
        Light[] allLights = FindObjectsOfType<Light>();
        foreach (Light light in allLights)
        {
            if (light.name.Contains("Area Light") || 
                light.type == LightType.Spot || 
                light.type == LightType.Point || 
                light.type == LightType.Directional)
            {
                if (!light.name.Contains("Flashlight"))
                {
                    sceneLights.Add(light);
                    originalIntensities[light] = light.intensity;
                }
            }
        }
        
    }
    
    public void ToggleLightingMode()
    {
        var newMode = currentMode == LightingMode.Normal ? LightingMode.Dark : LightingMode.Normal;
        SetLightingMode(newMode);
    }
    
    public void SetLightingMode(LightingMode mode)
    {
        Debug.Log($"LightingModeManager: Dark Mode {(mode == LightingMode.Dark ? "ON" : "OFF")}");
        
        currentMode = mode;
        persistedMode = mode; 
        
        switch (mode)
        {
            case LightingMode.Normal:
                SetNormalLighting();
                break;
            case LightingMode.Dark:
                SetDarkLighting();
                break;
        }
        
        // This is necessary b/c URP is not handling extremely bright lights applied to the target surface well, so we make the surface darker when dark mode is enabled so it looks "correct" when illuminated.
        UpdateTargetMaterial(mode);
    }
    
    private System.Collections.IEnumerator RestorePersistedMode()
    {
        yield return new WaitForEndOfFrame();
        
        int maxAttempts = 50;
        int attempts = 0;
        
        while (attempts < maxAttempts)
        {
            GaiaGlobal gaiaGlobal = GaiaGlobal.Instance;
            if (gaiaGlobal != null && gaiaGlobal.SceneProfile != null)
            {
                yield return new WaitForSeconds(0.5f);
                break;
            }
            attempts++;
            yield return new WaitForSeconds(0.1f);
        }
        
        if (attempts >= maxAttempts)
        {
            Debug.LogWarning($"LightingModeManager: Gaia initialization timeout after {maxAttempts} attempts");
        }
        
        CheckForGaiaSceneLighting();
        
        // Wait for Gaia to fully apply its default profile first
        yield return new WaitForSeconds(0.5f);
        SetLightingMode(persistedMode);
        
        if (persistedMode == LightingMode.Dark && nightProfileIndex >= 0)
        {
            yield return new WaitForSeconds(0.5f);
            
            for (int i = 0; i < 3; i++)
            {
                ForceGaiaProfileReload(nightProfileIndex);
                
                yield return new WaitForSeconds(0.2f);
                
                ApplySkyboxFromProfile(nightProfileIndex);
                
                if (RenderSettings.skybox != null)
                {
                    Material skybox = RenderSettings.skybox;
                    RenderSettings.skybox = null;
                    yield return null;
                    RenderSettings.skybox = skybox;
                    
                    if (skybox.HasProperty("_Exposure"))
                    {
                        float currentExposure = skybox.GetFloat("_Exposure");
                        if (currentExposure != gaiaSceneProfile.m_lightingProfiles[nightProfileIndex].m_skyboxExposure)
                        {
                            ApplySkyboxFromProfile(nightProfileIndex);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
    
    private void SetNormalLighting()
    {
        foreach (Light light in sceneLights)
        {
            if (light != null && originalIntensities.ContainsKey(light))
            {
                light.intensity = originalIntensities[light];
                light.enabled = true;
            }
        }
        
        RenderSettings.ambientLight = normalModeAmbientColor;
        RenderSettings.ambientIntensity = normalModeAmbientIntensity;
        
        if (gaiaSceneProfile != null && dayProfileIndex >= 0)
        {
            int targetProfile = (originalLightingProfileIndex != nightProfileIndex) ? originalLightingProfileIndex : dayProfileIndex;
            gaiaSceneProfile.m_selectedLightingProfileValuesIndex = targetProfile;
            
            GaiaGlobal gaiaGlobal = GaiaGlobal.Instance;
            if (gaiaGlobal != null)
            {
                gaiaGlobal.UpdateGaiaTimeOfDay(false);
            }
            
            ApplySkyboxFromProfile(targetProfile);
        }
        
        if (flashlightController != null)
        {
            flashlightController.SetFlashlightEnabled(false);
        }
    }
    
    private void SetDarkLighting()
    {
        foreach (Light light in sceneLights)
        {
            if (light != null && originalIntensities.ContainsKey(light))
            {
                light.intensity = originalIntensities[light] * darkModeLightIntensityMultiplier;
                light.enabled = true;
            }
        }
        
        RenderSettings.ambientLight = darkModeAmbientColor;
        RenderSettings.ambientIntensity = darkModeAmbientIntensity;
        
        if (gaiaSceneProfile != null && nightProfileIndex >= 0)
        {
            GaiaGlobal gaiaGlobal = GaiaGlobal.Instance;
            if (gaiaGlobal != null && gaiaGlobal.SceneProfile != null)
            {
                if (gaiaSceneProfile != gaiaGlobal.SceneProfile)
                {
                    CheckForGaiaSceneLighting();
                }
            }
            
            if (nightProfileIndex >= 0 && gaiaSceneProfile != null)
            {
                GaiaLightingProfileValues nightProfile = gaiaSceneProfile.m_lightingProfiles[nightProfileIndex];
                
                gaiaSceneProfile.m_selectedLightingProfileValuesIndex = nightProfileIndex;
                
                if (gaiaGlobal != null)
                {
                    gaiaGlobal.UpdateGaiaTimeOfDay(false);
                    
                    if (gaiaSceneProfile.m_gaiaTimeOfDay != null)
                    {
                        gaiaSceneProfile.m_gaiaTimeOfDay.m_todHour = 1;
                        gaiaSceneProfile.m_gaiaTimeOfDay.m_todMinutes = 0;
                        gaiaGlobal.UpdateGaiaTimeOfDay(false);
                    }
                }
                
                ForceGaiaProfileReload(nightProfileIndex);
                
                if (nightProfile != null)
                {
                    ApplySkyboxFromProfile(nightProfileIndex);
                    
                    if (RenderSettings.skybox != null)
                    {
                        Material skybox = RenderSettings.skybox;
                        RenderSettings.skybox = null;
                        RenderSettings.skybox = skybox;
                    }
                }
            }
        }
        
        RenderSettings.ambientLight = darkModeAmbientColor;
        RenderSettings.ambientIntensity = darkModeAmbientIntensity;
        RenderSettings.ambientSkyColor = darkModeAmbientColor * 1.05f; // Barely any sky brightness
        RenderSettings.ambientEquatorColor = darkModeAmbientColor * 1.1f; // Minimal horizon glow
        RenderSettings.ambientGroundColor = darkModeAmbientColor * 0.1f; // Almost no ground reflection;
        
        if (flashlightController != null)
        {
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool enableFlashlight = (currentSceneName == "OutdoorRange");
            if (enableFlashlight)
            {
                Debug.Log("LightingModeManager: Flashlight ON");
            }
            flashlightController.SetFlashlightEnabled(enableFlashlight);
        }
    }
    
    public LightingMode GetCurrentMode()
    {
        return currentMode;
    }
    
    public FlashlightController GetFlashlightController()
    {
        return flashlightController;
    }
    
    private void ForceGaiaProfileReload(int profileIndex)
    {
        if (gaiaSceneProfile == null || profileIndex < 0)
        {
            return;
        }
            
        int tempIndex = (profileIndex == 0) ? 1 : 0;
        if (tempIndex < gaiaSceneProfile.m_lightingProfiles.Count)
        {
            gaiaSceneProfile.m_selectedLightingProfileValuesIndex = tempIndex;
            GaiaGlobal gaiaGlobal = GaiaGlobal.Instance;
            if (gaiaGlobal != null)
            {
                gaiaGlobal.UpdateGaiaTimeOfDay(false);
            }
        }
        
        gaiaSceneProfile.m_selectedLightingProfileValuesIndex = profileIndex;
        if (GaiaGlobal.Instance != null)
        {
            GaiaGlobal.Instance.UpdateGaiaTimeOfDay(false);
        }
    }
    
    private void ApplySkyboxFromProfile(int profileIndex)
    {
        if (gaiaSceneProfile == null || profileIndex < 0 || profileIndex >= gaiaSceneProfile.m_lightingProfiles.Count)
        {
            return;
        }
            
        GaiaLightingProfileValues profile = gaiaSceneProfile.m_lightingProfiles[profileIndex];
        if (profile == null)
            return;
            
        Material skyboxMat = RenderSettings.skybox;
        if (skyboxMat != null)
        {
            // DON'T create a new material with different shader - just modify the existing one
            // This preserves the PWS/Skybox/PW_HDRI shader that Gaia uses
            
            // Check for PWS shader properties (Gaia's custom shader)
            if (skyboxMat.HasProperty("_HDRITint"))
            {
                skyboxMat.SetColor("_HDRITint", profile.m_skyboxTint);
            }
            else if (skyboxMat.HasProperty("_Tint"))
            {
                skyboxMat.SetColor("_Tint", profile.m_skyboxTint);
            }
            
            // PWS shader uses _HDRIExposure instead of _Exposure
            if (skyboxMat.HasProperty("_HDRIExposure"))
            {
                skyboxMat.SetFloat("_HDRIExposure", profile.m_skyboxExposure);
            }
            else if (skyboxMat.HasProperty("_Exposure"))
            {
                skyboxMat.SetFloat("_Exposure", profile.m_skyboxExposure);
            }
                
            if (skyboxMat.HasProperty("_Rotation"))
                skyboxMat.SetFloat("_Rotation", profile.m_skyboxRotationOffset);
                
            // For HDRI skyboxes - PWS shader uses _MainTex
            if (profile.m_skyboxHDRI != null)
            {
                if (skyboxMat.HasProperty("_MainTex"))
                {
                    skyboxMat.SetTexture("_MainTex", profile.m_skyboxHDRI);
                }
                else if (skyboxMat.HasProperty("_Tex"))
                {
                    skyboxMat.SetTexture("_Tex", profile.m_skyboxHDRI);
                }
            }
                
            // For procedural skyboxes
            if (skyboxMat.HasProperty("_SunSize"))
                skyboxMat.SetFloat("_SunSize", profile.m_sunSize);
                
            if (skyboxMat.HasProperty("_AtmosphereThickness"))
            {
                skyboxMat.SetFloat("_AtmosphereThickness", profile.m_atmosphereThickness);
            }
                
            if (skyboxMat.HasProperty("_SkyTint"))
                skyboxMat.SetColor("_SkyTint", profile.m_skyboxTint);
                
            if (skyboxMat.HasProperty("_GroundColor"))
                skyboxMat.SetColor("_GroundColor", profile.m_groundColor);
            
            skyboxMat.SetFloat("_UpdateTrigger", Random.Range(0f, 1f));
            
            DynamicGI.UpdateEnvironment();
        }
        
        RenderSettings.ambientMode = profile.m_ambientMode;
        RenderSettings.ambientIntensity = profile.m_ambientIntensity;
        RenderSettings.ambientSkyColor = profile.m_skyAmbient;
        RenderSettings.ambientEquatorColor = profile.m_equatorAmbient;
        RenderSettings.ambientGroundColor = profile.m_groundAmbient;
    }
    
    private void UpdateTargetMaterial(LightingMode mode)
    {
        if (b27TargetMaterial == null)
        {
            return;
        }
        
        if (mode == LightingMode.Dark)
        {
            b27TargetMaterial.SetColor("_BaseColor", new Color(0.65f, 0.65f, 0.65f, 1f));
            b27TargetMaterial.SetColor("_Color", new Color(0.65f, 0.65f, 0.65f, 1f));
        }
        else
        {
            b27TargetMaterial.SetColor("_BaseColor", Color.white);
            b27TargetMaterial.SetColor("_Color", Color.white);
        }
    }
}