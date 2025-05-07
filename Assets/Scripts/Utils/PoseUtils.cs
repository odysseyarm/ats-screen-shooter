using UnityEngine;
using RadiosityPose = Radiosity.OdysseyHubClient.Pose;

public static class PoseUtils
{
    public struct UnityPose
    {
        public Vector3 position;
        public Quaternion rotation;
        public Matrix4x4 matrix;
    }

    public static UnityPose ConvertOdyPoseToUnity(RadiosityPose pose)
    {
        Matrix4x4 raw = new Matrix4x4();
        raw.SetColumn(0, new Vector4(pose.rotation.m11, pose.rotation.m21, pose.rotation.m31, 0f)); // right
        raw.SetColumn(1, new Vector4(pose.rotation.m12, pose.rotation.m22, pose.rotation.m32, 0f)); // up
        raw.SetColumn(2, new Vector4(pose.rotation.m13, pose.rotation.m23, pose.rotation.m33, 0f)); // forward
        raw.SetColumn(3, new Vector4(pose.translation.x, pose.translation.y, pose.translation.z, 1f)); // translation

        // flip Y axis
        Matrix4x4 flipY = new Matrix4x4();
        flipY.SetColumn(0, new Vector4(1f, 0f, 0f, 0f)); // right
        flipY.SetColumn(1, new Vector4(0f, -1f, 0f, 0f)); // up
        flipY.SetColumn(2, new Vector4(0f, 0f, 1f, 0f)); // forward
        flipY.SetColumn(3, new Vector4(0f, 0f, 0f, 1f)); // translation

        // apply flip
        Matrix4x4 transformed = flipY * raw * flipY;

        transformed.Decompose(out var t, out var r, out var s);

        return new UnityPose {
            position = t,
            rotation = r,
            matrix = transformed,
        };
    }
}
