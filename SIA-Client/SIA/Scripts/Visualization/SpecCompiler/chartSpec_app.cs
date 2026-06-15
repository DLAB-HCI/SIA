using UnityEngine;
using IATK;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class chartSpec_app : MonoBehaviour
{
    private const string SpecCompilerRelativePath = "SIA/Scripts/Visualization/SpecCompiler";

    private ScatterplotVisualisation scatterplot;

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

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    private void RefreshScatterplot()
    {
        scatterplot = FindObjectOfType<ScatterplotVisualisation>();
    }

    public string GetScatterplotSpec()
    {
        RefreshScatterplot();
        if (scatterplot == null || scatterplot.visualisationReference == null)
        {
            Debug.LogWarning("[chartSpec_app] Scatterplot not found.");
            return "{}";
        }

        var vis = scatterplot.visualisationReference;
        var dataSource = vis.dataSource;

        string templatePath = GetSpecCompilerFilePath("chartSpec_temp.json");
        JObject spec = JObject.Parse(File.ReadAllText(templatePath));

        ApplyAxisSpec(spec, "x", vis.xDimension, dataSource);
        ApplyAxisSpec(spec, "y", vis.yDimension, dataSource);
        ApplyAxisSpec(spec, "z", vis.zDimension, dataSource);

        DimensionFilter colorDim = vis.colourDimension;
        if (colorDim != null && !string.IsNullOrEmpty(colorDim.Attribute) &&
            dataSource[colorDim.Attribute] != null)
        {
            string colorField = colorDim.Attribute;
            float[] colorData = dataSource[colorField].Data;
            JObject colorSpec = (JObject)spec["encoding"]["color"];
            BuildColorSpec(colorSpec, colorField, colorData);
        }
        else
        {
            Debug.LogWarning("[chartSpec_app] Color dimension missing, skipping.");
        }

        spec["transform"] = BuildTransformSpec(scatterplot);

        spec["width"] = vis.width;
        spec["height"] = vis.height;
        spec["depth"] = vis.depth;

        spec["description"] = "Exported from IATK scatterplot";

        return spec.ToString();
    }

    private void ApplyAxisSpec(JObject spec, string axisKey, DimensionFilter dim, DataSource dataSource)
    {
        if (dim == null || string.IsNullOrEmpty(dim.Attribute) || dataSource[dim.Attribute] == null)
        {
            Debug.LogWarning($"[chartSpec_app] {axisKey.ToUpper()} axis missing, skipping.");
            return;
        }

        string field = dim.Attribute;
        float[] data = dataSource[field].Data;
        if (data == null || data.Length == 0)
        {
            Debug.LogWarning($"[chartSpec_app] {axisKey.ToUpper()} data empty, skipping.");
            return;
        }

        JObject axisSpec = (JObject)spec["encoding"][axisKey];
        axisSpec["field"] = field;
        axisSpec["axis"]["title"] = field;
        axisSpec["scale"]["domain"] = new JArray(data.Min(), data.Max());
        axisSpec["bin"] = BuildBinSpec(field, data);
    }

    public void SaveScatterplotSpec(string fileName = "chartSpec_app.json")
    {
        string savePath = GetSpecCompilerFilePath(fileName);

        string json = GetScatterplotSpec();
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[chartSpec_app] Spec is empty, not saving.");
            return;
        }

        File.WriteAllText(savePath, json);
        Debug.Log("[chartSpec_app] Saved chart spec to: " + GetSpecCompilerAssetPath(fileName));
    }

    private JObject BuildBinSpec(string field, float[] values, int binCount = 10)
    {
        JObject binSpec = new JObject
        {
            ["enable"] = true,
            ["step"] = null,
            ["edges"] = null
        };

        if (values == null || values.Length == 0)
            return binSpec;

        var distinct = values.Distinct().OrderBy(v => v).ToArray();

        if (distinct.Length <= binCount)
        {
            binSpec["edges"] = new JArray(distinct);
            binSpec["step"] = null;
        }
        else
        {
            float min = values.Min();
            float max = values.Max();
            float step = (max - min) / binCount;

            JArray edges = new JArray();
            for (int i = 0; i <= binCount; i++)
                edges.Add(min + i * step);

            binSpec["step"] = step;
            binSpec["edges"] = edges;
        }

        return binSpec;
    }

    private void BuildColorSpec(JObject colorSpec, string field, float[] values)
    {
        colorSpec["field"] = field;
        colorSpec["legend"]["title"] = field;

        var distinctValues = values.Distinct().OrderBy(v => v).ToArray();
        colorSpec["scale"]["domain"] = new JArray(distinctValues);

        JArray conditions = new JArray();
        if (distinctValues.Length > 10)
        {
            int binCount = 3;
            float min = values.Min();
            float max = values.Max();
            float step = (max - min) / binCount;

            string[] palette = { "#4E79A7", "#F28E2B", "#E15759" };

            for (int i = 0; i < binCount; i++)
            {
                float a = min + i * step;
                float b = (i == binCount - 1) ? max : (a + step);
                string op = (i == binCount - 1) ? "<=" : "<";
                conditions.Add(new JObject
                {
                    ["test"] = $"datum.{field} >= {a} && datum.{field} {op} {b}",
                    ["value"] = palette[i % palette.Length]
                });
            }
        }
        else
        {
            string[] palette = { "#4E79A7", "#F28E2B", "#E15759", "#76B7B2", "#59A14F" };
            for (int i = 0; i < distinctValues.Length; i++)
            {
                conditions.Add(new JObject
                {
                    ["test"] = $"datum.{field} == '{distinctValues[i]}'",
                    ["value"] = palette[i % palette.Length]
                });
            }
        }

        colorSpec["condition"] = conditions;
    }

    private JArray BuildTransformSpec(ScatterplotVisualisation scatterplot)
    {
        var vis = scatterplot.visualisationReference;
        JArray transformArr = new JArray();
        List<string> filters = new List<string>();

        void AddFilter(DimensionFilter d)
        {
            if (d == null || string.IsNullOrEmpty(d.Attribute) || vis.dataSource[d.Attribute] == null) return;

            float dataMin = vis.dataSource[d.Attribute].MetaData.minValue;
            float dataMax = vis.dataSource[d.Attribute].MetaData.maxValue;
            float minVal = dataMin + d.minFilter * (dataMax - dataMin);
            float maxVal = dataMin + d.maxFilter * (dataMax - dataMin);

            if (minVal > dataMin && maxVal < dataMax)
                filters.Add($"datum.{d.Attribute} >= {minVal} && datum.{d.Attribute} <= {maxVal}");
            else if (minVal > dataMin)
                filters.Add($"datum.{d.Attribute} >= {minVal}");
            else if (maxVal < dataMax)
                filters.Add($"datum.{d.Attribute} <= {maxVal}");
        }

        AddFilter(vis.xDimension);
        AddFilter(vis.yDimension);
        AddFilter(vis.zDimension);

        if (filters.Count > 0)
            transformArr.Add(new JObject { ["filter"] = string.Join(" && ", filters) });

        return transformArr;
    }

    public void ApplyChartSpec(JObject newSpec)
    {
        if (newSpec == null)
        {
            Debug.LogError("[chartSpec_app] newSpec is null");
            return;
        }

        try
        {
            string specJson = newSpec.ToString();
            string savePath = GetSpecCompilerFilePath("chartSpec_app.json");
            File.WriteAllText(savePath, specJson);
            Debug.Log("[chartSpec_app] Applied and saved new chart spec from server.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[chartSpec_app] Error applying chart spec: " + e.Message);
        }
    }
}
