using UnityEngine;

[DisallowMultipleComponent]
public class Panel_HorizontalFollower : MonoBehaviour
{
    public Transform headTransform;
    public Camera viewCamera;

    [Header("Viewport (0-1)")]
    [SerializeField] public float viewportX = 0.50f;
    [SerializeField] public float viewportY = 0.20f;
    [SerializeField] public float cameraSpaceZ = 3.10f;
    [SerializeField] public float viewportXMin = 0.15f;
    [SerializeField] public float viewportXMax = 0.85f;
    [SerializeField] float yawLookOffsetDeg = 0.0f;
    [SerializeField] float leftOffset = 0.0f;
    [SerializeField] float rightOffset = 0.0f;

    [Header("Smoothing")]
    public float headRotSmooth = 0.15f;
    public float bodyPosSmooth = 2.0f;
    public float rotDeadzoneDeg = 8f;
    public float posDeadzoneM = 0.02f;

    [Header("Final Position Smoothing")]
    public float positionSmooth = 0.2f;

    [Header("Y Lock")]
    public bool lockWorldY = true;
    public float addHeight = 2.2f;

    float _smoothedViewportX;
    float _fixedY;
    bool _yLocked;

    Vector3 _camPosSmoothed;
    Quaternion _camRotSmoothed;

    void Start()
    {
        if (!viewCamera) viewCamera = Camera.main;
        _smoothedViewportX = Mathf.Clamp(viewportX, viewportXMin, viewportXMax);

        if (!viewCamera) return;
        _camPosSmoothed = viewCamera.transform.position;
        _camRotSmoothed = viewCamera.transform.rotation;

        SnapNow();
        _fixedY = transform.position.y + addHeight;
        _yLocked = lockWorldY;
    }

    void LateUpdate()
    {
        if (!viewCamera) viewCamera = Camera.main;
        if (!viewCamera) return;

        float dt = Time.deltaTime;

        Quaternion camRot = viewCamera.transform.rotation;
        float ang = Quaternion.Angle(_camRotSmoothed, camRot);
        if (ang > rotDeadzoneDeg)
        {
            float tRot = 1f - Mathf.Exp(-Mathf.Max(0.0001f, headRotSmooth) * dt);
            _camRotSmoothed = Quaternion.Slerp(_camRotSmoothed, camRot, tRot);
        }

        Vector3 camPos = viewCamera.transform.position;
        float posDelta = Vector3.Distance(_camPosSmoothed, camPos);
        if (posDelta > posDeadzoneM)
        {
            float tPosCam = 1f - Mathf.Exp(-Mathf.Max(0.0001f, bodyPosSmooth) * dt);
            _camPosSmoothed = Vector3.Lerp(_camPosSmoothed, camPos, tPosCam);
        }

        float z = cameraSpaceZ;

        float vFovRad = viewCamera.fieldOfView * Mathf.Deg2Rad;
        float hFovRad = 2f * Mathf.Atan(Mathf.Tan(vFovRad * 0.5f) * viewCamera.aspect);

        float nx = Mathf.Clamp(viewportX, viewportXMin, viewportXMax) - 0.4f;
        float ny = viewportY - 0.5f;

        float offX = (nx * 2f) * z * Mathf.Tan(hFovRad * 0.5f);
        float offY = (ny * 2f) * z * Mathf.Tan(vFovRad * 0.5f);

        Vector3 fwd = _camRotSmoothed * Vector3.forward;
        Vector3 right = _camRotSmoothed * Vector3.right;
        Vector3 up = _camRotSmoothed * Vector3.up;

        Vector3 target = _camPosSmoothed
               + fwd * z
               + right * (offX + leftOffset)
               + up * offY;

        if (_yLocked) target.y = _fixedY;

        float tFinal = 1f - Mathf.Exp(-Mathf.Max(0.0001f, positionSmooth) * dt);
        transform.position = Vector3.Lerp(transform.position, target, tFinal);

        Vector3 lookFrom = _camPosSmoothed;
        Vector3 look = lookFrom - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude > 1e-6f)
        {
            Quaternion offset = Quaternion.AngleAxis(yawLookOffsetDeg, Vector3.up);
            Vector3 rotatedLook = offset * look.normalized;
            transform.rotation = Quaternion.LookRotation(rotatedLook, Vector3.up);
        }
    }

    [ContextMenu("Snap Now")]
    public void SnapNow()
    {
        if (!viewCamera) viewCamera = Camera.main;
        if (!viewCamera) return;

        float vFovRad = viewCamera.fieldOfView * Mathf.Deg2Rad;
        float hFovRad = 2f * Mathf.Atan(Mathf.Tan(vFovRad * 0.5f) * viewCamera.aspect);
        float z = Mathf.Max(viewCamera.nearClipPlane + 0.08f, cameraSpaceZ);

        float nx = Mathf.Clamp(viewportX, viewportXMin, viewportXMax) - 0.5f;
        float ny = viewportY - 0.5f;

        float offX = (nx * 2f) * z * Mathf.Tan(hFovRad * 0.5f);
        float offY = (ny * 2f) * z * Mathf.Tan(vFovRad * 0.5f);

        if (_camPosSmoothed == Vector3.zero) _camPosSmoothed = viewCamera.transform.position;
        if (_camRotSmoothed == Quaternion.identity) _camRotSmoothed = viewCamera.transform.rotation;

        Vector3 fwd = _camRotSmoothed * Vector3.forward;
        Vector3 right = _camRotSmoothed * Vector3.right;
        Vector3 up = _camRotSmoothed * Vector3.up;

        Vector3 target = _camPosSmoothed + fwd * z + right * offX + up * offY;

        if (lockWorldY)
        {
            if (!_yLocked) { _fixedY = target.y + addHeight; _yLocked = true; }
            target.y = _fixedY;
        }

        transform.position = target;
    }
}
