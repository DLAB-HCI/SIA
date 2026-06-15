using System;
using System.Collections.Generic; 
using UnityEngine;
using EmbodiedNLI.Visualization;
using Newtonsoft.Json.Linq;
using System.IO;
using UnityEngine.InputSystem;
using IATK;

public class Inspector : MonoBehaviour
{
    private const string SpecCompilerRelativePath = "SIA/Scripts/Visualization/SpecCompiler";

    public AxisFilter axisFilter;
    public AxisScaler axisScaler;
    public ColorShifter colorShifter;
    public BinShifter binShifter;
    public AxisField axisField;

    private string previousChartSpecJson = null;

    private static string GetSpecCompilerDirectory()
    {
        return Path.Combine(Application.dataPath, SpecCompilerRelativePath);
    }

    private static string GetSpecCompilerFilePath(string fileName)
    {
        return Path.Combine(GetSpecCompilerDirectory(), fileName);
    }

    private static string GetSpecCompilerAssetPath(string fileName)
    {
        return $"Assets/{SpecCompilerRelativePath}/{fileName}";
    }

    void Start()
    {
        axisFilter?.SetRespectScaleDomain(false);

        if (axisFilter == null)
        {
            axisFilter = GetComponent<AxisFilter>();
            if (axisFilter == null)
            {
                axisFilter = FindObjectOfType<AxisFilter>();
                if (axisFilter != null)
                {
                    Debug.Log("AxisFilter component auto-wired.");
                }
                else
                {
                    Debug.LogWarning("AxisFilter not found in the scene. Please assign it manually.");
                }
            }
        }

        if (axisScaler == null)
        {
            axisScaler = GetComponent<AxisScaler>();
            if (axisScaler == null)
            {
                axisScaler = FindObjectOfType<AxisScaler>();
                if (axisScaler != null)
                {
                    Debug.Log("AxisScaler component auto-wired.");
                }
                else
                {
                    Debug.LogWarning("AxisScaler not found in the scene. Please assign it manually.");
                }
            }
        }

        if (colorShifter == null)
        {
            colorShifter = GetComponent<ColorShifter>();
            if (colorShifter == null)
            {
                colorShifter = FindObjectOfType<ColorShifter>();
                if (colorShifter != null)
                {
                    Debug.Log("ColorShifter component auto-wired.");
                }
            }
        }
        if (binShifter == null)
        {
            binShifter = GetComponent<BinShifter>();
            if (binShifter == null)
            {
                binShifter = FindObjectOfType<BinShifter>();
                if (binShifter != null)
                {
                    Debug.Log("BinShifter component auto-wired.");
                }
                else
                {
                    GameObject obj = GameObject.Find("ScatterplotVis");
                    if (obj == null)
                    {
                        obj = new GameObject("ScatterplotVis");
                        Debug.Log("Created new GameObject for BinShifter");
                    }

                    binShifter = obj.AddComponent<BinShifter>();
                    Debug.Log("Added BinShifter component to " + obj.name);
                }
            }
        }
    }

    public void ASTparsor(string jsonString)
    {
        Debug.Log("[Inspector] ====== RAW JSON STRING START ======");
        Debug.Log(jsonString);
        Debug.Log("[Inspector] ====== RAW JSON STRING END ======");

        Debug.Log($"[Inspector] ASTparsor called. Input preview: {jsonString.Substring(0, Math.Min(200, jsonString.Length))}");

        string chartSpecJson = ExtractChartSpecJsonRobust(jsonString);

    try
    {
        string savePathCurrent = GetSpecCompilerFilePath("chartSpec_current.json");
        string savePathPrev = GetSpecCompilerFilePath("chartSpec_previous.json");

        if (!string.IsNullOrEmpty(previousChartSpecJson))
        {
            File.WriteAllText(savePathPrev, previousChartSpecJson);
            Debug.Log($"[Inspector] Previous chartSpec JSON saved to: {GetSpecCompilerAssetPath("chartSpec_previous.json")}");
        }

        File.WriteAllText(savePathCurrent, chartSpecJson);
        Debug.Log($"[Inspector] Current chartSpec JSON saved to: {GetSpecCompilerAssetPath("chartSpec_current.json")}");

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string taggedFileName = $"chartSpec_{timestamp}.json";
        string savePathTagged = GetSpecCompilerFilePath(taggedFileName);
        File.WriteAllText(savePathTagged, chartSpecJson);
        Debug.Log($"[Inspector] chartSpec JSON saved with timestamp: {GetSpecCompilerAssetPath(taggedFileName)}");

        previousChartSpecJson = chartSpecJson;
    }
    catch (Exception e)
    {
        Debug.LogError($"[Inspector] Failed to save chartSpec JSON: {e.Message}");
    }
        try
        {
            var token = JToken.Parse(chartSpecJson);
            if (token.Type == JTokenType.Array)
            {
                var arr = (JArray)token;
                if (arr.Count > 1)
                {
                    chartSpecJson = arr[1].ToString();
                    Debug.Log("[Inspector] 'chart_spec' was an array; using the second element.");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Inspector] Skipped array handling for chartSpecJson: {e.Message}");
        }


        Debug.Log($"[Inspector] Final chartSpecJson preview: {chartSpecJson.Substring(0, Math.Min(200, chartSpecJson.Length))}");


        try
        {
            string savePathCurrent = GetSpecCompilerFilePath("chartSpec_current.json");
            string savePathPrev = GetSpecCompilerFilePath("chartSpec_previous.json");

            if (!string.IsNullOrEmpty(previousChartSpecJson))
            {
                File.WriteAllText(savePathPrev, previousChartSpecJson);
                Debug.Log($"[Inspector] Previous chartSpec JSON saved to: {GetSpecCompilerAssetPath("chartSpec_previous.json")}");
            }

            File.WriteAllText(savePathCurrent, chartSpecJson);
            Debug.Log($"[Inspector] Current chartSpec JSON saved to: {GetSpecCompilerAssetPath("chartSpec_current.json")}");

            previousChartSpecJson = chartSpecJson;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Inspector] Failed to save chartSpec JSON: {e.Message}");
        }


        ChartSpec spec = SpecAST.Parse(chartSpecJson);
        if (spec == null)
        {
            Debug.LogError("[Inspector] SpecAST.Parse returned null. Check JSON format.");
        }
        else
        {
            Debug.Log("[Inspector] SpecAST.Parse succeeded. Dispatching to components.");

        }

        string taskType = ExtractTaskFromJson(jsonString);
        taskType = NormalizeTask(taskType);
        Debug.Log($"[Inspector] Task: {taskType}");


        switch (taskType)
        {
            case "Binning":
            case "Filter":
                ApplyAxisField(spec);
                ApplyAxisFilter(spec);
                ApplyColorShifter(spec);
                ApplyDotColliders();
                break;

            case "Navigate":
                ApplyAxisField(spec);
                ApplyAxisFilter(spec);
                ApplyColorShifter(spec);
                ApplyDotColliders();
                break;

            case "Bin":
                ApplyAxisField(spec);
                ApplyAxisFilter(spec);
                ApplyColorShifter(spec);
                ApplyDotColliders();
                break;

            default:
                Debug.LogWarning($"[Inspector] Unknown task '{taskType}'. Applying all components.");
                ApplyAxisField(spec);
                ApplyAxisFilter(spec);
                ApplyColorShifter(spec);
                ApplyDotColliders();
                break;
        }
    }

    private void ApplyDotColliders()
    {
        var dotAssigner = GetComponent<ScatterDotColliderAssigner>() ?? FindObjectOfType<ScatterDotColliderAssigner>();
        if (dotAssigner == null)
        {
            Debug.LogWarning("[Inspector] ScatterDotColliderAssigner was not found, so colliders could not be applied.");
            return;
        }

        var viz = dotAssigner.GetComponent<AbstractVisualisation>();
        if (viz == null)
        {
            Debug.LogWarning("[Inspector] AbstractVisualisation was not found.");
            return;
        }

        StartCoroutine(UpdateCollidersAfterVisualUpdate(dotAssigner, viz));
    }

    private System.Collections.IEnumerator UpdateCollidersAfterVisualUpdate(ScatterDotColliderAssigner dotAssigner, AbstractVisualisation viz)
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        dotAssigner.RefreshFromVisualisation();

        var alive = BuildAliveMaskFromFiltersAndScale(viz);
        if (alive != null)
            dotAssigner.UpdateActiveByMask(alive, hideInactive: true);
    }

    private HashSet<int> BuildAliveMaskFromFiltersAndScale(AbstractVisualisation visComp)
    {
        if (visComp == null || visComp.visualisationReference == null) return null;

        var vis = visComp.visualisationReference;
        var ds = vis.dataSource;
        if (ds == null) return null;

        string xA = vis.xDimension != null ? vis.xDimension.Attribute : null;
        string yA = vis.yDimension != null ? vis.yDimension.Attribute : null;
        string zA = vis.zDimension != null ? vis.zDimension.Attribute : null;

        float fXMin = (vis.xDimension != null) ? vis.xDimension.minFilter : 0f;
        float fXMax = (vis.xDimension != null) ? vis.xDimension.maxFilter : 1f;
        float fYMin = (vis.yDimension != null) ? vis.yDimension.minFilter : 0f;
        float fYMax = (vis.yDimension != null) ? vis.yDimension.maxFilter : 1f;
        float fZMin = (vis.zDimension != null) ? vis.zDimension.minFilter : 0f;
        float fZMax = (vis.zDimension != null) ? vis.zDimension.maxFilter : 1f;

        float sXMin = (vis.xDimension != null) ? vis.xDimension.minScale : 0f;
        float sXMax = (vis.xDimension != null) ? vis.xDimension.maxScale : 1f;
        float sYMin = (vis.yDimension != null) ? vis.yDimension.minScale : 0f;
        float sYMax = (vis.yDimension != null) ? vis.yDimension.maxScale : 1f;
        float sZMin = (vis.zDimension != null) ? vis.zDimension.minScale : 0f;
        float sZMax = (vis.zDimension != null) ? vis.zDimension.maxScale : 1f;

        var alive = new HashSet<int>();
        int n = ds.DataCount;

        for (int i = 0; i < n; i++)
        {
            bool ok = true;

            if (!string.IsNullOrEmpty(xA))
            {
                float x = ds[xA].Data[i];
                ok &= (x >= fXMin && x <= fXMax) && (x >= sXMin && x <= sXMax);
            }
            if (!string.IsNullOrEmpty(yA))
            {
                float y = ds[yA].Data[i];
                ok &= (y >= fYMin && y <= fYMax) && (y >= sYMin && y <= sYMax);
            }
            if (!string.IsNullOrEmpty(zA))
            {
                float z = ds[zA].Data[i];
                ok &= (z >= fZMin && z <= fZMax) && (z >= sZMin && z <= sZMax);
            }

            if (ok) alive.Add(i);
        }

        Debug.Log($"[Inspector] alive (filters+scale): {alive.Count}/{n}");
        return alive;
    }

    private string ExtractTaskFromJson(string jsonString)
    {
        try
        {
            var root = JObject.Parse(jsonString);
            string task =
                (string?)root.SelectToken("$.module_spec.speechPatternAnalyzer.uncertainty.calibrated_task")
                ?? (string?)root.SelectToken("$.module_spec.speechPatternAnalyzer.uncertainty.task_LLM_original")
                ?? "unknown";

            switch (task?.Trim().ToLowerInvariant())
            {
                case "filter":
                    task = "Filter";
                    break;
                case "navigate":
                    task = "Navigate";
                    break;
                case "bin":
                case "binning":
                case "binnning":
                    task = "Bin";
                    break;
                default:
                    if (!string.IsNullOrEmpty(task) && task != "unknown")
                        task = char.ToUpperInvariant(task[0]) + task.Substring(1);
                    break;
            }

            double? conf =
                (double?)root.SelectToken("$.module_spec.speechPatternAnalyzer.uncertainty.calibrated_confidence");
            double? ent =
                (double?)root.SelectToken("$.module_spec.speechPatternAnalyzer.uncertainty.calibrated_entropy");

            string label; double value;
            if (conf.HasValue) { label = "Confidence"; value = conf.Value; }
            else if (ent.HasValue) { label = "Entropy"; value = ent.Value; }
            else { label = "Entropy"; value = 0; }

            Debug.Log($"[Inspector] task: {task}");
            return task;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Inspector] task parse error: {e.Message}");
            return "Error";
        }
    }

    private string NormalizeTask(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "Unknown";
        var k = s.Trim().ToLowerInvariant();
        switch (k)
        {
            case "filter": return "Filter";
            case "navigate": return "Navigate";
            case "bin":
            case "binning":
            case "binnning":
                return "Bin";
            default:
                return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }


    public void ApplyColorShifter(ChartSpec spec)
    {
        if (colorShifter == null)
        {
            Debug.LogError("Inspector: ColorShifter is not assigned.");
            return;
        }
        Debug.Log("[Inspector] Passing spec to ColorShifter: " + spec);
        colorShifter.ColorShift(spec);
    }

    public void ApplyAxisFilter(ChartSpec spec)
    {
        if (axisFilter == null)
        {
            Debug.LogError("Inspector: AxisFilter is not assigned.");
            return;
        }
        Debug.Log("[Inspector] Passing spec to AxisFilter: " + spec);
        axisFilter.Filter(spec);
    }

    public void ApplyAxisScaler(ChartSpec spec)
    {
        if (axisScaler == null)
        {
            Debug.LogError("Inspector: AxisScaler is not assigned.");
            return;
        }
        Debug.Log("[Inspector] Passing spec to AxisScaler: " + spec);
        axisScaler.Scale(spec);
    }

    public void ApplyAxisField(ChartSpec spec)
    {
        if (axisField == null)
        {
            Debug.LogError("Inspector: AxisField is not assigned.");
            return;
        }
        Debug.Log("[Inspector] Passing spec to AxisField: " + spec);
        axisField.Field(spec);
    }

    public void ApplyBin(ChartSpec spec)
    {
        if (binShifter == null)
        {
            Debug.LogError("Inspector: BinShifter is not assigned.");
            return;
        }
        Debug.Log("[Inspector] Bin method invoked: " + spec);
        binShifter.Bin(spec);
    }

private string ExtractChartSpecJsonRobust(string jsonString)
{
    JToken root;
    try { root = JToken.Parse(jsonString); }
    catch { return jsonString; }

    var candidates = new[]
    {
        "$.chart_spec",
        "$.gpt_response_json.chart_spec",
        "$.module_spec.chart_spec",
        "$.module_spec.gpt_response_json.chart_spec",
        "$.updated_module_spec.chart_spec",
        "$.module_spec.updated_module_spec.chart_spec"
    };

    foreach (var path in candidates)
    {
        var tok = root.SelectToken(path);
        var picked = PickChartSpecToken(tok);
        if (picked != null) return picked.ToString();
    }

    var found = FindFirstChartLikeObject(root);
    if (found != null) return found.ToString();

    return jsonString;
}

private JToken PickChartSpecToken(JToken token)
{
    if (token == null || token.Type == JTokenType.Null) return null;

    if (token.Type == JTokenType.Array)
    {
        var arr = (JArray)token;
        if (arr.Count >= 2 && arr[1].Type == JTokenType.Object) return arr[1];
        foreach (var e in arr)
            if (e.Type == JTokenType.Object && e["encoding"] != null)
                return e;
        return null;
    }

    if (token.Type == JTokenType.String)
    {
        var s = token.Value<string>();
        try
        {
            var parsed = JToken.Parse(s);
            return PickChartSpecToken(parsed) ?? parsed;
        }
        catch { return token; }
    }

    if (token.Type == JTokenType.Object) return token;

    return null;
}

private JObject FindFirstChartLikeObject(JToken root)
{
    if (root == null) return null;

    if (root.Type == JTokenType.Object)
    {
        var obj = (JObject)root;
        if (obj["encoding"] != null) return obj;
        foreach (var prop in obj.Properties())
        {
            var child = FindFirstChartLikeObject(prop.Value);
            if (child != null) return child;
        }
    }
    else if (root.Type == JTokenType.Array)
    {
        foreach (var e in (JArray)root)
        {
            var child = FindFirstChartLikeObject(e);
            if (child != null) return child;
        }
    }
    return null;
}


    private string NormalizeSpecJson(string chartSpecJson)
    {
        if (string.IsNullOrWhiteSpace(chartSpecJson)) return chartSpecJson;

        JToken root;
        try { root = JToken.Parse(chartSpecJson); }
        catch { return chartSpecJson; }

        if (root.Type != JTokenType.Object) return chartSpecJson;
        var spec = (JObject)root;

        var enc = spec["encoding"] as JObject;
        if (enc != null)
        {
            void FixAxisDomain(string axisKey)
            {
                var axis = enc[axisKey] as JObject;
                if (axis == null) return;

                JArray GetDomainArray()
                {
                    if (axis["domain"] is JArray d1) return d1;
                    if (axis["scale"]?["domain"] is JArray d2) return d2;
                    return null;
                }

                var domain = GetDomainArray();
                if (domain == null) return;

                if (domain.Count != 2)
                {
                    if (axis["scale"]?["domain"] != null) axis["scale"]["domain"] = null;
                    if (axis["domain"] != null) axis["domain"] = null;
                    return;
                }

                double ToDouble(JToken t, out bool ok)
                {
                    ok = false;
                    if (t == null) return 0;
                    if (t.Type == JTokenType.Float || t.Type == JTokenType.Integer)
                    {
                        ok = true; return t.Value<double>();
                    }
                    if (t.Type == JTokenType.String)
                    {
                        if (double.TryParse(t.Value<string>(), System.Globalization.NumberStyles.Float,
                                            System.Globalization.CultureInfo.InvariantCulture, out var v))
                        { ok = true; return v; }
                    }
                    return 0;
                }

                var a = ToDouble(domain[0], out var ok1);
                var b = ToDouble(domain[1], out var ok2);

                if (ok1 && ok2)
                {
                    if (axis["scale"] == null) axis["scale"] = new JObject();
                    axis["scale"]["domain"] = new JArray(a, b);
                }
                else
                {
                    if (axis["scale"]?["domain"] != null) axis["scale"]["domain"] = null;
                    if (axis["domain"] != null) axis["domain"] = null;
                }

                if (axis["scale"]?["range"] == null)
                    axis["scale"]["range"] = new JArray(0, 1);

                if (axis["scale"]?["type"] == null)
                    axis["scale"]["type"] = "linear";
            }

            FixAxisDomain("x");
            FixAxisDomain("y");
            FixAxisDomain("z");

            void FixBin(string axisKey)
            {
                var axis = enc[axisKey] as JObject;
                if (axis == null) return;

                if (axis["bin"] == null) axis["bin"] = new JObject();
                var bin = (JObject)axis["bin"];
                if (bin["enable"] == null) bin["enable"] = false;
                if (bin["step"] == null || bin["step"].Type == JTokenType.Null) bin["step"] = null;
                if (bin["edges"] == null || bin["edges"].Type == JTokenType.Null) bin["edges"] = null;
            }

            FixBin("x");
            FixBin("y");
            FixBin("z");

            var color = enc["color"] as JObject;
            if (color != null)
            {
                if (color["scale"] == null) color["scale"] = new JObject();

                var cdom = color["scale"]["domain"];
                if (cdom is JArray cdomArr && cdomArr.Count > 0 && cdomArr[0].Type == JTokenType.String)
                {
                    color["scale"]["domain"] = null;
                }

                if (color["scale"]?["type"] == null)
                    color["scale"]["type"] = "ordinal";

                if (color["scale"]?["range"] == null)
                {
                    color["scale"]["range"] = new JArray(
                        "#4E79A7", "#F28E2B", "#E15759", "#76B7B2", "#59A14F",
                        "#EDC949", "#AF7AA1", "#FF9DA7", "#9C755F", "#BAB0AC"
                    );
                }

                if (color["legend"] == null) color["legend"] = new JObject();
                if (color["legend"]?["title"] == null)
                    color["legend"]["title"] = color["field"] != null ? color["field"].ToString() : "category";

                if (color["value"] == null) color["value"] = "#BAB0AC";

                if (color["condition"] == null) color["condition"] = new JArray();

                if (color["condition"].Type == JTokenType.Object)
                {
                    color["condition"] = new JArray(color["condition"]);
                }

                if (color["condition"] is JArray condArr)
                {
                    var cleaned = new JArray();
                    foreach (var c in condArr)
                    {
                        if (c is JObject cj && cj["test"] != null && cj["value"] != null)
                            cleaned.Add(cj);
                    }
                    color["condition"] = cleaned;
                }
            }

            if (enc["tooltip"] != null && enc["tooltip"].Type != JTokenType.Array)
            {
                enc["tooltip"] = new JArray(enc["tooltip"]);
            }
        }

        if (spec["mark"] == null) spec["mark"] = "scatter";
        if (spec["data"] == null) spec["data"] = new JObject();
        if (spec["data"]?["url"] == null) spec["data"]["url"] = "data.csv";

        if (spec["width"] == null) spec["width"] = 1.0;
        if (spec["height"] == null) spec["height"] = 1.0;
        if (spec["depth"] == null) spec["depth"] = 1.0;

        return spec.ToString(Newtonsoft.Json.Formatting.None);
    }
}










                    







        
        
        
        
                




















    




