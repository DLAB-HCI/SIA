using UnityEngine;
using Newtonsoft.Json.Linq;
using IATK;
using System;
using System.Globalization;
using EmbodiedNLI.Visualization; // ChartSpec  

public class AxisField : MonoBehaviour
{
    private ScatterplotVisualisation scatterplotVisualisation;

    void Awake()
    {
        scatterplotVisualisation = GetComponent<ScatterplotVisualisation>();
        if (scatterplotVisualisation == null)
        {
            Debug.LogError("[AxisField] Requires a ScatterplotVisualisation component on the same GameObject.");
        }
    }

    public void Field(ChartSpec spec)
    {
        Debug.Log("[AxisField] SetFields called with ChartSpec object");
        if (scatterplotVisualisation == null)
        {
            Debug.LogError("[AxisField] ScatterplotVisualisation not found.");
            return;
        }
        if (spec == null || spec.Encoding == null)
        {
            Debug.LogWarning("[AxisField] ChartSpec does not contain encoding information.");
            return;
        }

        bool changed = false;

        if (spec.Encoding.X != null && !string.IsNullOrEmpty(spec.Encoding.X.Field))
        {
            UpdateAxis("x", spec.Encoding.X.Field);
            changed = true;
        }

        if (spec.Encoding.Y != null && !string.IsNullOrEmpty(spec.Encoding.Y.Field))
        {
            UpdateAxis("y", spec.Encoding.Y.Field);
            changed = true;
        }

        if (spec.Encoding.Z != null && !string.IsNullOrEmpty(spec.Encoding.Z.Field))
        {
            UpdateAxis("z", spec.Encoding.Z.Field);
            changed = true;
        }

        if (changed)
        {
            scatterplotVisualisation.UpdateVisualisation(AbstractVisualisation.PropertyType.DimensionChange);
            Debug.Log("[AxisField] Applied field changes to visualisation.");
        }
    }

    public void Field(string jsonSpec)
    {
        if (scatterplotVisualisation == null)
        {
            Debug.LogError("[AxisField] ScatterplotVisualisation not found.");
            return;
        }
        if (string.IsNullOrEmpty(jsonSpec))
        {
            Debug.LogWarning("[AxisField] SetFields called with empty JSON spec.");
            return;
        }

        try
        {
            ChartSpec spec = SpecAST.Parse(jsonSpec);
            if (spec != null)
            {
                Field(spec);
                return;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AxisField] Failed to parse JSON as ChartSpec, falling back to legacy mode: {e.Message}");
        }

        ProcessLegacyJsonSpec(jsonSpec);
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
            Debug.LogError($"[AxisField] Invalid JSON: {e.Message}");
            return;
        }

        var encoding = spec["encoding"] as JObject;
        if (encoding == null)
        {
            Debug.LogWarning("[AxisField] No encoding object found in spec.");
            return;
        }

        bool changed = false;
        if (encoding["x"] != null) { UpdateAxisFromJson("x", encoding["x"]); changed = true; }
        if (encoding["y"] != null) { UpdateAxisFromJson("y", encoding["y"]); changed = true; }
        if (encoding["z"] != null) { UpdateAxisFromJson("z", encoding["z"]); changed = true; }

        if (changed)
        {
            scatterplotVisualisation.UpdateVisualisation(AbstractVisualisation.PropertyType.DimensionChange);
            Debug.Log("[AxisField] Applied field changes to visualisation (legacy mode).");
        }
    }

    private void UpdateAxis(string axis, string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName))
        {
            Debug.LogWarning($"[AxisField] Empty field name for {axis} axis.");
            return;
        }

        var vis = scatterplotVisualisation.visualisationReference;
        
        switch (axis.ToLower())
        {
            case "x":
                vis.xDimension.Attribute = fieldName;
                scatterplotVisualisation.UpdateVisualisation(AbstractVisualisation.PropertyType.X);
                break;
            case "y":
                vis.yDimension.Attribute = fieldName;
                scatterplotVisualisation.UpdateVisualisation(AbstractVisualisation.PropertyType.Y);
                break;
            case "z":
                vis.zDimension.Attribute = fieldName;
                scatterplotVisualisation.UpdateVisualisation(AbstractVisualisation.PropertyType.Z);
                break;
            default:
                Debug.LogWarning($"[AxisField] Unknown axis '{axis}'.");
                return;
        }

        Debug.Log($"[AxisField] Updated '{axis}' field name to '{fieldName}'");
    }

    private void UpdateAxisFromJson(string axis, JToken axisToken)
    {
        string fieldName = null;
        
        JToken fieldToken = axisToken["field"];
        if (fieldToken != null)
        {
            if (fieldToken.Type == JTokenType.String)
            {
                fieldName = fieldToken.Value<string>();
            }
            else if (fieldToken.Type == JTokenType.Object)
            {
                JToken nameToken = fieldToken["name"];
                if (nameToken != null && nameToken.Type == JTokenType.String)
                {
                    fieldName = nameToken.Value<string>();
                }
            }
        }

        if (string.IsNullOrEmpty(fieldName))
        {
            Debug.LogWarning($"[AxisField] No valid field name for axis '{axis}'.");
            return;
        }

        UpdateAxis(axis, fieldName);
    }
}
