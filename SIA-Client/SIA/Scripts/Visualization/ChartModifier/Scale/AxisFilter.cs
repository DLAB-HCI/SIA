using UnityEngine;
using Newtonsoft.Json.Linq;
using IATK;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using EmbodiedNLI.Visualization;


public class AxisFilter : MonoBehaviour
{
    private ScatterplotVisualisation scatterplotVisualisation;
    
    [SerializeField] private bool respectScaleDomain = false; //  OFF: transform 
    public void SetRespectScaleDomain(bool on) => respectScaleDomain = on;


    void Awake()
    {
        scatterplotVisualisation = GetComponent<ScatterplotVisualisation>();
        if (scatterplotVisualisation == null)
        {
            Debug.LogError("AxisFilter requires a ScatterplotVisualisation component on the same GameObject.");
        }
    }

    private void ClearFiltersAndScales()
    {
        var vis = scatterplotVisualisation?.visualisationReference;
        if (vis == null) return;

        void Clear(IATK.DimensionFilter d)   //   
        {
            if (d == null) return;
            d.minFilter = 0f; d.maxFilter = 1f;
            d.minScale  = 0f; d.maxScale  = 1f;
        }

        Clear(vis.xDimension);
        Clear(vis.yDimension);
        Clear(vis.zDimension);

        scatterplotVisualisation.UpdateVisualisation(AbstractVisualisation.PropertyType.DimensionFiltering);
        Debug.Log("[AxisFilter] Cleared filters and scales to [0,1] (Option A).");
    }



    public void Filter(ChartSpec spec)
    {
        Debug.Log("[AxisFilter] Filter called with ChartSpec object");
        if (scatterplotVisualisation == null)
        {
            Debug.LogError("[AxisFilter] ScatterplotVisualisation not found.");
            return;
        }

        bool hasTransform = spec?.Transform != null && spec.Transform.Count > 0;

        if (!hasTransform)
        {
            ClearFiltersAndScales();

            if (respectScaleDomain)
            {
                ProcessScaleDomainFilters(spec);
                Debug.Log("[AxisFilter] respectScaleDomain=ON  applied scale.domain after reset.");
            }
            else
            {
                Debug.Log("[AxisFilter] respectScaleDomain=OFF  ignoring scale.domain.");
            }
            return;
        }

        ProcessTransformFilters(spec);
        if (respectScaleDomain)
        {
            Debug.Log("[AxisFilter] respectScaleDomain=ON, but transform present  skipping domain to avoid conflicts (by policy).");
        }
    }

    private void ProcessTransformFilters(ChartSpec spec)
    {
        Dictionary<string, string> fieldToAxisMap = BuildFieldToAxisMap(spec);
        if (fieldToAxisMap.Count == 0)
        {
            Debug.LogWarning("[AxisFilter] Could not determine field-to-axis mapping from encoding.");
            return;
        }

        Dictionary<string, float> axisMinValues = new Dictionary<string, float>();
        Dictionary<string, float> axisMaxValues = new Dictionary<string, float>();

        bool changed = false;
        foreach (var transform in spec.Transform)
        {
            if (string.IsNullOrEmpty(transform.Filter))
                continue;

            string filterExpression = transform.Filter;
            Debug.Log($"[AxisFilter] Processing filter expression: {filterExpression}");

            string[] conditions = filterExpression.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string condition in conditions)
            {
                ProcessFilterCondition(condition.Trim(), fieldToAxisMap, axisMinValues, axisMaxValues);
            }

            changed = true;
        }

        if (changed)
        {
            ApplyAxisFilters(axisMinValues, axisMaxValues);
        }
    }

    private void ProcessScaleDomainFilters(ChartSpec spec)
    {
        if (spec?.Encoding == null) return;

        bool changed = false;

        if (spec.Encoding.X?.Scale?.Domain != null && spec.Encoding.X.Scale.Domain is System.Collections.IList xDomain && xDomain.Count == 2)
        {
            float min = Convert.ToSingle(xDomain[0]);
            float max = Convert.ToSingle(xDomain[1]);
            ApplyAxisFilter("x", min, max);
            changed = true;
            Debug.Log($"[AxisFilter] Applied X scale domain filter: [{min}, {max}]");
        }

        if (spec.Encoding.Y?.Scale?.Domain != null && spec.Encoding.Y.Scale.Domain is System.Collections.IList yDomain && yDomain.Count == 2)
        {
            float min = Convert.ToSingle(yDomain[0]);
            float max = Convert.ToSingle(yDomain[1]);
            ApplyAxisFilter("y", min, max);
            changed = true;
            Debug.Log($"[AxisFilter] Applied Y scale domain filter: [{min}, {max}]");
        }

        if (spec.Encoding.Z?.Scale?.Domain != null && spec.Encoding.Z.Scale.Domain is System.Collections.IList zDomain && zDomain.Count == 2)
        {
            float min = Convert.ToSingle(zDomain[0]);
            float max = Convert.ToSingle(zDomain[1]);
            ApplyAxisFilter("z", min, max);
            changed = true;
            Debug.Log($"[AxisFilter] Applied Z scale domain filter: [{min}, {max}]");
        }

        if (changed)
        {
            scatterplotVisualisation.UpdateVisualisation(AbstractVisualisation.PropertyType.DimensionFiltering);
            Debug.Log("[AxisFilter] Applied scale domain filters to visualisation.");
        }
    }

    public void Filter(string jsonSpec)
    {
        if (scatterplotVisualisation == null)
        {
            Debug.LogError("[AxisFilter] ScatterplotVisualisation not found.");
            return;
        }
        if (string.IsNullOrEmpty(jsonSpec))
        {
            Debug.LogWarning("[AxisFilter] Filter called with empty JSON spec.");
            return;
        }

        try
        {
            ChartSpec spec = SpecAST.Parse(jsonSpec);
            if (spec == null)
            {
                Debug.LogError("[AxisFilter] Failed to parse JSON into ChartSpec.");
                return;
            }

            Filter(spec);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AxisFilter] Error processing JSON spec: {e.Message}");
            ProcessLegacyJsonSpec(jsonSpec);
        }
    }

    public void FilterAxes(string jsonSpec)
    {
        Filter(jsonSpec);
    }

    private Dictionary<string, string> BuildFieldToAxisMap(ChartSpec spec)
    {
        Dictionary<string, string> fieldToAxisMap = new Dictionary<string, string>();

        if (spec.Encoding == null)
            return fieldToAxisMap;

        void AddFieldVariations(string field, string axis)
        {
            if (string.IsNullOrEmpty(field)) return;

            fieldToAxisMap[field] = axis;

            fieldToAxisMap[$"[{field}]"] = axis;
            fieldToAxisMap[$"['{field}']"] = axis;
            fieldToAxisMap[$"[\"{field}\"]"] = axis;

            if (field.Contains("(") || field.Contains(")") || field.Contains(" "))
            {
                string escaped = field.Replace("(", "\\(").Replace(")", "\\)");
                fieldToAxisMap[escaped] = axis;
            }
        }

        if (spec.Encoding.X != null && !string.IsNullOrEmpty(spec.Encoding.X.Field))
        {
            AddFieldVariations(spec.Encoding.X.Field, "x");
        }

        if (spec.Encoding.Y != null && !string.IsNullOrEmpty(spec.Encoding.Y.Field))
        {
            AddFieldVariations(spec.Encoding.Y.Field, "y");
        }

        if (spec.Encoding.Z != null && !string.IsNullOrEmpty(spec.Encoding.Z.Field))
        {
            AddFieldVariations(spec.Encoding.Z.Field, "z");
        }

        Debug.Log($"[AxisFilter] Built field to axis map with {fieldToAxisMap.Count} entries");
        return fieldToAxisMap;
    }

    private void ProcessFilterCondition(
        string condition,
        Dictionary<string, string> fieldToAxisMap,
        Dictionary<string, float> axisMinValues,
        Dictionary<string, float> axisMaxValues)
    {
        var regex = new Regex(@"datum(?:\.|\[\s*['""]?)([^'""\]\s]+(?:\([^)]*\))?[^'""\]\s]*)(?:['""]?\s*\])?\s*([<>=!]=|[<>=])\s*(-?\d+(?:\.\d+)?)");
        var match = regex.Match(condition);

        if (!match.Success)
        {
            Debug.LogWarning($"[AxisFilter] Could not parse filter condition: {condition}");
            return;
        }

        string rawFieldName = match.Groups[1].Value.Trim();
        string op = match.Groups[2].Value;
        float value = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

        string axis = null;
        if (fieldToAxisMap.TryGetValue(rawFieldName, out axis))
        {
        }
        else
        {
            var possibleMatches = fieldToAxisMap.Keys.Where(k =>
                k.Contains(rawFieldName) || rawFieldName.Contains(k.Replace("[", "").Replace("]", "").Replace("'", "").Replace("\"", ""))
            ).ToList();

            if (possibleMatches.Any())
            {
                axis = fieldToAxisMap[possibleMatches.First()];
                Debug.Log($"[AxisFilter] Found approximate field match: '{rawFieldName}' -> '{possibleMatches.First()}' -> axis '{axis}'");
            }
            else
            {
                Debug.LogWarning($"[AxisFilter] Field '{rawFieldName}' not mapped to any axis. Available fields: {string.Join(", ", fieldToAxisMap.Keys)}");
                return;
            }
        }

        switch (op)
        {
            case ">=":
                if (!axisMinValues.ContainsKey(axis) || value > axisMinValues[axis])
                    axisMinValues[axis] = value;
                break;
            case ">":
                if (!axisMinValues.ContainsKey(axis) || value + 0.000001f > axisMinValues[axis])
                    axisMinValues[axis] = value + 0.000001f;
                break;
            case "<=":
                if (!axisMaxValues.ContainsKey(axis) || value < axisMaxValues[axis])
                    axisMaxValues[axis] = value;
                break;
            case "<":
                if (!axisMaxValues.ContainsKey(axis) || value - 0.000001f < axisMaxValues[axis])
                    axisMaxValues[axis] = value - 0.000001f;
                break;
            case "==":
                axisMinValues[axis] = value;
                axisMaxValues[axis] = value;
                break;
            default:
                Debug.LogWarning($"[AxisFilter] Unsupported operator '{op}' in condition: {condition}");
                break;
        }

        Debug.Log($"[AxisFilter] Extracted filter for axis '{axis}': {rawFieldName} {op} {value}");
    }

    private void ApplyAxisFilters(Dictionary<string, float> axisMinValues, Dictionary<string, float> axisMaxValues)
    {
        bool changed = false;
        foreach (var axis in new[] { "x", "y", "z" })
        {
            if (axisMinValues.ContainsKey(axis) || axisMaxValues.ContainsKey(axis))
            {
                ApplyAxisFilter(
                    axis,
                    axisMinValues.ContainsKey(axis) ? axisMinValues[axis] : float.NegativeInfinity,
                    axisMaxValues.ContainsKey(axis) ? axisMaxValues[axis] : float.PositiveInfinity
                );
                changed = true;
            }
        }

        if (changed)
        {
            scatterplotVisualisation.UpdateVisualisation(AbstractVisualisation.PropertyType.DimensionFiltering);
            Debug.Log("[AxisFilter] Applied transform filters to visualisation.");
        }
    }

    private void ApplyAxisFilter(string axis, float min, float max)
    {
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
            default:
                Debug.LogWarning($"[AxisFilter] Unknown axis '{axis}'.");
                return;
        }

        min = float.IsNegativeInfinity(min) ? dataMin : Mathf.Clamp(min, dataMin, dataMax);
        max = float.IsPositiveInfinity(max) ? dataMax : Mathf.Clamp(max, dataMin, dataMax);

        float minNorm = (dataMax - dataMin) > 0 ? (min - dataMin) / (dataMax - dataMin) : 0f;
        float maxNorm = (dataMax - dataMin) > 0 ? (max - dataMin) / (dataMax - dataMin) : 1f;

        switch (axis.ToLower())
        {
            case "x":
                vis.xDimension.minFilter = minNorm;
                vis.xDimension.maxFilter = maxNorm;
                break;
            case "y":
                vis.yDimension.minFilter = minNorm;
                vis.yDimension.maxFilter = maxNorm;
                break;
            case "z":
                vis.zDimension.minFilter = minNorm;
                vis.zDimension.maxFilter = maxNorm;
                break;
        }

        Debug.Log($"[AxisFilter] Applied '{axis}' filter: [{min}, {max}] (normalized: [{minNorm}, {maxNorm}])");
    }

    private void ProcessLegacyJsonSpec(string jsonSpec)
    {
        if (!respectScaleDomain) { 
            Debug.Log("[AxisFilter] respectScaleDomain=OFF  legacy domain parsing skipped."); 
        return; 
        }

        try
        {
            JObject spec = JObject.Parse(jsonSpec);
            var encoding = spec["encoding"] as JObject;
            if (encoding == null)
            {
                Debug.LogWarning("[AxisFilter] No encoding object found in spec.");
                return;
            }

            bool changed = false;

            if (encoding["x"]?["scale"]?["domain"] != null)
            {
                ProcessLegacyAxisFilter("x", encoding["x"]["scale"]["domain"]);
                changed = true;
            }
            if (encoding["y"]?["scale"]?["domain"] != null)
            {
                ProcessLegacyAxisFilter("y", encoding["y"]["scale"]["domain"]);
                changed = true;
            }
            if (encoding["z"]?["scale"]?["domain"] != null)
            {
                ProcessLegacyAxisFilter("z", encoding["z"]["scale"]["domain"]);
                changed = true;
            }

            if (changed)
            {
                scatterplotVisualisation.UpdateVisualisation(AbstractVisualisation.PropertyType.DimensionFiltering);
                Debug.Log("[AxisFilter] Applied legacy JSON filters to visualisation.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[AxisFilter] Error processing legacy JSON spec: {e.Message}");
        }
    }

    private void ProcessLegacyAxisFilter(string axis, JToken domainToken)
    {
        if (domainToken?.Type != JTokenType.Array) return;

        var domainArray = domainToken as JArray;
        if (domainArray.Count != 2) return;

        float min = ParseFloatToken(domainArray[0]);
        float max = ParseFloatToken(domainArray[1]);

        ApplyAxisFilter(axis, min, max);
    }

    private float ParseFloatToken(JToken token)
    {
        if (token == null) return 0f;
        if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
            return token.Value<float>();
        if (token.Type == JTokenType.String)
        {
            if (float.TryParse(token.Value<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                return result;
        }
        Debug.LogWarning($"[AxisFilter] Could not parse float from token '{token}'.");
        return 0f;
    }
}












            
            





            


        












        























            
            





            




        





        
        

        






















            
            





            


        





        



        
                
                
                
                
                
        

        























            
            





            


        





        



        
                
                
                
                
                
        

        























            
            





            




        





        
        

        






















            
            





            


        





        



        
                
                
                
                
                
        

        














