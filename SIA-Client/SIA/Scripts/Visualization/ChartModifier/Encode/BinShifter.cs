using UnityEngine;
using Newtonsoft.Json.Linq;
using IATK;
using System;
using System.Linq;
using System.Collections.Generic;
using EmbodiedNLI.Visualization;

public class BinShifter : MonoBehaviour
{
    private ScatterplotVisualisation scatterplotVisualisation;
    private Dictionary<string, float[]> originalDataCache = new Dictionary<string, float[]>();
    private Dictionary<float, string> binLabels = new Dictionary<float, string>();

    void Start()
    {
        scatterplotVisualisation = GetComponent<ScatterplotVisualisation>();
        if (scatterplotVisualisation == null)
        {
            Debug.LogError("Failed to get ScatterplotVisualisation component");
        }
    }

    public void Bin(ChartSpec spec)
    {
        if (spec == null || spec.Encoding == null)
        {
            Debug.LogError("[BinShifter] Spec or encoding is null.");
            return;
        }

        if (scatterplotVisualisation == null)
        {
            Debug.LogError("[BinShifter] scatterplotVisualisation is null.");
            return;
        }

        if (scatterplotVisualisation.visualisationReference == null)
        {
            Debug.LogError("[BinShifter-DEBUG] visualisationReference null.");
            return;
        }


        var dataSource = scatterplotVisualisation.visualisationReference.dataSource;

        ProcessAxisBinning(spec, "x");
        ProcessAxisBinning(spec, "y");
        ProcessAxisBinning(spec, "z");

        scatterplotVisualisation.UpdateVisualisation(AbstractVisualisation.PropertyType.None);

    }

    private void ProcessAxisBinning(ChartSpec spec, string axisName)
    {
        var binSpec = GetBinSpecForAxis(spec, axisName);
        if (binSpec == null)
        {
            Debug.LogWarning($"[BinShifter] No bin configuration for axis '{axisName}'.");
            return;
        }

        if (!binSpec.Enable)
        {
            return;
        }

        string fieldName = GetFieldNameForAxis(spec, axisName);
        if (string.IsNullOrEmpty(fieldName))
        {
            Debug.LogError($"[BinShifter] Field name is missing for axis '{axisName}'.");
            return;
        }


        var dataSource = scatterplotVisualisation.visualisationReference.dataSource;
        int dimensionIndex = -1;

        for (int i = 0; i < dataSource.DimensionCount; i++)
        {
            if (dataSource[i].Identifier.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
            {
                dimensionIndex = i;
                break;
            }
        }

        if (dimensionIndex == -1)
        {
            Debug.LogError($"[BinShifter] Field '{fieldName}' was not found in the data source.");
            return;
        }

        ApplyBinning(dimensionIndex, binSpec, axisName, spec);
    }

    private BinSpec GetBinSpecForAxis(ChartSpec spec, string axisName)
    {
        switch (axisName.ToLower())
        {
            case "x": return spec.Encoding.X?.Bin;
            case "y": return spec.Encoding.Y?.Bin;
            case "z": return spec.Encoding.Z?.Bin;
            default: return null;
        }
    }

    private string GetFieldNameForAxis(ChartSpec spec, string axisName)
    {
        switch (axisName.ToLower())
        {
            case "x": return spec.Encoding.X?.Field;
            case "y": return spec.Encoding.Y?.Field;
            case "z": return spec.Encoding.Z?.Field;
            default: return null;
        }
    }

    private void ApplyBinning(int dimensionIndex, BinSpec binSpec, string axisName, ChartSpec spec)
    {
        var dataSource = scatterplotVisualisation.visualisationReference.dataSource;
        int dataCount = dataSource.DataCount;

        float[] originalValues = new float[dataCount];
        for (int i = 0; i < dataCount; i++)
        {
            try
            {
                originalValues[i] = Convert.ToSingle(dataSource.getOriginalValue(
                    dataSource[dimensionIndex].Data[i],
                    dataSource[dimensionIndex].Identifier));
            }
            catch (Exception e)
            {
                Debug.LogError($"[BinShifter] Failed to convert source value: {e.Message}");
                return;
            }
        }

        float[] binnedValues = new float[dataCount];

        List<float> floatEdges = null;
        if (binSpec.Edges != null && binSpec.Edges.Count > 1)
        {
            floatEdges = binSpec.Edges.Select(e => (float)e).ToList();

            ApplyCustomBinEdges(originalValues, binnedValues, floatEdges, axisName);

        }
        else if (binSpec.Step.HasValue && binSpec.Step.Value > 0)
        {
            float step = (float)binSpec.Step.Value;

            float dataRange = originalValues.Max() - originalValues.Min();
            int estimatedBins = (int)(dataRange / step);
            if (estimatedBins > 1000)
            {
                Debug.LogWarning($"[BinShifter] Step value {step} may produce too many bins ({estimatedBins}).");
            }

            ApplyStepBinning(originalValues, binnedValues, step);
        }
        else
        {
            Debug.LogWarning("[BinShifter] Binning is enabled but step/edges are not valid.");
            return;
        }


        int nanCount = binnedValues.Count(float.IsNaN);
        int infCount = binnedValues.Count(float.IsInfinity);
        if (nanCount > 0 || infCount > 0)
            Debug.LogError($"[BinShifter] Invalid output values: NaN={nanCount}, Infinity={infCount}");


        UpdateBinnedValues(dimensionIndex, binnedValues, axisName, binSpec, spec);
    }




    private void ApplyStepBinning(float[] originalValues, float[] binnedValues, float step)
    {
        for (int i = 0; i < originalValues.Length; i++)
        {
            float binStart = Mathf.Floor(originalValues[i] / step) * step;
            binnedValues[i] = binStart + (step / 2);
        }
    }

    private void UpdateBinnedValues(int dimensionIndex, float[] binnedValues, string axisName, BinSpec binSpec, ChartSpec spec)
    {
        var viz = scatterplotVisualisation.visualisationReference;
        var dataSource = viz.dataSource;

        string dimensionName = dataSource[dimensionIndex].Identifier;
        float[] originalValues = new float[dataSource.DataCount];
        Array.Copy(dataSource[dimensionIndex].Data, originalValues, dataSource.DataCount);

        bool shouldNormalize = true;

        float minValue = binnedValues.Min();
        float maxValue = binnedValues.Max();
        float range = maxValue - minValue;

        if (shouldNormalize && range > 0)
        {
            for (int i = 0; i < binnedValues.Length; i++)
            {
                binnedValues[i] = (binnedValues[i] - minValue) / range;
            }

        }
        else if (!shouldNormalize)
        {
        }
        else
        {
            Debug.LogWarning($"[BinShifter] Range is zero for axis '{axisName}', normalization skipped.");
        }

        PlayerPrefs.SetString($"original_{axisName}_{dimensionName}", "saved");

        for (int i = 0; i < dataSource.DataCount; i++)
        {
            dataSource[dimensionIndex].Data[i] = binnedValues[i];
        }

        switch (axisName.ToLower())
        {
            case "x":
                viz.xDimension.Attribute = dimensionName;
                scatterplotVisualisation.UpdateVisualisation(ScatterplotVisualisation.PropertyType.X);
                break;
            case "y":
                viz.yDimension.Attribute = dimensionName;
                scatterplotVisualisation.UpdateVisualisation(ScatterplotVisualisation.PropertyType.Y);
                break;
            case "z":
                viz.zDimension.Attribute = dimensionName;
                scatterplotVisualisation.UpdateVisualisation(ScatterplotVisualisation.PropertyType.Z);
                break;
            default:
                Debug.LogError($"[BinShifter] Unknown axis '{axisName}'.");
                break;
        }


        PlayerPrefs.SetString($"binned_{axisName}_{dimensionName}", "saved");
    }
  private void BackupOriginalValues(string axisName, string dimensionName, float[] values)
    {
        string key = $"{axisName}_{dimensionName}";
        if (!originalDataCache.ContainsKey(key))
        {
            float[] backup = new float[values.Length];
            Array.Copy(values, backup, values.Length);
            originalDataCache[key] = backup;
        }
    }

    public void RestoreOriginalValues(string axisName)
    {
        var viz = scatterplotVisualisation.visualisationReference;
        string dimensionName = "";

        switch (axisName.ToLower())
        {
            case "x":
                dimensionName = viz.xDimension.Attribute;
                break;
            case "y":
                dimensionName = viz.yDimension.Attribute;
                break;
            case "z":
                dimensionName = viz.zDimension.Attribute;
                break;
        }

        string key = $"{axisName}_{dimensionName}";
        if (originalDataCache.ContainsKey(key))
        {
            var dataSource = viz.dataSource;
            float[] backup = originalDataCache[key];
            Array.Copy(backup, dataSource[dataSource[axisName == "x" ? 0 : axisName == "y" ? 1 : 2].Index].Data, backup.Length);

            scatterplotVisualisation.UpdateVisualisation(
                axisName == "x" ? ScatterplotVisualisation.PropertyType.X :
                axisName == "y" ? ScatterplotVisualisation.PropertyType.Y :
                ScatterplotVisualisation.PropertyType.Z
            );

        }
        else
        {
            Debug.LogWarning($"[BinShifter] No backup values found for axis '{axisName}'.");
        }
    }

    private void ApplyCustomBinEdges(float[] originalValues, float[] binnedValues, List<float> edges, string axisName)
    {
        var sortedEdges = edges.OrderBy(x => x).ToList();
        binLabels.Clear();

        for (int i = 1; i < sortedEdges.Count; i++)
        {
            float midpoint = (sortedEdges[i - 1] + sortedEdges[i]) / 2f;
            string label = $"{sortedEdges[i - 1]}-{sortedEdges[i]}";
            binLabels[midpoint] = label;
        }

        for (int i = 0; i < originalValues.Length; i++)
        {
            float value = originalValues[i];
            int binIndex = 0;
            while (binIndex < sortedEdges.Count && value > sortedEdges[binIndex])
                binIndex++;

            if (binIndex == 0)
                binnedValues[i] = sortedEdges[0];
            else if (binIndex >= sortedEdges.Count)
                binnedValues[i] = sortedEdges.Last();
            else
                binnedValues[i] = (sortedEdges[binIndex - 1] + sortedEdges[binIndex]) / 2f;
        }
    }
  
}
