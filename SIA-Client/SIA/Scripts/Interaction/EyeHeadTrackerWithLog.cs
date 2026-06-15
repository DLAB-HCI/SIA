using UnityEngine;
using OVR; // Oculus Integration

public class EyeHeadTrackerWithLog : MonoBehaviour
{
    void Update()
    {
        // Head pose
        OVRPlugin.Posef headPose = OVRPlugin.GetNodePose(OVRPlugin.Node.Head, OVRPlugin.Step.Render);
        Vector3 headPos = new Vector3(headPose.Position.x, headPose.Position.y, headPose.Position.z);
        Quaternion headRot = new Quaternion(headPose.Orientation.x, headPose.Orientation.y, headPose.Orientation.z, headPose.Orientation.w);

        // Eye poses (raw position + orientation)
        OVRPlugin.Posef leftEyePose  = OVRPlugin.GetNodePose(OVRPlugin.Node.EyeLeft,  OVRPlugin.Step.Render);
        OVRPlugin.Posef rightEyePose = OVRPlugin.GetNodePose(OVRPlugin.Node.EyeRight, OVRPlugin.Step.Render);

        Vector3 leftPos  = new Vector3(leftEyePose.Position.x,  leftEyePose.Position.y,  leftEyePose.Position.z);
        Quaternion leftRot = new Quaternion(leftEyePose.Orientation.x, leftEyePose.Orientation.y, leftEyePose.Orientation.z, leftEyePose.Orientation.w);

        Vector3 rightPos  = new Vector3(rightEyePose.Position.x,  rightEyePose.Position.y,  rightEyePose.Position.z);
        Quaternion rightRot = new Quaternion(rightEyePose.Orientation.x, rightEyePose.Orientation.y, rightEyePose.Orientation.z, rightEyePose.Orientation.w);

        // Gaze origin and direction from both eyes
        Vector3 gazeOrigin    = (leftPos + rightPos) * 0.5f;
        Vector3 gazeDirection = ((leftRot  * Vector3.forward) + (rightRot * Vector3.forward)) * 0.5f;

        _ = headPos;
        _ = headRot;
        _ = gazeOrigin;
        _ = gazeDirection;
    }
}
