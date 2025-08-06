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
    [SerializeField] private float darkModeAmbientIntensity = 0.01f; // Much darker
    [SerializeField] private float normalModeAmbientIntensity = 1f;
    [SerializeField] private Color darkModeAmbientColor = new Color(0.01f, 0.01f, 0.015f); // Nearly black with slight blue tint
    [SerializeField] private Color normalModeAmbientColor = Color.white;
    
    [Header("References")]
    [SerializeField] private FlashlightController flashlightController;
    
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
            GameObject flashlightObj = new GameObject("FlashlightSystem");
            flashlightController = flashlightObj.AddComponent<FlashlightController>();
        }
        
        StoreOriginalLightingSettings();
        FindSceneLights();
    }
    
    
    void Start()
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
        
        // Check for Gaia Scene Lighting
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
        // Try to get Gaia Global instance which contains the scene profile
        GaiaGlobal gaiaGlobal = GaiaGlobal.Instance;
        if (gaiaGlobal != null && gaiaGlobal.SceneProfile != null)
        {
            gaiaSceneProfile = gaiaGlobal.SceneProfile;
            
            // If lighting profiles list is empty, try to find and load the lighting profile
            if (gaiaSceneProfile.m_lightingProfiles == null || gaiaSceneProfile.m_lightingProfiles.Count == 0)
            {
                
                // Try to find the Gaia Lighting System Profile asset
                var lightingProfiles = UnityEngine.Resources.FindObjectsOfTypeAll<GaiaLightingProfile>();
                GaiaLightingProfile lightingProfile = null;
                
                // Look for the main Gaia Lighting System Profile
                foreach (var profile in lightingProfiles)
                {
                    if (profile.name.Contains("Gaia Lighting System Profile"))
                    {
                        lightingProfile = profile;
                        break;
                    }
                }
                
                // If not found by name, just use the first one found
                if (lightingProfile == null && lightingProfiles.Length > 0)
                {
                    lightingProfile = lightingProfiles[0];
                }
                
                #if UNITY_EDITOR
                // In editor, try loading from asset database
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
            
            // Reset indices
            dayProfileIndex = -1;
            nightProfileIndex = -1;
            
            // Find Day and Night profile indices by name
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
        SetLightingMode(currentMode == LightingMode.Normal ? LightingMode.Dark : LightingMode.Normal);
    }
    
    public void SetLightingMode(LightingMode mode)
    {
        currentMode = mode;
        persistedMode = mode;  // Save to static variable for persistence
        
        switch (mode)
        {
            case LightingMode.Normal:
                SetNormalLighting();
                break;
            case LightingMode.Dark:
                SetDarkLighting();
                break;
        }
    }
    
    private System.Collections.IEnumerator RestorePersistedMode()
    {
        // Wait for end of frame to ensure scene is loaded
        yield return new WaitForEndOfFrame();
        
        // Wait for Gaia to be fully initialized
        int maxAttempts = 50; // 5 seconds max wait
        int attempts = 0;
        
        while (attempts < maxAttempts)
        {
            GaiaGlobal gaiaGlobal = GaiaGlobal.Instance;
            if (gaiaGlobal != null && gaiaGlobal.SceneProfile != null)
            {
                // Gaia is ready, wait a bit more for skybox initialization
                yield return new WaitForSeconds(0.5f);
                break;
            }
            attempts++;
            yield return new WaitForSeconds(0.1f);
        }
        
        // Force re-check for Gaia since it might have initialized after our Start
        CheckForGaiaSceneLighting();
        
        // Wait for Gaia to fully apply its default profile first
        yield return new WaitForSeconds(0.5f);
        
        // Now restore the persisted mode
        SetLightingMode(persistedMode);
        
        // Force a complete skybox refresh if in dark mode
        if (persistedMode == LightingMode.Dark && nightProfileIndex >= 0)
        {
            yield return new WaitForSeconds(0.5f);
            
            // Force multiple update attempts to ensure skybox is applied
            for (int i = 0; i < 3; i++)
            {
                // Force Gaia to fully reload the profile
                ForceGaiaProfileReload(nightProfileIndex);
                
                yield return new WaitForSeconds(0.2f);
                
                // Force apply skybox settings again
                ApplySkyboxFromProfile(nightProfileIndex);
                
                // Check if skybox was applied correctly
                if (RenderSettings.skybox != null)
                {
                    Material skybox = RenderSettings.skybox;
                    // Force skybox to update by temporarily setting to null
                    RenderSettings.skybox = null;
                    yield return null;
                    RenderSettings.skybox = skybox;
                    
                    // Force the skybox material to refresh its properties
                    if (skybox.HasProperty("_Exposure"))
                    {
                        float currentExposure = skybox.GetFloat("_Exposure");
                        if (currentExposure != gaiaSceneProfile.m_lightingProfiles[nightProfileIndex].m_skyboxExposure)
                        {
                            // Try applying again if it didn't stick
                            ApplySkyboxFromProfile(nightProfileIndex);
                        }
                        else
                        {
                            // Success, break out of retry loop
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
        
        // Restore Gaia lighting profile to original or Day
        if (gaiaSceneProfile != null && dayProfileIndex >= 0)
        {
            // Use original profile if it wasn't night, otherwise use day
            int targetProfile = (originalLightingProfileIndex != nightProfileIndex) ? originalLightingProfileIndex : dayProfileIndex;
            gaiaSceneProfile.m_selectedLightingProfileValuesIndex = targetProfile;
            
            // Force update of Gaia lighting
            GaiaGlobal gaiaGlobal = GaiaGlobal.Instance;
            if (gaiaGlobal != null)
            {
                gaiaGlobal.UpdateGaiaTimeOfDay(false);
            }
            
            // Manually update skybox if needed
            ApplySkyboxFromProfile(targetProfile);
            
        }
        
        // Always disable flashlight in normal mode
        if (flashlightController != null)
        {
            flashlightController.SetFlashlightEnabled(false);
        }
    }
    
    private void SetDarkLighting()
    {
        foreach (Light light in sceneLights)
        {
            if (light != null)
            {
                light.intensity = 0f;
                light.enabled = false;
            }
        }
        
        RenderSettings.ambientLight = darkModeAmbientColor;
        RenderSettings.ambientIntensity = darkModeAmbientIntensity;
        
        // Set Gaia lighting profile to Night
        if (gaiaSceneProfile != null && nightProfileIndex >= 0)
        {
            // First check if we need to refresh the Gaia reference
            GaiaGlobal gaiaGlobal = GaiaGlobal.Instance;
            if (gaiaGlobal != null && gaiaGlobal.SceneProfile != null)
            {
                // Re-check for Gaia profiles if needed
                if (gaiaSceneProfile != gaiaGlobal.SceneProfile)
                {
                    CheckForGaiaSceneLighting();
                }
            }
            
            if (nightProfileIndex >= 0 && gaiaSceneProfile != null)
            {
                // Get the night profile
                GaiaLightingProfileValues nightProfile = gaiaSceneProfile.m_lightingProfiles[nightProfileIndex];
                
                gaiaSceneProfile.m_selectedLightingProfileValuesIndex = nightProfileIndex;
                
                // Force update of Gaia lighting
                if (gaiaGlobal != null)
                {
                    gaiaGlobal.UpdateGaiaTimeOfDay(false);
                    
                    // Try to force time of day update if Gaia supports it
                    if (gaiaSceneProfile.m_gaiaTimeOfDay != null)
                    {
                        // Force night time
                        gaiaSceneProfile.m_gaiaTimeOfDay.m_todHour = 1;
                        gaiaSceneProfile.m_gaiaTimeOfDay.m_todMinutes = 0;
                        gaiaGlobal.UpdateGaiaTimeOfDay(false);
                    }
                }
                
                // Force reload the profile to ensure it's applied
                ForceGaiaProfileReload(nightProfileIndex);
                
                // Apply skybox properties directly without creating new materials
                if (nightProfile != null)
                {
                    // Apply skybox properties to the existing material
                    ApplySkyboxFromProfile(nightProfileIndex);
                    
                    // Force Unity to refresh the skybox by triggering a small change
                    if (RenderSettings.skybox != null)
                    {
                        // Save reference and force update
                        Material skybox = RenderSettings.skybox;
                        RenderSettings.skybox = null;
                        RenderSettings.skybox = skybox;
                    }
                }
            }
        }
        
        // Force even darker settings after Gaia profile is applied
        RenderSettings.ambientLight = darkModeAmbientColor;
        RenderSettings.ambientIntensity = darkModeAmbientIntensity;
        RenderSettings.ambientSkyColor = darkModeAmbientColor;
        RenderSettings.ambientEquatorColor = darkModeAmbientColor * 0.5f;
        RenderSettings.ambientGroundColor = Color.black;
        
        // Make fog darker too
        RenderSettings.fogColor = new Color(0.01f, 0.01f, 0.02f);
        
        // Only enable flashlight in OutdoorRange scene
        if (flashlightController != null)
        {
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool enableFlashlight = (currentSceneName == "OutdoorRange");
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
            return;
            
        // Force Gaia to reload by switching to a different profile and back
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
        
        // Now switch to the target profile
        gaiaSceneProfile.m_selectedLightingProfileValuesIndex = profileIndex;
        if (GaiaGlobal.Instance != null)
        {
            GaiaGlobal.Instance.UpdateGaiaTimeOfDay(false);
        }
    }
    
    private void ApplySkyboxFromProfile(int profileIndex)
    {
        if (gaiaSceneProfile == null || profileIndex < 0 || profileIndex >= gaiaSceneProfile.m_lightingProfiles.Count)
            return;
            
        GaiaLightingProfileValues profile = gaiaSceneProfile.m_lightingProfiles[profileIndex];
        if (profile == null)
            return;
            
        // Apply skybox settings directly to RenderSettings
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
            
            // Force skybox to update
            skyboxMat.SetFloat("_UpdateTrigger", Random.Range(0f, 1f));
            
            // Force dynamic GI update
            DynamicGI.UpdateEnvironment();
        }
        
        // Also update ambient settings from the profile
        RenderSettings.ambientMode = profile.m_ambientMode;
        RenderSettings.ambientIntensity = profile.m_ambientIntensity;
        RenderSettings.ambientSkyColor = profile.m_skyAmbient;
        RenderSettings.ambientEquatorColor = profile.m_equatorAmbient;
        RenderSettings.ambientGroundColor = profile.m_groundAmbient;
        
        // Update fog settings
        RenderSettings.fogColor = profile.m_fogColor;
        RenderSettings.fogMode = profile.m_fogMode;
        if (profile.m_fogMode == FogMode.Linear)
        {
            RenderSettings.fogStartDistance = profile.m_fogStartDistance;
            RenderSettings.fogEndDistance = profile.m_fogEndDistance;
        }
        else
        {
            RenderSettings.fogDensity = profile.m_fogDensity;
        }
    }
}