using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QualificationModeMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Toggle responsiveDistanceToggle;
    [SerializeField] private TextMeshProUGUI responsiveDistanceLabel;
    
    [Header("Target Controller")]
    private QualificationTargetController targetController;
    
    void Start()
    {
        // Find the QualificationTargetController in the scene
        GameObject qualificationTarget = GameObject.Find("B27 Paper Target w Stand 6.5ft version");
        if (qualificationTarget != null)
        {
            targetController = qualificationTarget.GetComponent<QualificationTargetController>();
            if (targetController == null)
            {
                targetController = qualificationTarget.AddComponent<QualificationTargetController>();
                Debug.Log("QualificationModeMenuManager: Added QualificationTargetController to target");
            }
        }
        else
        {
            Debug.LogWarning("QualificationModeMenuManager: Could not find B27 Paper Target in scene");
        }
        
        // Setup the toggle if it exists
        if (responsiveDistanceToggle != null)
        {
            responsiveDistanceToggle.onValueChanged.AddListener(OnResponsiveDistanceToggled);
            
            // Set initial state from the target controller
            if (targetController != null)
            {
                responsiveDistanceToggle.isOn = targetController.IsResponsiveDistanceEnabled();
            }
        }
        else
        {
            Debug.LogWarning("QualificationModeMenuManager: Responsive Distance Toggle not assigned in Inspector");
        }
        
        // Update label if it exists
        if (responsiveDistanceLabel != null)
        {
            responsiveDistanceLabel.text = "Responsive Distance";
        }
    }
    
    void OnEnable()
    {
        // Re-sync toggle state when menu becomes visible
        if (responsiveDistanceToggle != null && targetController != null)
        {
            responsiveDistanceToggle.isOn = targetController.IsResponsiveDistanceEnabled();
        }
    }
    
    void OnDestroy()
    {
        if (responsiveDistanceToggle != null)
        {
            responsiveDistanceToggle.onValueChanged.RemoveListener(OnResponsiveDistanceToggled);
        }
    }
    
    private void OnResponsiveDistanceToggled(bool isOn)
    {
        if (targetController != null)
        {
            targetController.SetResponsiveDistanceEnabled(isOn);
            Debug.Log($"QualificationModeMenuManager: Responsive Distance set to {isOn}");
        }
        else
        {
            Debug.LogError("QualificationModeMenuManager: No QualificationTargetController found!");
        }
    }
    
    public void SetResponsiveDistance(bool enabled)
    {
        if (responsiveDistanceToggle != null)
        {
            responsiveDistanceToggle.isOn = enabled;
        }
        
        if (targetController != null)
        {
            targetController.SetResponsiveDistanceEnabled(enabled);
        }
    }
}