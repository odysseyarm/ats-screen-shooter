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
        Debug.Log("Opening Exit modal");
        if (exitModal != null)
        {
            exitModal.SetActive(true);
        }
    }
    
    private void OnEscapePressed(InputAction.CallbackContext context)
    {
        OpenExitModal();
    }
    
}