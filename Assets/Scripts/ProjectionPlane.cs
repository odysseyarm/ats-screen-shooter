using UnityEngine;

namespace Apt.Unity.Projection
{
    [ExecuteInEditMode]
    public class ProjectionPlane : MonoBehaviour
    {
        //Code based on https://csc.lsu.edu/~kooima/pdfs/gen-perspective.pdf
        //and https://forum.unity.com/threads/vr-cave-projection.76051/
        [Header("Visualization")]
        public bool DrawGizmo = true;

        //Bottom-left, Bottom-right top-left, top-right corners of plane respectively
        public Vector3 BottomLeft { get; private set; }
        public Vector3 BottomRight { get; private set; }
        public Vector3 TopLeft { get; private set; }
        public Vector3 TopRight { get; private set; }

        //Vector right, up, normal from center of plane
        public Vector3 DirRight { get; private set; }
        public Vector3 DirUp { get; private set; }
        public Vector3 DirNormal { get; private set; }

        private GameObject alignmentCube;
        private Transform backTrans;
        private Transform leftTrans;
        private Transform rightTrans;
        private Transform topTrans;
        private Transform bottomTrans;

        private bool boundsSet = false;

        Matrix4x4 m;
        public Matrix4x4 M { get => m; }

        private void OnDrawGizmos()
        {
            if (DrawGizmo)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(BottomLeft, BottomRight);
                Gizmos.DrawLine(BottomLeft, TopLeft);
                Gizmos.DrawLine(TopRight, BottomRight);
                Gizmos.DrawLine(TopLeft, TopRight);

                //Draw direction towards eye
                Gizmos.color = Color.cyan;
                var planeCenter = BottomLeft + ((TopRight - BottomLeft) * 0.5f);
                Gizmos.DrawLine(planeCenter, planeCenter + DirNormal);
            }
        }

        void Start() {
            if (!boundsSet) {
                Vector2 size = new Vector2(1.756f, 0.988f);

                BottomLeft = transform.TransformPoint(new Vector3(-size.x, -size.y) * 0.5f);
                BottomRight = transform.TransformPoint(new Vector3(size.x, -size.y) * 0.5f);
                TopLeft = transform.TransformPoint(new Vector3(-size.x, size.y) * 0.5f);
                TopRight = transform.TransformPoint(new Vector3(size.x, size.y) * 0.5f);

                boundsSet = true;
            }
        }

        void Update()
        {
            DirRight = (BottomRight - BottomLeft).normalized;
            DirUp = (TopLeft - BottomLeft).normalized;
            DirNormal = -Vector3.Cross(DirRight, DirUp).normalized;

            m = Matrix4x4.zero;
            m[0, 0] = DirRight.x;
            m[0, 1] = DirRight.y;
            m[0, 2] = DirRight.z;

            m[1, 0] = DirUp.x;
            m[1, 1] = DirUp.y;
            m[1, 2] = DirUp.z;

            m[2, 0] = DirNormal.x;
            m[2, 1] = DirNormal.y;
            m[2, 2] = DirNormal.z;

            m[3, 3] = 1.0f;

        }

        public void SetLocalBounds(Vector3 tl, Vector3 tr, Vector3 bl, Vector3 br) {
            TopLeft = transform.TransformPoint(tl);
            TopRight = transform.TransformPoint(tr);
            BottomLeft = transform.TransformPoint(bl);
            BottomRight = transform.TransformPoint(br);
            boundsSet = true;
        }

        private void OnApplicationQuit()
        {
            if (Application.isPlaying && alignmentCube != null)
                DestroyImmediate(alignmentCube);
        }
    }
}