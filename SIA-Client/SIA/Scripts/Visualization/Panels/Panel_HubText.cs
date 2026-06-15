using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Newtonsoft.Json.Linq; // : JSON 

public class Panel_HubText : MonoBehaviour
{
    [Header("Optional:    ")]
    public Font defaultFont;

    public Text speechText;   //  ()
    public Text hubText;      //  insight()

    public void SetHubText(string text)
    {
        if (hubText != null)
            hubText.text = "Action Guideline: " + text;
    }
    public void DisplayUserSpeech(string userText)
    {
        if (speechText != null)
            speechText.text = userText;
    }


    //  insight   
    public void DisplayFeedForwardInsight(string json)
    {
        try
        {
            var root = JObject.Parse(json);

            //  finalFeedforward = "c1" or "c2"
            string key = (string?)root.SelectToken("$.module_spec.result.updated_module_spec.dataComposer.feedforward.finalFeedforward");

            //  key candidates  insight 
            var cTok = root.SelectToken($"$.module_spec.result.updated_module_spec.dataComposer.feedforward.candidates.{key}");

            string insight = (string?)cTok?["insight"] ?? "No insight";
            SetHubText(insight);
        }
        catch
        {
            SetHubText("FeedForward insight  ");
        }
    }


    public static Panel_HubText CreateHudPanelText(GameObject panel)
    {
        var hubTextComp = panel.GetComponent<Panel_HubText>();
        if (hubTextComp != null)
            return hubTextComp;

        hubTextComp = panel.AddComponent<Panel_HubText>();
        return hubTextComp;
    }

    private void Awake()
    {
        // :  
        if (speechText == null)
        {
            var speechGO = new GameObject("SpeechText", typeof(RectTransform), typeof(Text));
            speechGO.transform.SetParent(transform, false);
            speechText = speechGO.GetComponent<Text>();

            // // 
            speechText.font = ResolveUIFont(defaultFont);
            speechText.fontSize = 16;
            speechText.color = Color.black;
            speechText.alignment = TextAnchor.UpperLeft;
            speechText.horizontalOverflow = HorizontalWrapMode.Wrap;
            speechText.verticalOverflow = VerticalWrapMode.Overflow;
            speechText.raycastTarget = false;

            var rt = speechGO.GetComponent<RectTransform>();
            //  ,  
            rt.anchorMin = new Vector2(0.05f, 0.60f); //  40% 
            rt.anchorMax = new Vector2(0.95f, 0.90f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            speechText.text = "Say what you want...";
        }

        // :  insight
        if (hubText == null)
        {
            var textGO = new GameObject("HubText", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(transform, false);
            hubText = textGO.GetComponent<Text>();

            //   
            hubText.font = ResolveUIFont(defaultFont);
            hubText.fontSize = 16;
            hubText.color = Color.black;
            hubText.alignment = TextAnchor.UpperLeft;
            hubText.horizontalOverflow = HorizontalWrapMode.Wrap;
            hubText.verticalOverflow = VerticalWrapMode.Overflow;
            hubText.raycastTarget = false;

            var rt = textGO.GetComponent<RectTransform>();
            //  ,  (10%) 
            rt.anchorMin = new Vector2(0.05f, 0.10f); //  50% 
            rt.anchorMax = new Vector2(0.95f, 0.55f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            //   ,  !
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.dynamicPixelsPerUnit = 4f; // () ,  1
                }
            }
            //     
            string[] hubPlaceholders = new string[]
            {
                "Explore the cluster to identify representative data points and understand typical characteristics.",
                "Navigate to the cluster's edge to spot outliers and assess how they differ from the majority.",
                "Move into the chart to explore the cluster around the center point, revealing detailed characteristics.",
                "Navigate to the edge of the cluster to identify outliers and assess how they differ.",
                "Move to the plane in front of you to explore data points with lower values, which may reveal patterns in older properties.",
                "Navigate to the plane in front of you to focus on properties with higher values, which could highlight premium features.",
                "Explore the plane in front of you to closely examine older properties, which may reveal unique historical features.",
                "Navigate to the plane in front of you to focus on high-value properties, which could highlight luxury features.",
                "Navigate to the plane in front of you to focus on high-value properties. This could highlight luxury features.",
                "Explore the plane in front of you to discover properties with the highest values. This reveals trends in luxury housing.",
                "Move to the plane in front of you to examine newly built properties. This highlights modern architectural trends.",
                "Navigate to the plane in front of you to focus on older properties. This uncovers historical architectural styles.",
                "Move to the plane in front of you to explore properties with the largest areas. This reveals potential for development or expansion.",
                "Explore the plane in front of you to uncover properties with the lowest values. This highlights budget-friendly options.",
                "Navigate to the plane in front of you to examine properties with the highest number of rooms. This offers insights into spacious living arrangements.",
                "Navigate to the plane in front of you to focus on properties with the smallest areas. This highlights compact living spaces.",
                "Explore the plane in front of you to discover properties with the highest overall condition. This provides insights into well-maintained homes.",
                "Navigate to the plane in front of you to explore properties with the largest areas. This reveals potential for development or expansion.",
                "Navigate to the plane in front of you to focus on properties with the smallest areas. This highlights compact living spaces.",
                "Explore the plane in front of you to discover premium properties. This highlights luxury features.",
                "Navigate to the plane in front of you to uncover older or more compact properties. This reveals unique characteristics."
            };

            //  
            int randIndex = Random.Range(0, hubPlaceholders.Length);
            SetHubText(hubPlaceholders[randIndex]);   //    

        }

        Debug.Log("[Panel_HubText] Awake, font=" + (hubText.font ? hubText.font.name : "null"));
    }

    // ----------    ----------
    private static Font ResolveUIFont(Font inspectorFont)
    {
        if (inspectorFont != null) return inspectorFont;
        try
        {
            var builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtin != null) return builtin;
        }
        catch { }

        string[] candidates = {
            "Arial", "Segoe UI", "Helvetica", "Roboto",
            "Apple SD Gothic Neo", "Noto Sans CJK KR", "Malgun Gothic", "NanumGothic"
        };

        try
        {
            var found = candidates.FirstOrDefault(FontExistsOnOS);
            if (!string.IsNullOrEmpty(found))
                return Font.CreateDynamicFontFromOSFont(found, 22);
        }
        catch { }

        Debug.LogWarning("[Panel_HubText]   :  Font  .");
        return null;
    }

    private static bool FontExistsOnOS(string familyName)
    {
        try
        {
            var f = Font.CreateDynamicFontFromOSFont(new[] { familyName }, 16);
            return f != null;
        }
        catch { return false; }
    }
    
    // Panel_HubText      .

    // ================== Legend ==================
    private Transform legendPanelParent;

    public void InitLegendPanel()
    {
        if (legendPanelParent != null) return;

        var legendGO = new GameObject("LegendPanel", typeof(RectTransform));
        legendGO.transform.SetParent(transform, false);
        var legendRT = legendGO.GetComponent<RectTransform>();

        //   
        legendRT.anchorMin = new Vector2(0, 1f);
        legendRT.anchorMax = new Vector2(1f, 1f);
        legendRT.pivot     = new Vector2(0.5f, 1f);
        legendRT.sizeDelta = new Vector2(0, 40);
        legendRT.anchoredPosition = Vector2.zero;

        legendPanelParent = legendGO.transform;
    }
    
}