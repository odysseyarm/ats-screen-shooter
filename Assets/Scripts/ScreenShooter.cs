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
            // Use BulletHoleParent if set, otherwise use the pool
            Transform parentTransform = BulletHoleParent != null ? BulletHoleParent : bulletHolePool.transform;
            
            Instantiate(
                BulletHole,
                hit.point + hit.normal * .01f,
                Quaternion.FromToRotation(Vector3.up, hit.normal),
                parentTransform
            );
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
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
