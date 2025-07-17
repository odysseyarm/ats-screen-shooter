using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ExitModalManager : MonoBehaviour
{
    [Header("Button References")]
    public Button confirmExitButton;
    public Button cancelExitButton;
    
    [Header("Manager References")]
    public AppManager appManager;
    
    private UserInputActions inputActions;
    
    void Awake()
    {
        inputActions = new UserInputActions();
        
        if (confirmExitButton == null || cancelExitButton == null)
        {
            Debug.LogError("ExitModalManager: Button references not set!");
        }
        
        if (appManager == null)
        {
            appManager = FindObjectOfType<AppManager>();
            if (appManager == null)
            {
                Debug.LogError("ExitModalManager: AppManager not found!");
            }
        }
    }
    
    void OnEnable()
    {
        inputActions.ExitModal.Enable();
        inputActions.Gameplay.Disable();
        
        inputActions.ExitModal.Exit.performed += OnExitPressed;
        inputActions.ExitModal.Back.performed += OnBackPressed;
        
        if (confirmExitButton != null)
            confirmExitButton.onClick.AddListener(TriggerExit);
            
        if (cancelExitButton != null)
            cancelExitButton.onClick.AddListener(TriggerBack);
    }
    
    void OnDisable()
    {
        inputActions.ExitModal.Exit.performed -= OnExitPressed;
        inputActions.ExitModal.Back.performed -= OnBackPressed;
        inputActions.ExitModal.Disable();
        inputActions.Gameplay.Enable();
        
        if (confirmExitButton != null)
            confirmExitButton.onClick.RemoveListener(TriggerExit);
            
        if (cancelExitButton != null)
            cancelExitButton.onClick.RemoveListener(TriggerBack);
    }
    
    private void OnExitPressed(InputAction.CallbackContext context)
    {
        TriggerExit();
    }
    
    private void OnBackPressed(InputAction.CallbackContext context)
    {
        TriggerBack();
    }
    
    public void TriggerExit()
    {
        Debug.Log("Exit triggered - Quitting application");
        if (appManager != null)
        {
            appManager.QuitApplication();
        }
    }
    
    public void TriggerBack()
    {
        Debug.Log("Back triggered - Closing modal");
        CloseModal();
    }
    
    private void CloseModal()
    {
        gameObject.SetActive(false);
    }
}