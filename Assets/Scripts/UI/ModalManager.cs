using UnityEngine;
using UnityEngine.InputSystem;

public class CanvasManager : MonoBehaviour
{
    public UserInputActions inputActions;
   
    [Header("Modal References")]
    public GameObject exitModal;
    
    private void Awake()
    {
        inputActions = new UserInputActions();
    }
    
    private void OnEnable()
    {
        inputActions.Gameplay.Escape.performed += OnEscapePressed;
        inputActions.Gameplay.Enable();
    }
    
    private void OnDisable()
    {
        inputActions.Gameplay.Escape.performed -= OnEscapePressed;
        inputActions.Gameplay.Disable();
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