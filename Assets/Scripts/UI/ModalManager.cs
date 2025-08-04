using UnityEngine;
using UnityEngine.InputSystem;

public class CanvasManager : MonoBehaviour
{
    public AppControls inputActions;
   
    [Header("Modal References")]
    public GameObject exitModal;
    
    private void Awake()
    {
        inputActions = new AppControls();
    }
    
    private void OnEnable()
    {
        inputActions.UI.Escape.performed += OnEscapePressed;
        inputActions.UI.Enable();
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