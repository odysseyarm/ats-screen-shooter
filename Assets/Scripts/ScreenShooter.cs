using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenShooter : MonoBehaviour
{
    [SerializeField]
    public GameObject BulletHole;

    [SerializeField]
    [Tooltip("Optional: Parent transform for bullet holes. If set, bullet holes will be instantiated as children of this transform instead of the pool.")]
    public Transform BulletHoleParent;
    
    [Header("Reactive Mode Settings")]
    [SerializeField]
    [Tooltip("Scale multiplier for bullet holes in Reactive Mode")]
    private float reactiveModeScale = 2.5f; // 250%

    private GameObject bulletHolePool;
    private AppModeManager appModeManager;

    public void CreateShot(Vector2 screenPoint) {
        Ray ray = Camera.main.ScreenPointToRay(screenPoint);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit)) {
            // Check if we hit a ReactiveTarget
            ReactiveTarget target = hit.collider.GetComponentInParent<ReactiveTarget>();
            if (target != null)
            {
                target.OnHit(hit.point, hit.normal);
            }
            
            // Parent bullet holes to the hit object so they move with it
            Transform parentTransform = null;
            
            // Check if we hit a target that should be the parent
            if (hit.collider.name == "Body1" || hit.collider.gameObject.name.Contains("Target"))
            {
                // Parent to the actual target object
                parentTransform = hit.collider.transform;
            }
            else if (BulletHoleParent != null)
            {
                // Use the manually set parent if available
                parentTransform = BulletHoleParent;
            }
            else
            {
                // Fall back to the pool
                parentTransform = bulletHolePool.transform;
            }
            
            if (BulletHole != null)
            {
                GameObject bulletHole = Instantiate(
                    BulletHole,
                    hit.point + hit.normal * .01f,
                    Quaternion.FromToRotation(Vector3.up, hit.normal),
                    parentTransform
                );
                
                // Scale up bullet holes in Reactive Mode
                if (appModeManager != null && appModeManager.GetCurrentMode() == TargetMode.Reactive)
                {
                    bulletHole.transform.localScale *= reactiveModeScale;
                    Debug.Log($"Scaled bullet hole to {reactiveModeScale}x for Reactive Mode");
                }
            }
        }
    }

    public void ClearBulletHoles() {
        // Clear bullet holes from the pool
        foreach (Transform transform in bulletHolePool.transform) {
            UnityEngine.Object.Destroy(transform.gameObject);
        }
        
        // Also clear bullet holes from the parent if set
        if (BulletHoleParent != null) {
            // Find all BulletHole children and destroy them
            foreach (Transform child in BulletHoleParent) {
                if (child.name.Contains("BulletHole")) {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }
        }
        
        // Clear bullet holes that may be parented to target objects (like Body1)
        // This handles the case where bullet holes are parented directly to hit objects
        GameObject[] allBulletHoles = GameObject.FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allBulletHoles) {
            if (obj.name.Contains("BulletHole(Clone)")) {
                UnityEngine.Object.Destroy(obj);
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        bulletHolePool = new GameObject();
        bulletHolePool.name = "BulletHolePool";
        
        // Find the AppModeManager to check current mode
        appModeManager = FindObjectOfType<AppModeManager>();
        if (appModeManager == null)
        {
            Debug.LogWarning("ScreenShooter: AppModeManager not found in scene");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
