using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenShooter : MonoBehaviour
{
    [SerializeField]
    public GameObject BulletHole;

    private GameObject bulletHolePool;

    public void CreateShot(Vector2 screenPoint) {
        Ray ray = Camera.main.ScreenPointToRay(screenPoint);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit)) {
            Instantiate(
                BulletHole,
                hit.point + hit.normal * .01f,
                Quaternion.FromToRotation(Vector3.up, hit.normal),
                bulletHolePool.transform
            );
        }
    }

    public void ClearBulletHoles() {
        foreach (Transform transform in bulletHolePool.transform) {
            UnityEngine.Object.Destroy(transform.gameObject);
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
