using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class DistanceMenuManager : MonoBehaviour
{
    [Header("Button References")]
    public Button threeYardButton;
    public Button sevenYardButton;
    public Button fifteenYardButton;
    
    [Header("Text References (Optional)")]
    public TextMeshProUGUI threeYardText;
    public TextMeshProUGUI sevenYardText;
    public TextMeshProUGUI fifteenYardText;
    
    [Header("Responsive Distance UI")]
    public Toggle responsiveDistanceToggle;
    public TextMeshProUGUI responsiveDistanceLabel;
    
    [Header("Target Reference")]
    public GameObject b27Target;
    private QualificationTargetController targetController;
    
    [Header("Distance Settings")]
    [SerializeField] private float threeYardDistance = 5f;
    [SerializeField] private float sevenYardDistance = 7f;
    [SerializeField] private float fifteenYardDistance = 15f;
    
    private AppControls inputActions;
    
    void Awake()
    {
        if (threeYardButton == null || sevenYardButton == null || fifteenYardButton == null)
        {
            Debug.LogError("DistanceMenuManager: Button references not set!");
        }
        
        if (b27Target == null)
        {
            GameObject[] targets = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in targets)
            {
                if (obj.name.Contains("B27") && obj.name.Contains("Target"))
                {
                    b27Target = obj;
                    Debug.Log($"DistanceMenuManager: Found B27 target: {obj.name}");
                    break;
                }
            }
            
            if (b27Target == null)
            {
                Debug.LogError("DistanceMenuManager: B27 Target not found!");
            }
        }
        
        // Get the QualificationTargetController if the target exists
        if (b27Target != null)
        {
            targetController = b27Target.GetComponent<QualificationTargetController>();
            if (targetController == null)
            {
                targetController = b27Target.AddComponent<QualificationTargetController>();
                Debug.Log("DistanceMenuManager: Added QualificationTargetController to target");
            }
            
            // Fix the MeshCollider if it's missing its mesh
            QualificationTargetMeshFix meshFix = b27Target.GetComponent<QualificationTargetMeshFix>();
            if (meshFix == null)
            {
                meshFix = b27Target.AddComponent<QualificationTargetMeshFix>();
                Debug.Log("DistanceMenuManager: Added QualificationTargetMeshFix to ensure MeshCollider has mesh");
            }
        }
        
        inputActions = new AppControls();
    }
    
    void OnEnable()
    {
        inputActions.Enable();
        inputActions.UI.Enable();
        
        if (threeYardButton != null)
            threeYardButton.onClick.AddListener(() => SetTargetDistance(threeYardDistance));
            
        if (sevenYardButton != null)
            sevenYardButton.onClick.AddListener(() => SetTargetDistance(sevenYardDistance));
            
        if (fifteenYardButton != null)
            fifteenYardButton.onClick.AddListener(() => SetTargetDistance(fifteenYardDistance));
            
        // Setup responsive distance toggle
        if (responsiveDistanceToggle != null)
        {
            responsiveDistanceToggle.onValueChanged.AddListener(OnResponsiveDistanceToggled);
            
            // Set initial state from the target controller
            if (targetController != null)
            {
                responsiveDistanceToggle.isOn = targetController.IsResponsiveDistanceEnabled();
            }
        }
        
        // Update responsive distance label
        if (responsiveDistanceLabel != null)
        {
            responsiveDistanceLabel.text = "Responsive Distance";
        }
        
        // Subscribe to input action events
        inputActions.UI.ToggleResponsiveDistance.performed += OnToggleResponsiveDistanceAction;
        inputActions.UI.SetDistance3Yards.performed += OnSetDistance3Yards;
        inputActions.UI.SetDistance7Yards.performed += OnSetDistance7Yards;
        inputActions.UI.SetDistance15Yards.performed += OnSetDistance15Yards;
        
        UpdateButtonLabels();
    }
    
    void OnDisable()
    {
        // Unsubscribe from input action events
        inputActions.UI.ToggleResponsiveDistance.performed -= OnToggleResponsiveDistanceAction;
        inputActions.UI.SetDistance3Yards.performed -= OnSetDistance3Yards;
        inputActions.UI.SetDistance7Yards.performed -= OnSetDistance7Yards;
        inputActions.UI.SetDistance15Yards.performed -= OnSetDistance15Yards;
        
        inputActions.UI.Disable();
        inputActions.Disable();
        
        if (threeYardButton != null)
            threeYardButton.onClick.RemoveAllListeners();
            
        if (sevenYardButton != null)
            sevenYardButton.onClick.RemoveAllListeners();
            
        if (fifteenYardButton != null)
            fifteenYardButton.onClick.RemoveAllListeners();
            
        if (responsiveDistanceToggle != null)
            responsiveDistanceToggle.onValueChanged.RemoveListener(OnResponsiveDistanceToggled);
    }
    
    void Update()
    {
        // Input is now handled through input action events in OnEnable
        // No need to poll keyboard in Update anymore
    }
    
    private void SetTargetDistance(float distance)
    {
        if (b27Target != null)
        {
            // Disable responsive distance when manually setting position
            if (targetController != null && targetController.IsResponsiveDistanceEnabled())
            {
                targetController.SetResponsiveDistanceEnabled(false);
                if (responsiveDistanceToggle != null)
                {
                    responsiveDistanceToggle.isOn = false;
                }
                Debug.Log("DistanceMenuManager: Disabled responsive distance for manual positioning");
            }
            
            Vector3 currentPosition = b27Target.transform.position;
            currentPosition.z = distance;
            b27Target.transform.position = currentPosition;
            Debug.Log($"DistanceMenuManager: Set target distance to {distance} yards");
        }
        else
        {
            Debug.LogError("DistanceMenuManager: Cannot set distance - B27 Target is null!");
        }
    }
    
    private void OnResponsiveDistanceToggled(bool isOn)
    {
        if (targetController != null)
        {
            targetController.SetResponsiveDistanceEnabled(isOn);
            Debug.Log($"DistanceMenuManager: Responsive Distance set to {isOn}");
            
            if (isOn)
            {
                // When enabling responsive distance, reset to base position
                targetController.ResetToBasePosition();
            }
        }
        else
        {
            Debug.LogError("DistanceMenuManager: No QualificationTargetController found!");
        }
    }
    
    private void ToggleResponsiveDistance()
    {
        if (responsiveDistanceToggle != null)
        {
            // Toggle the checkbox state
            responsiveDistanceToggle.isOn = !responsiveDistanceToggle.isOn;
            Debug.Log($"DistanceMenuManager: R key pressed - Toggled Responsive Distance to {responsiveDistanceToggle.isOn}");
        }
        else
        {
            Debug.LogWarning("DistanceMenuManager: Responsive Distance Toggle UI element not found!");
        }
    }
    
    private void OnToggleResponsiveDistanceAction(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        ToggleResponsiveDistance();
    }
    
    private void OnSetDistance3Yards(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        SetTargetDistance(threeYardDistance);
        HighlightButton(threeYardButton);
    }
    
    private void OnSetDistance7Yards(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        SetTargetDistance(sevenYardDistance);
        HighlightButton(sevenYardButton);
    }
    
    private void OnSetDistance15Yards(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        SetTargetDistance(fifteenYardDistance);
        HighlightButton(fifteenYardButton);
    }
    
    private void HighlightButton(Button button)
    {
        if (button != null)
        {
            var colors = button.colors;
            var originalColor = colors.normalColor;
            colors.normalColor = colors.pressedColor;
            button.colors = colors;
            
            // Reset after a short delay
            Invoke(nameof(ResetButtonColors), 0.1f);
        }
    }
    
    private void ResetButtonColors()
    {
        if (threeYardButton != null)
        {
            var colors = threeYardButton.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            threeYardButton.colors = colors;
        }
        
        if (sevenYardButton != null)
        {
            var colors = sevenYardButton.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            sevenYardButton.colors = colors;
        }
        
        if (fifteenYardButton != null)
        {
            var colors = fifteenYardButton.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            fifteenYardButton.colors = colors;
        }
    }
    
    private void UpdateButtonLabels()
    {
        if (threeYardText != null)
        {
            threeYardText.text = "3 yard (1)";
        }
        else if (threeYardButton != null)
        {
            var textComponent = threeYardButton.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
                textComponent.text = "3 yard (1)";
        }
        
        if (sevenYardText != null)
        {
            sevenYardText.text = "7 yard (2)";
        }
        else if (sevenYardButton != null)
        {
            var textComponent = sevenYardButton.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
                textComponent.text = "7 yard (2)";
        }
        
        if (fifteenYardText != null)
        {
            fifteenYardText.text = "15 yard (3)";
        }
        else if (fifteenYardButton != null)
        {
            var textComponent = fifteenYardButton.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
                textComponent.text = "15 yard (3)";
        }
        
        // Update responsive distance label to show R key shortcut
        if (responsiveDistanceLabel != null)
        {
            responsiveDistanceLabel.text = "Responsive Distance (R)";
        }
    }
}