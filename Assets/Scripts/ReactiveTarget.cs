using System;
using System.Collections;
using UnityEngine;

public enum ReactiveTargetState
{
    Inactive,    // Down and not hittable
    Active,      // Standing and hittable (enemy)
    Friendly     // Standing and hittable but shouldn't be shot
}

public class ReactiveTarget : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private ReactiveTargetState currentState = ReactiveTargetState.Inactive;
    
    [Header("Hit Configuration")]
    [SerializeField] private int currentHitPoints = 1;
    [SerializeField] private int requiredHitsToDown = 1;
    [SerializeField] private float colorChangeDelay = 0.5f;
    
    [Header("Components")]
    [SerializeField] private Collider targetCollider;
    
    [Header("Day Materials")]
    [SerializeField] private Material enemyMaterialDay;
    [SerializeField] private Material friendlyMaterialDay;
    [SerializeField] private Material inactiveMaterialDay;
    
    [Header("Night Materials")]
    [SerializeField] private Material enemyMaterialNight;
    [SerializeField] private Material friendlyMaterialNight;
    [SerializeField] private Material inactiveMaterialNight;
    
    [Header("Components")]
    [SerializeField] private Renderer targetRenderer;
    
    private Animator targetAnimator;
    private LightingModeManager lightingModeManager;
    private Coroutine delayedMaterialUpdateCoroutine;
    
    public ReactiveTargetState CurrentState => currentState;
    public bool IsActive => currentState != ReactiveTargetState.Inactive;
    public bool IsFriendly => currentState == ReactiveTargetState.Friendly;
    public int CurrentHitPoints => currentHitPoints;
    public int RequiredHitsToDown => requiredHitsToDown;
    
    public event Action<ReactiveTarget> OnTargetHit;
    public event Action<ReactiveTarget> OnTargetDowned;
    public event Action<ReactiveTarget> OnStateChanged;
    
    void Awake()
    {
        targetAnimator = GetComponent<Animator>();
        if (targetAnimator == null)
        {
            Debug.LogError($"No Animator found on {gameObject.name}! ReactiveTarget requires an Animator component.");
        }
        
        if (targetCollider == null)
        {
            targetCollider = GetComponentInChildren<Collider>();
        }
        
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }
        
        lightingModeManager = FindObjectOfType<LightingModeManager>();
    }
    
    void Start()
    {
        SetState(ReactiveTargetState.Inactive);
    }
    
    public void SetState(ReactiveTargetState newState, bool immediateColorChange = true)
    {
        if (currentState == newState) return;
        
        ReactiveTargetState previousState = currentState;
        currentState = newState;
        
        // Reset hit points when activating
        if (newState != ReactiveTargetState.Inactive && previousState == ReactiveTargetState.Inactive)
        {
            currentHitPoints = requiredHitsToDown;
        }
        
        if (immediateColorChange)
        {
            UpdateMaterial();
        }
        else
        {
            // Cancel any existing delayed update
            if (delayedMaterialUpdateCoroutine != null)
            {
                StopCoroutine(delayedMaterialUpdateCoroutine);
            }
            delayedMaterialUpdateCoroutine = StartCoroutine(DelayedMaterialUpdate());
        }
        
        UpdateCollider();
        UpdateAnimation(previousState, newState);
        
        OnStateChanged?.Invoke(this);
    }
    
    private IEnumerator DelayedMaterialUpdate()
    {
        yield return new WaitForSeconds(colorChangeDelay);
        UpdateMaterial();
        delayedMaterialUpdateCoroutine = null;
    }
    
    private void UpdateAnimation(ReactiveTargetState from, ReactiveTargetState to)
    {
        if (targetAnimator == null) return;
        
        if (to == ReactiveTargetState.Inactive)
        {
            targetAnimator.ResetTrigger("StandUp");
            targetAnimator.SetTrigger("KnockDown");
        }
        else if (from == ReactiveTargetState.Inactive && (to == ReactiveTargetState.Active || to == ReactiveTargetState.Friendly))
        {
            targetAnimator.ResetTrigger("KnockDown");
            targetAnimator.SetTrigger("StandUp");
        }
    }
    
    private void UpdateMaterial()
    {
        if (targetRenderer == null)
        {
            Debug.LogWarning($"No renderer found on {gameObject.name} - cannot update material");
            return;
        }
        
        bool isDarkMode = false;
        if (lightingModeManager != null)
        {
            isDarkMode = lightingModeManager.GetCurrentMode() == LightingModeManager.LightingMode.Dark;
        }
        
        Material matToUse = null;
        string modeText = isDarkMode ? "NIGHT" : "DAY";
        
        switch (currentState)
        {
            case ReactiveTargetState.Active:
                matToUse = isDarkMode ? enemyMaterialNight : enemyMaterialDay;
                Debug.Log($"{gameObject.name} set to ENEMY ({modeText}) - Material: {(matToUse != null ? matToUse.name : "NULL")}");
                break;
            case ReactiveTargetState.Friendly:
                matToUse = isDarkMode ? friendlyMaterialNight : friendlyMaterialDay;
                Debug.Log($"{gameObject.name} set to FRIENDLY ({modeText}) - Material: {(matToUse != null ? matToUse.name : "NULL")}");
                break;
            case ReactiveTargetState.Inactive:
                matToUse = isDarkMode ? inactiveMaterialNight : inactiveMaterialDay;
                Debug.Log($"{gameObject.name} set to INACTIVE ({modeText}) - Material: {(matToUse != null ? matToUse.name : "NULL")}");
                break;
        }
        
        if (matToUse != null)
        {
            targetRenderer.material = matToUse;
        }
        else
        {
            Debug.LogWarning($"No {modeText} material assigned for state {currentState} on {gameObject.name}");
        }
    }
    
    private void UpdateCollider()
    {
        if (targetCollider != null)
        {
            targetCollider.enabled = IsActive;
        }
    }
    
    public void OnHit(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (!IsActive)
        {
            Debug.Log($"Target {gameObject.name} was hit but is inactive");
            return;
        }
        
        // Process the hit
        ProcessHit();
    }
    
    private void ProcessHit()
    {
        // Decrement hit points
        currentHitPoints--;
        
        Debug.Log($"Target {gameObject.name} hit! Was {(IsFriendly ? "FRIENDLY" : "ENEMY")} - Hits remaining: {currentHitPoints}");
        
        // Fire hit event
        OnTargetHit?.Invoke(this);
        
        // Check if target should go down
        if (currentHitPoints <= 0)
        {
            // Target is downed
            Debug.Log($"Target {gameObject.name} is going down!");
            OnTargetDowned?.Invoke(this);
            
            // Change state with delayed color change
            SetState(ReactiveTargetState.Inactive, false);
            
            if (IsFriendly)
            {
                Debug.LogWarning("Friendly target was shot down!");
            }
        }
        else
        {
            // Target still standing but took a hit - could add visual feedback here
            Debug.Log($"Target {gameObject.name} still standing with {currentHitPoints} hits remaining");
        }
    }
    
    public void Activate(bool asFriendly = false)
    {
        SetState(asFriendly ? ReactiveTargetState.Friendly : ReactiveTargetState.Active);
    }
    
    public void SetRequiredHits(int hits)
    {
        requiredHitsToDown = Mathf.Max(1, hits);
        currentHitPoints = requiredHitsToDown;
    }
    
    public void Deactivate()
    {
        SetState(ReactiveTargetState.Inactive);
    }
    
    public void RefreshMaterial()
    {
        UpdateMaterial();
    }
}