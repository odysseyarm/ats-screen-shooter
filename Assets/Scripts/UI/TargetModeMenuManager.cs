using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TargetModeMenuManager : MonoBehaviour
{
    [Header("Menu Components")]
    public GameObject menuPanel;
    public Button toggleButton;
    public TextMeshProUGUI toggleButtonText;
    
    [Header("Mode Buttons")]
    public Button qualificationButton;
    public Button reactiveButton;
    
    [Header("App Mode Manager")]
    public AppModeManager appModeManager;
    
    private bool isMenuOpen = true;
    
    void Start()
    {
        if (appModeManager == null)
        {
            appModeManager = FindObjectOfType<AppModeManager>();
            if (appModeManager == null)
            {
                Debug.LogError("AppModeManager not found! Please assign it in the inspector or ensure it exists in the scene.");
            }
        }
        
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleMenu);
            UpdateToggleButtonText();
        }
        
        if (qualificationButton != null)
        {
            qualificationButton.onClick.AddListener(() => SelectMode("Qualification"));
        }
        
        if (reactiveButton != null)
        {
            reactiveButton.onClick.AddListener(() => SelectMode("Reactive"));
        }
        
        if (menuPanel == null)
        {
            menuPanel = gameObject;
        }
    }
    
    void OnDestroy()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(ToggleMenu);
        }
        
        if (qualificationButton != null)
        {
            qualificationButton.onClick.RemoveAllListeners();
        }
        
        if (reactiveButton != null)
        {
            reactiveButton.onClick.RemoveAllListeners();
        }
    }
    
    private void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        
        if (menuPanel != null)
        {
            foreach (Transform child in menuPanel.transform)
            {
                if (child.gameObject != toggleButton?.gameObject)
                {
                    child.gameObject.SetActive(isMenuOpen);
                }
            }
        }
        
        UpdateToggleButtonText();
    }
    
    private void UpdateToggleButtonText()
    {
        if (toggleButtonText != null)
        {
            toggleButtonText.text = isMenuOpen ? "X" : "☰";
        }
    }
    
    private void SelectMode(string mode)
    {
        Debug.Log($"Target mode selected: {mode}");
        
        if (appModeManager != null)
        {
            switch (mode)
            {
                case "Qualification":
                    appModeManager.OnQualificationModeSelected();
                    UpdateButtonHighlight(TargetMode.Qualification);
                    break;
                case "Reactive":
                    appModeManager.OnReactiveModeSelected();
                    UpdateButtonHighlight(TargetMode.Reactive);
                    break;
            }
        }
        else
        {
            Debug.LogError("AppModeManager is not assigned!");
        }
    }
    
    public void UpdateButtonHighlight(TargetMode mode)
    {
        // Update button visual states based on current mode
        if (qualificationButton != null)
        {
            var qualColors = qualificationButton.colors;
            qualColors.normalColor = (mode == TargetMode.Qualification) ? qualColors.selectedColor : Color.white;
            qualificationButton.colors = qualColors;
        }
        
        if (reactiveButton != null)
        {
            var reactColors = reactiveButton.colors;
            reactColors.normalColor = (mode == TargetMode.Reactive) ? reactColors.selectedColor : Color.white;
            reactiveButton.colors = reactColors;
        }
    }
}