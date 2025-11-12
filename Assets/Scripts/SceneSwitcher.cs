using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    private AppControls appControls;

    public void Initialize(AppControls controls)
    {
        appControls = controls;
        if (appControls != null)
        {
            appControls.UI.SwitchScene.performed += OnSwitchScene;
        }
    }

    private void OnDestroy()
    {
        if (appControls != null)
        {
            appControls.UI.SwitchScene.performed -= OnSwitchScene;
            appControls.UI.Disable();
        }
    }

    private void OnSwitchScene(InputAction.CallbackContext context)
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"[{System.DateTime.Now:HH:mm:ss.fff}] SceneSwitcher: OnSwitchScene() triggered - Current scene: {currentSceneName}");
        
        if (currentSceneName == "IndoorRange")
        {
            Debug.Log($"[{System.DateTime.Now:HH:mm:ss.fff}] SceneSwitcher: Loading OutdoorRange scene");
            SceneManager.LoadScene("OutdoorRange");
        }
        else if (currentSceneName == "OutdoorRange")
        {
            Debug.Log($"[{System.DateTime.Now:HH:mm:ss.fff}] SceneSwitcher: Loading IndoorRange scene");
            SceneManager.LoadScene("IndoorRange");
        }
        else
        {
            Debug.LogWarning($"[{System.DateTime.Now:HH:mm:ss.fff}] SceneSwitcher: Unknown scene '{currentSceneName}' - no switch performed");
        }
    }

    public void SwitchScene()
    {
        Debug.Log($"[{System.DateTime.Now:HH:mm:ss.fff}] SceneSwitcher: SwitchScene() called programmatically");
        OnSwitchScene(default);
    }
}