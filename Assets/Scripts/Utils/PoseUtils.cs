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
        raw.SetColumn(0, new Vector4(pose.rotation.m11, pose.rotation.m12, pose.rotation.m13, 0f)); // right
        raw.SetColumn(1, new Vector4(pose.rotation.m21, pose.rotation.m22, pose.rotation.m23, 0f)); // up
        raw.SetColumn(2, new Vector4(pose.rotation.m31, pose.rotation.m32, pose.rotation.m33, 0f)); // forward
        raw.SetColumn(3, new Vector4(pose.translation.x, pose.translation.y, pose.translation.z, 1f)); // translation

        Matrix4x4 flipY = Matrix4x4.identity;
        flipY.m11 = -1; // flip Y row
        Matrix4x4 transformed = flipY * raw * flipY;

        Vector3 position = transformed.GetColumn(3);
        Quaternion rotation = transformed.rotation;

        return new UnityPose
        {
            position = position,
            rotation = rotation,
            matrix = transformed
        };
    }
}
