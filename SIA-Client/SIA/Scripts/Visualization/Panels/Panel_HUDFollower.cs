using UnityEngine;

public class Panel_HUDFollower : MonoBehaviour
{
    public Transform headTransform;

    [Header("  (  )")]
    [Tooltip(" (m)")]
    public float distance = 0.6f;
    [Tooltip("(+)/(-) (m).    (-0.3~-0.5 )")]
    public float viewOffsetY = -10.70f;   //   -0.25  -0.35 ( )

    [Header("")]
    [Tooltip("   ( )")]
    public float rotationSmooth = 0.8f;  //  1.2  0.8 (  )
    [Tooltip("   ( )")]
    public float positionSmooth = 6f;
    public float deadzoneDegrees = 8f;

    [Header("")]
    [Tooltip("HUD      (  )")]
    public float maxAngle = 30f; // degree
    [Tooltip("(+)/(-) (m).    ")]
    public float viewOffsetX = 0f;

    [Header("  ")]
    [Tooltip("    (deg, = )")]
    public float tiltTowardFaceDeg = -60f;  //   0  10 

    //  
    Quaternion _smoothedHeadRot;
    bool _initialized;

    void LateUpdate()
    {
        if (!headTransform) return;

        float dt = Time.deltaTime;
        Quaternion headRot = headTransform.rotation;

        // 
        if (!_initialized)
        {
            _smoothedHeadRot = headRot;
            _initialized = true;
        }

        // -----  :  +  (Slerp) -----
        float ang = Quaternion.Angle(_smoothedHeadRot, headRot);

        if (ang > deadzoneDegrees)
        {
            float t = 1f - Mathf.Exp(-rotationSmooth * dt);
            _smoothedHeadRot = Quaternion.Slerp(_smoothedHeadRot, headRot, t);
        }

        // -----    ( pitch ,  yaw ) -----
        Vector3 flatForward = _smoothedHeadRot * Vector3.forward;
        flatForward.y = 0f;
        flatForward.Normalize();

        Vector3 desiredPos = headTransform.position +
            flatForward * Mathf.Max(0.05f, distance) +
            Vector3.up * viewOffsetY * 2.5f;

        //  
        float tp = 1f - Mathf.Exp(-positionSmooth * dt);
        transform.position = Vector3.Lerp(transform.position, desiredPos, tp);

        // -----      -----
        Vector3 lookDir = headTransform.position - transform.position;
        lookDir.y = 0f; //  
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion baseRot = Quaternion.LookRotation(lookDir, Vector3.up);

            //    
            Quaternion tilt = Quaternion.AngleAxis(tiltTowardFaceDeg, baseRot * Vector3.right);
            transform.rotation = tilt * baseRot;
        }

        // -----    -----
        Vector3 headForward = _smoothedHeadRot * Vector3.forward;
        headForward.y = 0f; // pitch 
        headForward.Normalize();

        Vector3 toPanel = (desiredPos - headTransform.position).normalized;
        float angle = Vector3.Angle(headForward, toPanel);

        if (angle < maxAngle)
        {
            Quaternion rotLimit = Quaternion.AngleAxis(maxAngle, Vector3.right);
            Vector3 limitedDir = rotLimit * headForward;

            desiredPos = headTransform.position +
                         limitedDir.normalized * distance +
                         Vector3.up * viewOffsetY *2.5f;

            transform.position = Vector3.Lerp(transform.position, desiredPos, tp);
        }
    }
}
