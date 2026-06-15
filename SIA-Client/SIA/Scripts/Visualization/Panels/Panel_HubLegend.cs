using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;  // JSON 
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;

public class Panel_HubLegend : MonoBehaviour
{
    private Transform legendPanelParent;

    private const string SpecCompilerRelativePath = "SIA/Scripts/Visualization/SpecCompiler";

    private static string GetCurrentChartSpecPath()
    {
        return Path.Combine(Application.dataPath, SpecCompilerRelativePath, "chartSpec_current.json");
    }

   //     
private void AddLegendTitle(string fieldName)
{
    var titleGO = new GameObject("LegendTitle", typeof(RectTransform), typeof(Text));
    titleGO.transform.SetParent(legendPanelParent, false);

    var titleTxt = titleGO.GetComponent<Text>();
    titleTxt.text = fieldName;
    titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    titleTxt.fontSize = 20;
    titleTxt.color = Color.black;
    titleTxt.alignment = TextAnchor.MiddleLeft;

    var titleRT = titleGO.GetComponent<RectTransform>();
    titleRT.sizeDelta = new Vector2(400, 30);
    titleRT.anchorMin = new Vector2(0, 1);
    titleRT.anchorMax = new Vector2(0, 1);
    titleRT.pivot = new Vector2(0, 1);
    titleRT.anchoredPosition = new Vector2(20, 40);//  
}

public void RefreshLegendFromSpec(string jsonPath)
{
    if (!System.IO.File.Exists(jsonPath))
    {
        Debug.LogError("[Legend] Spec JSON not found: " + jsonPath);
        return;
    }

    string json = System.IO.File.ReadAllText(jsonPath);
    var root = JObject.Parse(json);

    string colorField = root.SelectToken("$.encoding.color.field")?.ToString();
    if (!string.IsNullOrEmpty(colorField) && colorField.StartsWith("datum."))
        colorField = colorField.Replace("datum.", "");

    var conditions = root.SelectToken("$.encoding.color.condition") as JArray;

    InitLegendPanel();
    foreach (Transform child in legendPanelParent)
        Destroy(child.gameObject);

    //     
    if (!string.IsNullOrEmpty(colorField))
    {
        AddLegendTitle(colorField);
    }

    if (conditions == null || conditions.Count == 0)
    {
        AddLegendEntry(
            new Color(0.7294118f, 0.6901961f, 0.6745098f),
            "All Houses",
            new Vector2(20, -40) //  
        );
        return;
    }

    int index = 0;
    foreach (var cond in conditions)
    {
        string test = cond.Value<string>("test");
        string hex = cond.Value<string>("value");
        string label = ExtractCategoryLabel(test);
        Color color;
        ColorUtility.TryParseHtmlString(hex, out color);

        //    -40 
        AddLegendEntry(color, label, new Vector2(20 + index * 180, -40));
        index++;
    }
}

    private string ExtractStringValue(string condition)
    {
        // "datum.Exterior_Condition == 'Poor'"  "Poor"
        string[] parts = condition.Split(new[] { "==", "!=" }, System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            return parts[1].Trim().Trim('\'', '"');
        }
        return condition;
    }

public string ExtractCategoryLabel(string test)
{
    test = test.Trim();

    //    (AND , OR )
    if (test.Contains("&&"))
    {
        // "datum['Basement(sq)'] >= 0 && datum['Basement(sq)'] < 500"
        //         
        var parts = test.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            string condition1 = parts[0].Trim();
            string condition2 = parts[1].Trim();
            
            //    
            string value1 = ExtractValueFromCondition(condition1);
            string value2 = ExtractValueFromCondition(condition2);
            string op1 = ExtractOperatorFromCondition(condition1);
            string op2 = ExtractOperatorFromCondition(condition2);
            
            //   
            if (op1 == ">=" && op2 == "<")
                return $"{value1} - {value2}";
            else if (op1 == ">=" && op2 == "<=")
                return $"{value1} - {value2}";
            else
                return $"{op1} {value1} & {op2} {value2}";
        }
    }

    //   
    string[] operators = { ">=", "<=", "==", "!=", ">", "<" };
    foreach (var op in operators)
    {
        if (test.Contains(op))
        {
            string value = ExtractValueFromCondition(test);
            return $"{op} {value}";
        }
    }

    return test;
}

private string ExtractValueFromCondition(string condition)
{
    // "datum['Basement(sq)'] >= 0"  "0"
    string[] operators = { ">=", "<=", "==", "!=", ">", "<" };
    foreach (var op in operators)
    {
        if (condition.Contains(op))
        {
            var parts = condition.Split(new[] { op }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                return parts[1].Trim().Trim('\'', '"');
            }
        }
    }
    return condition;
}

private string ExtractOperatorFromCondition(string condition)
{
    // "datum['Basement(sq)'] >= 0"  ">="
    string[] operators = { ">=", "<=", "==", "!=", ">", "<" };
    foreach (var op in operators)
    {
        if (condition.Contains(op))
        {
            return op;
        }
    }
    return "";
}
    public void InitLegendPanel()
    {
        if (legendPanelParent != null) return;

        var legendGO = new GameObject("LegendPanel", typeof(RectTransform));
        legendGO.transform.SetParent(transform, false);

        var legendRT = legendGO.GetComponent<RectTransform>();
        legendRT.anchorMin = new Vector2(0, 1f);
        legendRT.anchorMax = new Vector2(1f, 1f);
        legendRT.pivot = new Vector2(0.5f, 1f);
        legendRT.sizeDelta = new Vector2(0, 40);

        //     
        legendRT.anchoredPosition = new Vector2(60, 40);

        legendPanelParent = legendGO.transform;
    }


public void AddLegendEntry(Color color, string label, Vector2 anchoredPos)
{
    if (!legendPanelParent) InitLegendPanel();

    //  GO ()
    var boxParent = new GameObject("ColorBoxWrapper", typeof(RectTransform));
    boxParent.transform.SetParent(legendPanelParent, false);
    var parentRT = boxParent.GetComponent<RectTransform>();
    parentRT.sizeDelta = new Vector2(21, 21); //   
    parentRT.anchorMin = new Vector2(0, 1);
    parentRT.anchorMax = new Vector2(0, 1);
    parentRT.pivot     = new Vector2(0, 1);
    parentRT.anchoredPosition = anchoredPos;

    //   ( )
    var borderGO = new GameObject("Border", typeof(RectTransform), typeof(Image));
    borderGO.transform.SetParent(boxParent.transform, false);
    var borderImg = borderGO.GetComponent<Image>();
    borderImg.color = Color.white; // 

    var borderRT = borderGO.GetComponent<RectTransform>();
    borderRT.sizeDelta = parentRT.sizeDelta;
    borderRT.anchorMin = Vector2.zero;
    borderRT.anchorMax = Vector2.one;
    borderRT.offsetMin = Vector2.zero;
    borderRT.offsetMax = Vector2.zero;

    //    ()
    var boxGO = new GameObject("ColorBox", typeof(RectTransform), typeof(Image));
    boxGO.transform.SetParent(boxParent.transform, false);
    var boxImg = boxGO.GetComponent<Image>();
    boxImg.color = color;

    var boxRT = boxGO.GetComponent<RectTransform>();
    boxRT.sizeDelta = new Vector2(20, 20); //  
    boxRT.anchorMin = new Vector2(0.5f, 0.5f);
    boxRT.anchorMax = new Vector2(0.5f, 0.5f);
    boxRT.pivot     = new Vector2(0.5f, 0.5f);
    boxRT.anchoredPosition = Vector2.zero;

    // 
    var textGO = new GameObject("LegendLabel", typeof(RectTransform), typeof(Text));
    textGO.transform.SetParent(legendPanelParent, false);
    var txt = textGO.GetComponent<Text>();
    txt.text = label;
    txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    txt.color = Color.black;
    txt.fontSize = 18;
    txt.alignment = TextAnchor.MiddleLeft;

    var textRT = textGO.GetComponent<RectTransform>();
    textRT.sizeDelta = new Vector2(200, 30);
    textRT.anchorMin = new Vector2(0, 1);
    textRT.anchorMax = new Vector2(0, 1);
    textRT.pivot     = new Vector2(0, 1);
    textRT.anchoredPosition = anchoredPos + new Vector2(33, 3); //   
}

// Panel_HubLegend.cs  
public void UpdateLegendFromColors(Color[] colors, string[] labels)
{
    InitLegendPanel();

    //   
    foreach (Transform child in legendPanelParent)
        Destroy(child.gameObject);

    // JSON    
    string jsonPath = GetCurrentChartSpecPath();
    string fieldName = "House Data"; // 

    if (System.IO.File.Exists(jsonPath))
    {
        try
        {
            string json = System.IO.File.ReadAllText(jsonPath);
            var root = JObject.Parse(json);
            var colorFieldToken = root.SelectToken("$.encoding.color.field");
            if (colorFieldToken != null)
            {
                string colorField = colorFieldToken.ToString();
                // datum.  datum['']   
                if (colorField.StartsWith("datum."))
                    colorField = colorField.Substring(6);
                if (colorField.StartsWith("['") && colorField.EndsWith("']"))
                    colorField = colorField.Substring(2, colorField.Length - 4);
                fieldName = colorField;
            }
        }
        catch { }
    }

    AddLegendTitle(fieldName);

    for (int i = 0; i < colors.Length; i++)
    {
        var label = (labels != null && i < labels.Length) ? labels[i] : $"Category {i+1}";
        AddLegendEntry(colors[i], label, new Vector2(20 + i * 180, 0));
    }
}
}
