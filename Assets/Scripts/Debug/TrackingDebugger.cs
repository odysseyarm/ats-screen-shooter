using UnityEngine;

public class TrackingDebugger : MonoBehaviour
{
    private InputHandlers inputHandlers;
    private QualificationDistanceManager qualificationDistanceManager;
    private QualificationTargetController targetController;
    private float lastLogTime = 0f;
    private float logInterval = 2f; // Log every 2 seconds
    
    void Start()
    {
        inputHandlers = FindObjectOfType<InputHandlers>();
        qualificationDistanceManager = FindObjectOfType<QualificationDistanceManager>();
        targetController = FindObjectOfType<QualificationTargetController>();
        
        if (inputHandlers == null)
            Debug.LogError("TrackingDebugger: InputHandlers not found!");
        if (qualificationDistanceManager == null)
            Debug.LogError("TrackingDebugger: QualificationDistanceManager not found!");
        if (targetController == null)
            Debug.LogError("TrackingDebugger: QualificationTargetController not found!");
    }
    
    void Update()
    {
        if (Time.time - lastLogTime > logInterval)
        {
            lastLogTime = Time.time;
            LogTrackingStatus();
        }
        
        // Log on key press for immediate feedback
        if (Input.GetKeyDown(KeyCode.F12))
        {
            LogTrackingStatus();
        }
    }
    
    private void LogTrackingStatus()
    {
        string status = "=== TRACKING STATUS ===\n";
        
        if (inputHandlers != null)
        {
            status += $"InputHandlers.IsTracking: {inputHandlers.IsTracking}\n";
            status += $"InputHandlers.Translation: {inputHandlers.Translation}\n";
            
            int deviceCount = 0;
            foreach (var (_, player) in inputHandlers.Players)
            {
                if (player != null && player.device != null)
                    deviceCount++;
            }
            status += $"Connected Devices: {deviceCount}\n";
        }
        else
        {
            status += "InputHandlers: NOT FOUND\n";
        }
        
        if (qualificationDistanceManager != null)
        {
            status += $"TSR Enabled: {qualificationDistanceManager.IsTrueSizeEnabled()}\n";
            status += $"TSR Distance: {qualificationDistanceManager.GetCurrentDistanceYards()} yards\n";
        }
        else
        {
            status += "TSR: NOT FOUND\n";
        }
        
        if (targetController != null)
        {
            status += $"Responsive Distance Enabled: {targetController.IsResponsiveDistanceEnabled()}\n";
            status += $"Target Position: {targetController.transform.position}\n";
        }
        else
        {
            status += "Responsive Distance: NOT FOUND\n";
        }
        
        status += "======================";
        Debug.Log(status);
    }
}
