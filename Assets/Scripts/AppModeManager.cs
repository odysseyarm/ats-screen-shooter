using UnityEngine;

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
            QualificationModeMenu.SetActive(mode == TargetMode.Qualification);
        
        if (ReactiveModeMenu != null)
            ReactiveModeMenu.SetActive(mode == TargetMode.Reactive);
        
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
}