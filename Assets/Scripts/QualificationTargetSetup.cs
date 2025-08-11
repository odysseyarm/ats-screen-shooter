using UnityEngine;

public class QualificationTargetSetup : MonoBehaviour
{
    void Start()
    {
        // Check Body1 child for mesh collider
        Transform body1 = transform.Find("Body1");
        if (body1 != null)
        {
            Debug.Log($"QualificationTargetSetup: Found Body1 child");
            CheckAndFixCollider(body1.gameObject);
        }
        else
        {
            Debug.LogWarning($"QualificationTargetSetup: Body1 child not found, checking main object");
            CheckAndFixCollider(gameObject);
        }
        
        // Check all colliders in children
        Collider[] allColliders = GetComponentsInChildren<Collider>();
        Debug.Log($"QualificationTargetSetup: Found {allColliders.Length} colliders in target hierarchy:");
        foreach (var col in allColliders)
        {
            Debug.Log($"  - {col.gameObject.name}: Type={col.GetType().Name}, Enabled={col.enabled}, Trigger={col.isTrigger}, Layer={col.gameObject.layer}");
            if (col is MeshCollider mc)
            {
                Debug.Log($"    MeshCollider: Convex={mc.convex}");
                // Mesh colliders need to be convex for dynamic objects or non-convex for static
                // For a shooting target, non-convex is usually fine
            }
        }
    }
    
    void CheckAndFixCollider(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"QualificationTargetSetup: No collider found on {obj.name}! Adding BoxCollider...");
            col = obj.AddComponent<BoxCollider>();
        }
        
        // Make sure the collider is enabled
        if (!col.enabled)
        {
            Debug.LogWarning($"QualificationTargetSetup: Collider was disabled on {obj.name}, enabling it");
            col.enabled = true;
        }
        
        // Check if it's a trigger and fix if needed
        if (col.isTrigger)
        {
            Debug.LogWarning($"QualificationTargetSetup: Collider was set as trigger on {obj.name}, setting to non-trigger for bullet hits");
            col.isTrigger = false;
        }
        
        Debug.Log($"QualificationTargetSetup: {obj.name} collider check complete - Type: {col.GetType().Name}, Enabled: {col.enabled}, Trigger: {col.isTrigger}");
        
        // Log the bounds for debugging
        Debug.Log($"QualificationTargetSetup: Collider bounds - Center: {col.bounds.center}, Size: {col.bounds.size}");
    }
    
    void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}