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
    public Toggle responsiveDistanceToggle;  // Restored responsive toggle
    public TextMeshProUGUI responsiveDistanceLabel;
    
    [Header("True-Size Rendering UI")]
    public Toggle trueSizeToggle;
    public TextMeshProUGUI trueSizeLabel;
    
    [Header("Target Reference")]
    public GameObject b27Target;
    private QualificationTargetController targetController;
    private QualificationDistanceManager distanceManager;  // For true-size rendering
    
    [Header("Distance Settings")]
    [SerializeField] private float threeYardDistance = 3f;  // Changed to actual yards
    [SerializeField] private float sevenYardDistance = 7f;
    [SerializeField] private float fifteenYardDistance = 15f;
    
    private AppControls inputActions;
    private InputAction toggleTrueSizeAction;  // Custom action for T key
    
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
        
        // Find the QualificationDistanceManager for true-size rendering
        distanceManager = FindObjectOfType<QualificationDistanceManager>();
        if (distanceManager == null)
        {
            Debug.LogWarning("DistanceMenuManager: QualificationDistanceManager not found - true-size rendering will not work");
        }
        
        inputActions = new AppControls();
        
        // Create custom input action for T key to toggle True-Size Rendering
        toggleTrueSizeAction = new InputAction("ToggleTrueSize", binding: "<Keyboard>/t");
        toggleTrueSizeAction.performed += OnToggleTrueSizeAction;
    }
    
    void OnEnable()
    {
        inputActions.Enable();
        inputActions.UI.Enable();
        toggleTrueSizeAction?.Enable();  // Enable T key action
        
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
            
            // For Qualification Mode, default responsive distance off
            responsiveDistanceToggle.isOn = false;
            if (targetController != null)
            {
                targetController.SetResponsiveDistanceEnabled(false);
            }
        }
        
        // Setup true-size rendering toggle
        if (trueSizeToggle != null)
        {
            trueSizeToggle.onValueChanged.AddListener(OnTrueSizeToggled);
            
            // Start with true-size rendering disabled by default
            trueSizeToggle.isOn = false;
            if (distanceManager != null)
            {
                distanceManager.SetTrueSizeEnabled(false);
            }
        }
        
        // Update labels
        if (responsiveDistanceLabel != null)
        {
            responsiveDistanceLabel.text = "Responsive Distance (R)";
        }
        if (trueSizeLabel != null)
        {
            trueSizeLabel.text = "True-Size Rendering (T)";
        }
        
        // Subscribe to input action events
        inputActions.UI.ToggleResponsiveDistance.performed += OnToggleResponsiveDistanceAction;  // R key toggles responsive distance
        inputActions.UI.SetDistance3Yards.performed += OnSetDistance3Yards;
        inputActions.UI.SetDistance7Yards.performed += OnSetDistance7Yards;
        inputActions.UI.SetDistance15Yards.performed += OnSetDistance15Yards;
        
        UpdateButtonLabels();
    }
    
    void OnDisable()
    {
        // Unsubscribe from input action events
        inputActions.UI.ToggleResponsiveDistance.performed -= OnToggleResponsiveDistanceAction;  // R key toggles responsive distance
        inputActions.UI.SetDistance3Yards.performed -= OnSetDistance3Yards;
        inputActions.UI.SetDistance7Yards.performed -= OnSetDistance7Yards;
        inputActions.UI.SetDistance15Yards.performed -= OnSetDistance15Yards;
        
        toggleTrueSizeAction?.Disable();  // Disable T key action
        inputActions.UI.Disable();
        inputActions.Disable();
        
        if (threeYardButton != null)
            threeYardButton.onClick.RemoveAllListeners();
            
        if (sevenYardButton != null)
            sevenYardButton.onClick.RemoveAllListeners();
            
        if (fifteenYardButton != null)
            fifteenYardButton.onClick.RemoveAllListeners();
            
        if (trueSizeToggle != null)
            trueSizeToggle.onValueChanged.RemoveListener(OnTrueSizeToggled);
    }
    
    void Update()
    {
        // Input is now handled through input action events in OnEnable
        // No need to poll keyboard in Update anymore
    }
    
    private void SetTargetDistance(float distanceYards)
    {
        // Use QualificationDistanceManager for true-size rendering if available
        if (distanceManager != null)
        {
            distanceManager.SetDistanceYards(distanceYards);
            Debug.Log($"DistanceMenuManager: Set true-size distance to {distanceYards} yards");
        }
        else if (b27Target != null)
        {
            // Fallback to old method if QualificationDistanceManager not available
            // Disable responsive distance when manually setting position
            if (targetController != null && targetController.IsResponsiveDistanceEnabled())
            {
                targetController.SetResponsiveDistanceEnabled(false);
                Debug.Log("DistanceMenuManager: Disabled responsive distance for manual positioning");
            }
            
            // Note: This is the old method that just moves the target object
            // For true-size rendering, we should be moving the camera instead
            Vector3 currentPosition = b27Target.transform.position;
            currentPosition.z = distanceYards + 4f;  // Add offset for old method
            b27Target.transform.position = currentPosition;
            Debug.Log($"DistanceMenuManager: Fallback - Set target position to Z={currentPosition.z}");
        }
        else
        {
            Debug.LogError("DistanceMenuManager: Cannot set distance - No QualificationDistanceManager and B27 Target is null!");
        }
    }
    
    private void OnResponsiveDistanceToggled(bool isOn)
    {
        if (targetController != null)
        {
            targetController.SetResponsiveDistanceEnabled(isOn);
            Debug.Log($"DistanceMenuManager: Responsive Distance set to {isOn}");
        }
        else
        {
            Debug.LogError("DistanceMenuManager: No QualificationTargetController found!");
        }
    }
    
    private void OnTrueSizeToggled(bool isOn)
    {
        if (distanceManager != null)
        {
            distanceManager.SetTrueSizeEnabled(isOn);
            Debug.Log($"DistanceMenuManager: True-Size Rendering set to {isOn}");
        }
        else
        {
            Debug.LogError("DistanceMenuManager: No QualificationDistanceManager found!");
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
    
    private void ToggleTrueSize()
    {
        if (trueSizeToggle != null)
        {
            // Toggle the checkbox state
            trueSizeToggle.isOn = !trueSizeToggle.isOn;
            Debug.Log($"DistanceMenuManager: T key pressed - Toggled True-Size Rendering to {trueSizeToggle.isOn}");
        }
        else
        {
            Debug.LogWarning("DistanceMenuManager: True-Size Toggle UI element not found!");
        }
    }
    
    private void OnToggleTrueSizeAction(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        ToggleTrueSize();
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
        
        // Keep labels up-to-date
        if (responsiveDistanceLabel != null)
        {
            responsiveDistanceLabel.text = "Responsive Distance (R)";
        }
        if (trueSizeLabel != null)
        {
            trueSizeLabel.text = "True-Size Rendering (T)";
        }
    }
}