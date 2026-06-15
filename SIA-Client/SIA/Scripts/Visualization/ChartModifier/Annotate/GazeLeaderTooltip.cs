using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class GazeLeaderTooltip : MonoBehaviour
{
    private const float DEF_LEADER_LENGTH = 0.35f;
    private const float DEF_LEADER_WIDTH  = 0.002f;
    private static readonly Color DEF_LEADER_COLOR = Color.black;
    private const bool  DEF_LEADER_DIR_REL_HEAD = true;

    private static readonly Vector3 DEF_TOOLTIP_OFFSET = Vector3.zero;
    private const int    DEF_TOOLTIP_FONT_SIZE     = 40;
    private const float  DEF_TOOLTIP_CHAR_SIZE     = 0.01f;
    private static readonly Color DEF_TOOLTIP_COLOR = Color.black;
    private static Font  s_tooltipFont             = null;

    private static string[] s_columns =
    {
    };
    private const bool   DEF_AUTO_POS_UNDER_MAIN = true;
    private static readonly Vector3 DEF_KV_BASE_OFFSET = Vector3.zero;
    private static float s_kvExtraSpacing = -0.03f;
    private static float s_kvScale        = 1.0f;
    private static int   s_kvFontSize     = 25;
    private static float s_kvCharSize     = 0.008f;
    private static readonly Color DEF_KV_COLOR = new Color(0.1f, 0.1f, 0.1f);
    private static Font  s_kvFontOverride = null;
    private static float s_kvValueColumnX = 0.3f;

    private const bool DEF_FACE_CAMERA = true;

    private const bool USE_WORLD_PLACEMENT = true;

    private LineRenderer _leader;
    private GameObject _tooltipGO;
    private TextMesh _tooltipText;

    private GameObject _kvGO;
    private GameObject _kvKeyGO, _kvValGO;
    private TextMesh _kvKeyText, _kvValText;

    private readonly Dictionary<string, string> _kvValues = new Dictionary<string, string>();

    private Vector3 _lastP0, _lastP1;
    private string _lastText = "";
    private bool _hasAnyDrawn = false;

#if UNITY_EDITOR
    private bool _fontWarned;
#endif

    public static void SetLayout(float? kvValueColumnX = null, float? extraSpacing = null, float? kvScale = null)
    {
        if (kvValueColumnX.HasValue) s_kvValueColumnX = kvValueColumnX.Value;
        if (extraSpacing.HasValue)   s_kvExtraSpacing = extraSpacing.Value;
        if (kvScale.HasValue)        s_kvScale        = Mathf.Max(0.01f, kvScale.Value);
    }

    public static void SetFonts(Font tooltipFont = null, Font kvFont = null, int? kvFontSize = null, float? kvCharSize = null)
    {
        if (tooltipFont != null) s_tooltipFont = tooltipFont;
        if (kvFont != null)      s_kvFontOverride = kvFont;
        if (kvFontSize.HasValue) s_kvFontSize = Mathf.Max(1, kvFontSize.Value);
        if (kvCharSize.HasValue) s_kvCharSize = Mathf.Max(0.0001f, kvCharSize.Value);
    }

    public static void SetColumns(params string[] columns)
    {
        if (columns != null && columns.Length > 0) s_columns = columns;
    }

    void OnEnable()
    {
        GazeSelector.OnGazeHover += HandleGazeHover;
        EnsureVisuals();
    }

    void OnDisable()
    {
        GazeSelector.OnGazeHover -= HandleGazeHover;
    }

    void LateUpdate()
    {
        if (_leader == null || _tooltipGO == null) return;

        if (_hasAnyDrawn)
        {
            _leader.enabled = true;
            _leader.positionCount = 2;
            _leader.SetPosition(0, _lastP0);
            _leader.SetPosition(1, _lastP1);
            _leader.startWidth = _leader.endWidth = DEF_LEADER_WIDTH;
            _leader.startColor = _leader.endColor = DEF_LEADER_COLOR;

            _tooltipGO.SetActive(true);
            _tooltipGO.transform.position = _lastP1 + DEF_TOOLTIP_OFFSET;
            _tooltipText.text = _lastText;
            _tooltipText.color = DEF_TOOLTIP_COLOR;
            _tooltipText.fontSize = DEF_TOOLTIP_FONT_SIZE;
            _tooltipText.characterSize = DEF_TOOLTIP_CHAR_SIZE;
            if (s_tooltipFont != null && _tooltipText.font != s_tooltipFont)
                _tooltipText.font = s_tooltipFont;

            if (DEF_FACE_CAMERA && Camera.main != null)
            {
                var cam = Camera.main.transform;
                var rot = Quaternion.LookRotation(_tooltipGO.transform.position - cam.position, Vector3.up);
                _tooltipGO.transform.rotation = rot;
            }

            if (_kvGO != null)
            {
                _kvGO.SetActive(true);
                ApplyKVStyle();

                float mainHeight = 0f;
                var r = _tooltipText.GetComponent<Renderer>();
                if (r != null && r.enabled) mainHeight = r.bounds.size.y;

                Vector3 local = DEF_KV_BASE_OFFSET;
                if (DEF_AUTO_POS_UNDER_MAIN)
                {
                    local.y -= mainHeight;
                    local.y -= s_kvExtraSpacing;
                }

                if (USE_WORLD_PLACEMENT)
                {
                    Vector3 world = _tooltipGO.transform.TransformPoint(local);
                    Quaternion worldRot = _tooltipGO.transform.rotation;

                    Transform prevParent = _kvGO.transform.parent;
                    _kvGO.transform.SetParent(null, true);
                    _kvGO.transform.position = world;
                    _kvGO.transform.rotation = worldRot;
                    _kvGO.transform.SetParent(prevParent, true);
                }
                else
                {
                    _kvGO.transform.localPosition = local;
                }

                BuildKVColumns(out string keys, out string values);
                _kvKeyText.text = keys;
                _kvValText.text = values;

                _kvKeyGO.transform.localPosition = Vector3.zero;
                _kvValGO.transform.localPosition = new Vector3(s_kvValueColumnX, 0f, 0f);
            }

#if UNITY_EDITOR
            if (!_fontWarned)
            {
                Font f1 = s_tooltipFont != null ? s_tooltipFont : _tooltipText.font;
                Font f2 = s_kvFontOverride != null ? s_kvFontOverride : _kvKeyText.font;
                if (f1 != null && f2 != null && !(f1.dynamic && f2.dynamic))
                {
                    Debug.LogWarning("[GazeLeaderTooltip] Dynamic fonts are recommended for better readability at small font sizes.");
                    _fontWarned = true;
                }
            }
#endif
        }
        else
        {
            _leader.enabled = false;
            _tooltipGO.SetActive(false);
            if (_kvGO) _kvGO.SetActive(false);
        }
    }

    private void HandleGazeHover(GazeSelector.GazeHoverData data)
    {
        EnsureVisuals();

        Vector3 dir = DEF_LEADER_DIR_REL_HEAD && data.head != null
            ? (data.head.right + data.head.up).normalized
            : (transform.right + transform.up).normalized;

        _lastP0 = data.targetCenterWorld;
        _lastP1 = _lastP0 + dir * DEF_LEADER_LENGTH;

        string text = $"ID: {data.id}";

        bool hasAnyAxis =
            !(string.IsNullOrEmpty(data.xName) && string.IsNullOrEmpty(data.yName) && string.IsNullOrEmpty(data.zName)) ||
            !(string.IsNullOrEmpty(data.xValue) && string.IsNullOrEmpty(data.yValue) && string.IsNullOrEmpty(data.zValue));

        if (hasAnyAxis)
        {
            string xLine = string.IsNullOrEmpty(data.xName) ? "" : $"{data.xName}: {data.xValue}";
            string yLine = string.IsNullOrEmpty(data.yName) ? "" : $"{data.yName}: {data.yValue}";
            string zLine = string.IsNullOrEmpty(data.zName) ? "" : $"{data.zName}: {data.zValue}";
            if (!string.IsNullOrEmpty(xLine)) text += $"\n{xLine}";
            if (!string.IsNullOrEmpty(yLine)) text += $"\n{yLine}";
            if (!string.IsNullOrEmpty(zLine)) text += $"\n{zLine}";
        }

        _lastText = text;
        _hasAnyDrawn = true;
    }

    private void EnsureVisuals()
    {
        if (_leader == null)
        {
            var go = new GameObject("[GazeLeaderTooltip] LeaderLine");
            go.transform.SetParent(transform, false);
            _leader = go.AddComponent<LineRenderer>();
            _leader.useWorldSpace = true;
            _leader.material = new Material(Shader.Find("Sprites/Default"));
            _leader.numCornerVertices = 4;
            _leader.numCapVertices = 4;
            _leader.alignment = LineAlignment.View;
            _leader.enabled = false;
        }

        if (_tooltipGO == null)
        {
            _tooltipGO = new GameObject("[GazeLeaderTooltip] Tooltip");
            _tooltipGO.transform.SetParent(transform, false);
            _tooltipText = _tooltipGO.AddComponent<TextMesh>();
            _tooltipText.fontSize = DEF_TOOLTIP_FONT_SIZE;
            _tooltipText.characterSize = DEF_TOOLTIP_CHAR_SIZE;
            _tooltipText.color = DEF_TOOLTIP_COLOR;
            _tooltipText.anchor = TextAnchor.MiddleLeft;
            _tooltipText.alignment = TextAlignment.Left;
            if (s_tooltipFont != null) _tooltipText.font = s_tooltipFont;
            _tooltipGO.SetActive(false);
        }

        if (_kvGO == null)
        {
            _kvGO = new GameObject("[GazeLeaderTooltip] KV");
            _kvGO.transform.SetParent(_tooltipGO.transform, false);

            _kvKeyGO = new GameObject("[GazeLeaderTooltip] KV-Keys");
            _kvKeyGO.transform.SetParent(_kvGO.transform, false);
            _kvKeyText = _kvKeyGO.AddComponent<TextMesh>();

            _kvValGO = new GameObject("[GazeLeaderTooltip] KV-Vals");
            _kvValGO.transform.SetParent(_kvGO.transform, false);
            _kvValText = _kvValGO.AddComponent<TextMesh>();

            ApplyKVStyle();
            _kvGO.SetActive(false);
        }
    }

    public void SetValue(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (s_columns != null && s_columns.Length > 0 && !s_columns.Contains(key)) return;
        _kvValues[key] = string.IsNullOrEmpty(value) ? "-" : value;
    }

    public void SetValues(IDictionary<string, string> keyValues)
    {
        if (keyValues == null) return;
        foreach (var kv in keyValues) SetValue(kv.Key, kv.Value);
    }

    public void ClearLast()
    {
        _hasAnyDrawn = false;
        if (_leader) _leader.enabled = false;
        if (_tooltipGO) _tooltipGO.SetActive(false);
        if (_kvGO) _kvGO.SetActive(false);
    }

    private void BuildKVColumns(out string keys, out string values)
    {
        if (s_columns == null || s_columns.Length == 0)
        {
            keys = values = "";
            return;
        }

        var sbK = new StringBuilder(s_columns.Length * 12);
        var sbV = new StringBuilder(s_columns.Length * 12);

        for (int i = 0; i < s_columns.Length; i++)
        {
            string key = s_columns[i] ?? "";
            string val = (i == 0)
                ? " "
                : (_kvValues.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v)) ? v : "-";

            sbK.Append(key);

            if (i < s_columns.Length - 1)
            {
                sbK.Append('\n');
                sbV.Append('\n');
            }
        }

        keys = sbK.ToString();
        values = sbV.ToString();
    }

    private void ApplyKVStyle()
    {
        if (_kvKeyText != null)
        {
            _kvKeyText.fontSize = s_kvFontSize;
            _kvKeyText.characterSize = s_kvCharSize;
            _kvKeyText.anchor = TextAnchor.UpperLeft;
            _kvKeyText.alignment = TextAlignment.Left;
            _kvKeyText.color = DEF_KV_COLOR;
            if (s_kvFontOverride != null && _kvKeyText.font != s_kvFontOverride) _kvKeyText.font = s_kvFontOverride;
        }

        if (_kvValText != null)
        {
            _kvValText.fontSize = s_kvFontSize;
            _kvValText.characterSize = s_kvCharSize;
            _kvValText.anchor = TextAnchor.UpperLeft;
            _kvValText.alignment = TextAlignment.Left;
            _kvValText.color = DEF_KV_COLOR;
            if (s_kvFontOverride != null && _kvValText.font != s_kvFontOverride) _kvValText.font = s_kvFontOverride;
        }

        if (_kvGO != null) _kvGO.transform.localScale = Vector3.one * s_kvScale;
    }
}
