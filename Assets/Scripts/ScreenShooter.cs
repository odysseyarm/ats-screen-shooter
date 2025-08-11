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

    private GameObject bulletHolePool;

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
                Instantiate(
                    BulletHole,
                    hit.point + hit.normal * .01f,
                    Quaternion.FromToRotation(Vector3.up, hit.normal),
                    parentTransform
                );
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
    }

    // Start is called before the first frame update
    void Start()
    {
        bulletHolePool = new GameObject();
        bulletHolePool.name = "BulletHolePool";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
