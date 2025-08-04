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
        
        if (currentSceneName == "IndoorRange")
        {
            SceneManager.LoadScene("OutdoorRange");
        }
        else if (currentSceneName == "OutdoorRange")
        {
            SceneManager.LoadScene("IndoorRange");
        }
    }

    public void SwitchScene()
    {
        OnSwitchScene(default);
    }
}