using UnityEngine;

public class QualificationTargetMeshFix : MonoBehaviour
{
    void Start()
    {
        // Find Body1 child
        Transform body1 = transform.Find("Body1");
        if (body1 != null)
        {
            MeshCollider meshCollider = body1.GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                if (meshCollider.sharedMesh == null)
                {
                    Debug.Log("QualificationTargetMeshFix: MeshCollider has no mesh, attempting to fix...");
                    
                    // Try to get the mesh from the MeshFilter
                    MeshFilter meshFilter = body1.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        meshCollider.sharedMesh = meshFilter.sharedMesh;
                        Debug.Log($"QualificationTargetMeshFix: Assigned mesh '{meshFilter.sharedMesh.name}' to MeshCollider");
                    }
                    else
                    {
                        Debug.LogError("QualificationTargetMeshFix: Could not find mesh from MeshFilter!");
                    }
                }
                else
                {
                    Debug.Log($"QualificationTargetMeshFix: MeshCollider already has mesh: {meshCollider.sharedMesh.name}");
                }
            }
        }
    }
}