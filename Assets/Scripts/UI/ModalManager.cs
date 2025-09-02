using UnityEngine;
using UnityEngine.InputSystem;

public class CanvasManager : MonoBehaviour
{
    public AppControls inputActions;
   
    [Header("Modal References")]
    public GameObject exitModal;
    
    [Header("Menu References")]
    public GameObject distanceMenu;
    
    private void Awake()
    {
        inputActions = new AppControls();
        
        // Validate that the exit modal is assigned
        if (exitModal == null)
        {
            Debug.LogWarning("CanvasManager: Exit Modal is not assigned. Attempting to find it...");
            exitModal = transform.Find("ExitModal")?.gameObject;
            if (exitModal == null)
            {
                // Try to find it anywhere in children
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "ExitModal")
                    {
                        exitModal = child.gameObject;
                        Debug.Log("CanvasManager: Found ExitModal in children");
                        break;
                    }
                }
            }
        }
    }
    
    private void OnEnable()
    {
        inputActions.UI.Escape.performed += OnEscapePressed;
        inputActions.UI.Enable();
        
        // Show distance menu if we're in the OutdoorRange scene
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "OutdoorRange" && distanceMenu != null)
        {
            distanceMenu.SetActive(true);
        }
    }
    
    private void OnDisable()
    {
        inputActions.UI.Escape.performed -= OnEscapePressed;
        inputActions.UI.Disable();
    }
    
    private void OpenExitModal()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"Opening Exit modal in scene: {sceneName}");
        
        if (exitModal != null)
        {
            exitModal.SetActive(true);
            Debug.Log("Exit modal successfully activated");
        }
        else
        {
            Debug.LogError($"CanvasManager: Exit Modal is null in scene {sceneName}! Cannot open exit modal.");
            Debug.LogError("Please ensure the Canvas prefab in this scene has the ExitModal child object properly configured.");
        }
    }
    
    private void OnEscapePressed(InputAction.CallbackContext context)
    {
        OpenExitModal();
    }
    
}