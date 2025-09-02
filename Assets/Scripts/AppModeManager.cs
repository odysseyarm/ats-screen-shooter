using UnityEngine;
using UnityEngine.InputSystem;

public enum TargetMode
{
    None,
    Qualification,
    Reactive
}

public class AppModeManager : MonoBehaviour
{
    [Header("Mode Objects")]
    public GameObject QualificationMode;
    public GameObject ReactiveMode;
    
    [Header("UI Menus")]
    public GameObject TargetModeMenu;
    public GameObject ReactiveModeMenu;
    public GameObject QualificationModeMenu;
    
    private TargetMode currentMode = TargetMode.None;
    private TargetModeMenuManager targetModeMenuManager;
    private InputAction toggleModeAction;
    
    void Awake()
    {
        // Create and configure the input action for X key
        toggleModeAction = new InputAction("ToggleMode", binding: "<Keyboard>/x");
        toggleModeAction.performed += OnToggleModeAction;
    }
    
    void OnEnable()
    {
        toggleModeAction?.Enable();
    }
    
    void OnDisable()
    {
        toggleModeAction?.Disable();
    }
    
    void OnDestroy()
    {
        if (toggleModeAction != null)
        {
            toggleModeAction.performed -= OnToggleModeAction;
            toggleModeAction.Dispose();
        }
    }
    
    void Start()
    {
        if (TargetModeMenu != null)
        {
            targetModeMenuManager = TargetModeMenu.GetComponent<TargetModeMenuManager>();
            if (targetModeMenuManager == null)
            {
                Debug.LogError("TargetModeMenu does not have a TargetModeMenuManager component!");
            }
        }
        else
        {
            Debug.LogError("TargetModeMenu is not assigned in AppModeManager!");
        }
        
        SetMode(TargetMode.None);
    }
    
    private void OnToggleModeAction(InputAction.CallbackContext context)
    {
        ToggleMode();
    }
    
    public void SetMode(TargetMode mode)
    {
        currentMode = mode;
        
        if (QualificationMode != null)
            QualificationMode.SetActive(mode == TargetMode.Qualification);
        else if (mode == TargetMode.Qualification)
            Debug.LogWarning("QualificationMode GameObject is not assigned!");
        
        if (ReactiveMode != null)
            ReactiveMode.SetActive(mode == TargetMode.Reactive);
        else if (mode == TargetMode.Reactive)
            Debug.LogWarning("ReactiveMode GameObject is not assigned!");
        
        if (QualificationModeMenu != null)
        {
            QualificationModeMenu.SetActive(mode == TargetMode.Qualification);
            
            // When entering Qualification Mode, ensure Responsive Distance is turned off
            if (mode == TargetMode.Qualification)
            {
                // Find the DistanceMenuManager and disable responsive distance
                DistanceMenuManager distanceMenuManager = QualificationModeMenu.GetComponentInChildren<DistanceMenuManager>();
                if (distanceMenuManager != null && distanceMenuManager.responsiveDistanceToggle != null)
                {
                    distanceMenuManager.responsiveDistanceToggle.isOn = false;
                    Debug.Log("AppModeManager: Disabled Responsive Distance for Qualification Mode");
                }
                
                // Also directly disable it on the QualificationTargetController if it exists
                QualificationTargetController targetController = FindObjectOfType<QualificationTargetController>();
                if (targetController != null)
                {
                    targetController.SetResponsiveDistanceEnabled(false);
                }
            }
        }
        
        if (ReactiveModeMenu != null)
            ReactiveModeMenu.SetActive(mode == TargetMode.Reactive);
        
        // Update button highlights in the target mode menu
        if (targetModeMenuManager != null)
        {
            targetModeMenuManager.UpdateButtonHighlight(mode);
        }
        
        Debug.Log($"App Mode changed to: {mode}");
    }
    
    public void OnQualificationModeSelected()
    {
        SetMode(TargetMode.Qualification);
    }
    
    public void OnReactiveModeSelected()
    {
        SetMode(TargetMode.Reactive);
    }
    
    public TargetMode GetCurrentMode()
    {
        return currentMode;
    }
    
    public void ToggleMode()
    {
        // Toggle between Qualification and Reactive modes
        switch (currentMode)
        {
            case TargetMode.None:
            case TargetMode.Reactive:
                SetMode(TargetMode.Qualification);
                Debug.Log("X key pressed: Switching to Qualification Mode");
                break;
            case TargetMode.Qualification:
                SetMode(TargetMode.Reactive);
                Debug.Log("X key pressed: Switching to Reactive Mode");
                break;
        }
    }
}