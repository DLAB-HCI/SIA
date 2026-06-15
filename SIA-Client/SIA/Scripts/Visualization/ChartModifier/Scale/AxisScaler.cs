using System.Linq;
using UnityEngine;
using Newtonsoft.Json.Linq;
using IATK;
using System;
using System.Collections.Generic;
using System.Globalization;
using EmbodiedNLI.Visualization; // ChartSpec   

public class AxisScaler : MonoBehaviour
{
    private ScatterplotVisualisation scatterplotVisualisation;

    void Awake()
    {
        scatterplotVisualisation = GetComponent<ScatterplotVisualisation>();
        if (scatterplotVisualisation == null)
        {
            Debug.LogError("AxisScaler requires a ScatterplotVisualisation component on the same GameObject.");
        }
    }

    public void Scale(ChartSpec spec)
    {
        Debug.Log("[AxisScaler] Scale called with ChartSpec object");
        if (scatterplotVisualisation == null)
        {
            Debug.LogError("[AxisScaler] ScatterplotVisualisation not found.");
            return;
        }
        if (spec == null || spec.Encoding == null)
        {
            Debug.LogWarning("[AxisScaler] ChartSpec does not contain encoding information.");
            return;
        }

        bool changed = false;

        if (spec.Encoding.X?.Scale?.Domain != null)
        {
            double xmin, xmax;
            if (TryGetNumericDomain(spec.Encoding.X.Scale.Domain, out xmin, out xmax))
            {
                UpdateAxis("x", xmin, xmax);
                changed = true;
            }
        }

        if (spec.Encoding.Y?.Scale?.Domain != null)
        {
            double ymin, ymax;
            if (TryGetNumericDomain(spec.Encoding.Y.Scale.Domain, out ymin, out ymax))
            {
                UpdateAxis("y", ymin, ymax);
                changed = true;
            }
        }

        if (spec.Encoding.Z?.Scale?.Domain != null)
        {
            double zmin, zmax;
            if (TryGetNumericDomain(spec.Encoding.Z.Scale.Domain, out zmin, out zmax))
            {
                UpdateAxis("z", zmin, zmax);
                changed = true;
            }
        }


        if (changed)
        {
            scatterplotVisualisation.UpdateVisualisation(AbstractVisualisation.PropertyType.Scaling);
            Debug.Log("[AxisScaler] Applied scale changes to visualisation.");
        }
    }

    public void Scale(string jsonSpec)
    {
        if (scatterplotVisualisation == null)
        {
            Debug.LogError("[AxisScaler] ScatterplotVisualisation not found.");
            return;
        }
        if (string.IsNullOrEmpty(jsonSpec))
        {
            Debug.LogWarning("[AxisScaler] Scale called with empty JSON spec.");
            return;
        }

        try
        {
            ChartSpec spec = SpecAST.Parse(jsonSpec);
            if (spec != null)
            {
                Scale(spec);
                return;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AxisScaler] Failed to parse JSON as ChartSpec, falling back to legacy mode: {e.Message}");
        }

        ProcessLegacyJsonSpec(jsonSpec);
    }

    public void ScaleAxes(string jsonSpec)
    {
        Scale(jsonSpec);
    }

    private void UpdateAxis(string axis, double min, double max)
    {
        float minValue = (float)min;
        float maxValue = (float)max;

        var vis = scatterplotVisualisation.visualisationReference;
        float dataMin = 0, dataMax = 1;

        switch (axis.ToLower())
        {
            case "x":
                dataMin = vis.dataSource[vis.xDimension.Attribute].MetaData.minValue;
                dataMax = vis.dataSource[vis.xDimension.Attribute].MetaData.maxValue;
                break;
            case "y":
                dataMin = vis.dataSource[vis.yDimension.Attribute].MetaData.minValue;
                dataMax = vis.dataSource[vis.yDimension.Attribute].MetaData.maxValue;
                break;
            case "z":
                dataMin = vis.dataSource[vis.zDimension.Attribute].MetaData.minValue;
                dataMax = vis.dataSource[vis.zDimension.Attribute].MetaData.maxValue;
                break;
        }

        Debug.Log($"[AxisScaler] {axis} - : [{dataMin}, {dataMax}], : [{minValue}, {maxValue}]");

        if (minValue > maxValue)
        {
            float temp = minValue;
            minValue = maxValue;
            maxValue = temp;
            Debug.LogWarning($"[AxisScaler] {axis}  min > max  .");
        }

        if (Mathf.Approximately(dataMin, dataMax))
        {
            Debug.LogWarning($"[AxisScaler] {axis}    0 (min=max={dataMin}).   .");
            switch (axis.ToLower())
            {
                case "x": vis.xDimension.minScale = 0f; vis.xDimension.maxScale = 1f; break;
                case "y": vis.yDimension.minScale = 0f; vis.yDimension.maxScale = 1f; break;
                case "z": vis.zDimension.minScale = 0f; vis.zDimension.maxScale = 1f; break;
            }
            return;
        }

        float rangeBuffer = (dataMax - dataMin) * 0.01f; // 1%  
        float bufferedDataMin = dataMin - rangeBuffer;
        float bufferedDataMax = dataMax + rangeBuffer;

        minValue = Mathf.Clamp(minValue, bufferedDataMin, bufferedDataMax);
        maxValue = Mathf.Clamp(maxValue, bufferedDataMin, bufferedDataMax);

        float minNorm = Mathf.InverseLerp(bufferedDataMin, bufferedDataMax, minValue);
        float maxNorm = Mathf.InverseLerp(bufferedDataMin, bufferedDataMax, maxValue);

        if (float.IsNaN(minNorm) || float.IsInfinity(minNorm)) minNorm = 0f;
        if (float.IsNaN(maxNorm) || float.IsInfinity(maxNorm)) maxNorm = 1f;

        if (minNorm > maxNorm)
        {
            float temp = minNorm;
            minNorm = maxNorm;
            maxNorm = temp;
        }

        switch (axis.ToLower())
        {
            case "x":
                vis.xDimension.minScale = minNorm;
                vis.xDimension.maxScale = maxNorm;
                break;
            case "y":
                vis.yDimension.minScale = minNorm;
                vis.yDimension.maxScale = maxNorm;
                break;
            case "z":
                vis.zDimension.minScale = minNorm;
                vis.zDimension.maxScale = maxNorm;
                break;
        }

        Debug.Log($"[AxisScaler] {axis}   : [{minValue}, {maxValue}]  : [{minNorm}, {maxNorm}]");
    }

    private void ProcessLegacyJsonSpec(string jsonSpec)
    {
        JObject spec;
        try
        {
            spec = JObject.Parse(jsonSpec);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AxisScaler] Invalid JSON: {e.Message}");
            return;
        }

        var encoding = spec["encoding"] as JObject;
        if (encoding == null)
        {
            Debug.LogWarning("[AxisScaler] No encoding object found in spec.");
            return;
        }

        bool changed = false;
        if (encoding["x"] != null) { UpdateAxis("x", encoding["x"]); changed = true; }
        if (encoding["y"] != null) { UpdateAxis("y", encoding["y"]); changed = true; }
        if (encoding["z"] != null) { UpdateAxis("z", encoding["z"]); changed = true; }

        if (changed)
        {
            scatterplotVisualisation.UpdateVisualisation(AbstractVisualisation.PropertyType.Scaling);
            Debug.Log("[AxisScaler] Applied scale changes to visualisation (legacy mode).");
        }
    }

    private void UpdateAxis(string axis, JToken axisToken)
    {
        JToken domainToken = axisToken["domain"];
        if (domainToken == null && axisToken["scale"] != null)
            domainToken = axisToken["scale"]["domain"];

        if (domainToken == null || domainToken.Type != JTokenType.Array)
        {
            Debug.LogWarning($"[AxisScaler] No valid domain for axis '{axis}'.");
            return;
        }

        var domainArray = domainToken as JArray;
        if (domainArray.Count != 2)
        {
            Debug.LogWarning($"[AxisScaler] Domain for axis '{axis}' must have exactly two elements.");
            return;
        }
        float min = ParseFloatToken(domainArray[0]);
        float max = ParseFloatToken(domainArray[1]);

        UpdateAxis(axis, min, max);
    }

    private float ParseFloatToken(JToken token)
    {
        if (token == null) return 0f;
        if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
            return token.Value<float>();
        if (token.Type == JTokenType.String)
        {
            float result;
            if (float.TryParse(token.Value<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                return result;
        }
        Debug.LogWarning($"[AxisScaler] Could not parse float from token '{token}'.");
        return 0f;
    }
    
        
    private static bool TryGetNumericDomain(object domainObj, out double min, out double max)
    {
        min = 0; max = 0;
        if (domainObj == null) return false;

        if (domainObj is JArray jarr)
        {
            if (jarr.Count >= 2 && TryToDouble(jarr[0], out min) && TryToDouble(jarr[1], out max))
                return true;
            return false;
        }

        if (domainObj is JToken tok)
        {
            var arr = tok as JArray;
            if (arr != null && arr.Count >= 2 && TryToDouble(arr[0], out min) && TryToDouble(arr[1], out max))
                return true;
            return false;
        }

        if (domainObj is IEnumerable<object> objEnum)
        {
            var list = objEnum.ToList();
            if (list.Count >= 2 && TryToDouble(list[0], out min) && TryToDouble(list[1], out max))
                return true;
            return false;
        }

        return false;
    }

    private static bool TryToDouble(object v, out double d)
    {
        d = 0;
        if (v == null) return false;

        if (v is JValue jv)           // JValue   
            return TryToDouble(jv.Value, out d);

        if (v is double dd) { d = dd; return true; }
        if (v is float ff)  { d = ff; return true; }
        if (v is int ii)    { d = ii; return true; }
        if (v is long ll)   { d = ll; return true; }

        if (v is string s)
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d);

        return false;
    }
}
