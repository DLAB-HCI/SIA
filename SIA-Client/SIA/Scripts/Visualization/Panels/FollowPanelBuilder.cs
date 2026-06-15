using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class FollowPanelBuilder : MonoBehaviour
{
    [Header("Head Transform")]
    public Transform headTransform;

    [Header("HUD")]
    [SerializeField] Vector2 hudSize = new Vector2(800, 200);
    [SerializeField] float hudDistance = 0.6f;
    [SerializeField] float hudViewOffsetX = 0.0f;
    [SerializeField] float hudViewOffsetY = -0.59f;
    [SerializeField] float hudRotSmooth = 1.2f;
    [SerializeField] float hudPosSmooth = 6f;
    [SerializeField] float hudDeadzoneDeg = 8f;


    [Header("Wide Panel")]
    [SerializeField] Vector2 wideSize = new Vector2(1400 * 1.5f, 320 * 10.5f);
    [SerializeField] float wideViewportX = 0.50f;
    [SerializeField] float wideViewportY = 0.20f;
    [SerializeField] float wideCameraSpaceZ = 1.20f;
    [SerializeField] float wideYawToViewportGain = 0.15f;
    [SerializeField] float wideViewportXMin = 0.15f;
    [SerializeField] float wideViewportXMax = 0.85f;
    [SerializeField] float widePosSmooth = 8f;
    [SerializeField] float wideRotSmooth = 12f;


    [Header("Style")]
    [SerializeField] Color panelColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] Color borderColor = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] float borderThickness = 3f;

    private GameObject hudPanelRoot;
    private InputAction _toggleHud;
    private Panel_HubText _hubTextComp;

    void Awake()
    {
        _toggleHud = new InputAction("ToggleHUD", InputActionType.Button);

        _toggleHud.AddBinding("<XRController>{RightHand}/primaryButton");
        _toggleHud.AddBinding("<XRController>{LeftHand}/primaryButton");

        _toggleHud.AddBinding("<OculusTouchController>{RightHand}/primaryButton");
        _toggleHud.AddBinding("<OculusTouchController>{LeftHand}/primaryButton");

        _toggleHud.AddBinding("<OpenXRInput>/input/a/click");
        _toggleHud.AddBinding("<OpenXRInput>/input/x/click");


        _toggleHud.performed += ctx =>
        {
            Debug.Log($"[ToggleHUD] performed by: {ctx.control?.device?.displayName} / {ctx.control?.path}");
            OnToggleHud(ctx);
        };
    }

    void OnEnable()
    {
        _toggleHud.Enable();

        var devices = string.Join(", ", InputSystem.devices);
        Debug.Log($"[Input] Devices: {devices}");
    }

    void OnDisable()
    {
        _toggleHud.performed -= OnToggleHud;
        _toggleHud.Disable();
    }

    private void OnToggleHud(InputAction.CallbackContext ctx)
    {
        if (hudPanelRoot == null) return;
        var target = _hubTextComp.hubText.gameObject;
        target.SetActive(!target.activeSelf);
        Debug.Log($"[FollowPanelBuilder] HUD state: {(hudPanelRoot.activeSelf ? "ON" : "OFF")}");
    }
    void Start()
    {
        Transform head = headTransform != null ? headTransform : Camera.main?.transform;
        if (!head)
        {
            Debug.LogWarning("[FollowPanelBuilder] headTransform is not assigned.");
            return;
        }

        hudPanelRoot = BuildHudPanel(head);

        BuildWidePanel(head);
    }

    GameObject BuildHudPanel(Transform head)
    {
        var root  = CreateCanvasRoot("HUDPanel_Root", hudSize);
        var parent = root.transform.Find("Content");

        var panel = CreatePanelWithBorder(parent, "HUDPanel", hudSize);

        var hudUI  = Panel_HudUI.CreateHudPanelUI(panel);
        var hudTxt = Panel_HubText.CreateHudPanelText(panel);

        _hubTextComp = hudTxt;

        var follower = root.AddComponent<Panel_HUDFollower>();
        follower.headTransform   = head;
        follower.distance        = hudDistance;
        follower.viewOffsetX     = hudViewOffsetX;
        follower.viewOffsetY     = hudViewOffsetY;
        follower.rotationSmooth  = hudRotSmooth;
        follower.positionSmooth  = hudPosSmooth;
        follower.deadzoneDegrees = hudDeadzoneDeg;

        InitHudTransform(root.transform, head, follower);

        return root;
    }

    void BuildWidePanel(Transform head)
{
    var root = CreateCanvasRoot("WidePanel_Root", wideSize);
        var parent = root.transform.Find("Content");
    var widePanelGO = CreatePanelWithBorder(parent, "WidePanel", wideSize);

    var rtWideRoot  = root.GetComponent<RectTransform>();
    var wideRT      = widePanelGO.GetComponent<RectTransform>();

    float aspect  = 21f / 9f;
    float widthPx = 2100f;
    wideRT.sizeDelta   = new Vector2(widthPx, widthPx / aspect);
    rtWideRoot.sizeDelta = wideRT.sizeDelta;

    var arf = widePanelGO.AddComponent<AspectRatioFitter>();
    arf.aspectMode  = AspectRatioFitter.AspectMode.WidthControlsHeight;
    arf.aspectRatio = aspect;


    var follower = root.AddComponent<Panel_HorizontalFollower>();
    follower.headTransform = head;
    follower.viewCamera    = Camera.main;
    follower.viewportX     = wideViewportX;
    follower.viewportY     = wideViewportY;
    follower.positionSmooth = widePosSmooth;
    follower.viewportXMin   = wideViewportXMin;
    follower.viewportXMax   = wideViewportXMax;
    
        widePanelGO.AddComponent<Panel_HorizontalText>();

    InitWideTransform(root.transform, head, follower);
}



        GameObject CreateCanvasRoot(string name, Vector2 size)
        {
            var go = new GameObject(name,
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            go.transform.localScale = Vector3.one * 0.0015f;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(go.transform, false);

            content.transform.localScale = new Vector3(-1f, 1f, 1f);

            return go;
        }


    GameObject CreatePanelWithBorder(Transform parent, string name, Vector2 size)
    {
        var panelGO = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(parent, false);
        var rt = panelGO.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        var img = panelGO.GetComponent<Image>();
        img.color = panelColor;

        if (borderThickness > 0f)
        {
            var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(panelGO.transform, false);
            var bRT = border.GetComponent<RectTransform>();
            bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;
            bRT.offsetMin = new Vector2(-borderThickness, -borderThickness);
            bRT.offsetMax = new Vector2(borderThickness, borderThickness);
            var bImg = border.GetComponent<Image>();
            bImg.color = borderColor;
            bImg.raycastTarget = false;
        }

        return panelGO;
    }


    void InitHudTransform(Transform root, Transform head, Panel_HUDFollower f)
    {
        var r = head.rotation;
        Vector3 pos =
            head.position +
            (r * Vector3.forward) * Mathf.Max(0.05f, f.distance) +
            (r * Vector3.right)   * f.viewOffsetX +
            (r * Vector3.up)      * f.viewOffsetY;

        root.position = pos;

        root.rotation = Quaternion.LookRotation(head.position - pos, r * Vector3.up);

        Vector3 toCam = (head.position - root.position).normalized;
        if (Vector3.Dot(-root.forward, toCam) < 0f)
            root.Rotate(0f, 180f, 0f, Space.Self);

        var canvas = root.GetComponent<Canvas>();
        if (canvas && canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
            canvas.worldCamera = Camera.main;
    }

    void InitWideTransform(Transform root, Transform head, Panel_HorizontalFollower f)
    {
        var cam = f.viewCamera ? f.viewCamera : Camera.main;
        if (!cam) return;

        float z = Mathf.Max(cam.nearClipPlane + 0.08f, f.cameraSpaceZ);
        Vector3 worldTarget = cam.ViewportToWorldPoint(new Vector3(f.viewportX, f.viewportY, z));

        root.position = worldTarget;

        Vector3 look = head.position - worldTarget; 
        look.y = 0f;
        if (look.sqrMagnitude > 1e-6f)
            root.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
    }

}
