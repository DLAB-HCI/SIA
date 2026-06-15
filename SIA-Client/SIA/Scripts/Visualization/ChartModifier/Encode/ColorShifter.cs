using UnityEngine;
using Newtonsoft.Json.Linq;
using IATK;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using EmbodiedNLI.Visualization; // ChartSpec, ColorSpec 

public class ColorShifter : MonoBehaviour
{
    private ScatterplotVisualisation scatterplotVisualisation;

    void Start()
    {
        scatterplotVisualisation = GetComponent<ScatterplotVisualisation>();
        if (scatterplotVisualisation == null)
        {
            Debug.LogError("Failed to get ScatterplotVisualisation component");
        }
        else
        {
            Debug.Log("ScatterplotVisualisation component found");
        }
    }

    public void ColorShift(ChartSpec spec)
    {
        Debug.Log("[ColorShifter] ColorShift ");
        if (scatterplotVisualisation == null)
        {
            scatterplotVisualisation = GetComponent<ScatterplotVisualisation>();
        }

        if (scatterplotVisualisation == null)
        {
            Debug.LogError("[ColorShifter] ScatterplotVisualisation is missing.");
            return;
        }

        if (spec == null || spec.Encoding == null || spec.Encoding.Color == null)
        {
            Debug.LogWarning("[ColorShifter] Spec or encoding.color is missing. Keep current colors.");
            return;
        }
        Debug.Log("[ColorShifter] Spec in colorshifter: " + spec.ToString());
        Debug.Log("[ColorShifter] condition count: " + (spec.Encoding.Color.Condition?.Count ?? 0));

        var colorSpec = spec.Encoding.Color;
        string defaultColor = colorSpec.Value;
        var scaleSpec = colorSpec.Scale;



        if (scaleSpec?.Domain != null)
        {
            var domainArray = scaleSpec.Domain as JArray;
            if (domainArray != null && domainArray.Count > 0)
            {
                var first = domainArray.First;

                if (first.Type == JTokenType.String)
                {
                    List<string> categoryDomain = domainArray.ToObject<List<string>>();
                    Debug.Log("[ColorShifter] Categorical domain: " + string.Join(", ", categoryDomain));
                }
                else if (first.Type == JTokenType.Float || first.Type == JTokenType.Integer)
                {
                    List<double> numericDomain = domainArray.ToObject<List<double>>();
                    Debug.Log("[ColorShifter] Numeric domain: " + string.Join(", ", numericDomain));
                }
                else
                {
                    Debug.LogWarning("[ColorShifter] Unknown domain type: " + first.Type);
                }
            }
        }

        var conditions = colorSpec.Condition ?? new List<ColorConditionSpec>();
        var dataSource = scatterplotVisualisation.visualisationReference.dataSource;
        int dataCount = dataSource.DataCount;
        Color[] colors = Enumerable.Repeat(ParseColor(defaultColor), dataCount).ToArray();

        for (int i = 0; i < dataSource.DimensionCount; i++)
        {
            Debug.Log($"[Check] Column {i}: '{dataSource[i].Identifier}'");
        }
        
        if (conditions.Count > 0)
        {
            foreach (var cond in conditions)
            {
                var targetDimensions = GetTargetDimensions(cond.Test);
                var columnIndices = new Dictionary<string, int>();
                foreach (string dim in targetDimensions)
                    for (int i = 0; i < dataSource.DimensionCount; i++)
                        if (dataSource[i].Identifier.Equals(dim, StringComparison.OrdinalIgnoreCase))
                            columnIndices[dim] = i;

                for (int i = 0; i < dataCount; i++)
                {
                    if (colors[i] != ParseColor(defaultColor)) continue;

                    bool met = EvaluateComplexCondition(dataSource, columnIndices, i, cond.Test);
                    if (met)
                        colors[i] = ParseColor(cond.Value);
                }
            }

            foreach (var view in scatterplotVisualisation.viewList)
                view.SetColors(colors);

            scatterplotVisualisation.visualisationReference.colourArray = colors;
            scatterplotVisualisation.UpdateVisualisation(ScatterplotVisualisation.PropertyType.Colour);
            DataStore.Instance.SetCurrentColourArray("colourArray", colors);
            Debug.Log("Updated colourArray saved to DataStore.");
        }
        else
        {
            Color newColor = ParseColor(defaultColor);
            UpdateColor(newColor);
            Debug.Log("default: " + newColor);
        }






    }

    private Color ParseColor(string colorName)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(colorName, out color))
        {
            return color;
        }
        return ParseColor("#717171ff"); // DarkGray (HTML )

    }

    private void UpdateColor(Color newColor)
    {
        scatterplotVisualisation.visualisationReference.colour = newColor;
        scatterplotVisualisation.UpdateVisualisation(ScatterplotVisualisation.PropertyType.Colour);
        Debug.Log("Color changed to: " + newColor);

        DataStore.Instance.SetCurrentColourArray("colourArray", scatterplotVisualisation.visualisationReference.colourArray);
        Debug.Log("Updated colourArray saved to DataStore.");

    }

    private void ApplyConditionalColor(string vegaLiteCondition, string conditionColor, string defaultColor)
    {
        var dataSource = scatterplotVisualisation.visualisationReference.dataSource;
        var dataCount = dataSource.DataCount;

        List<string> targetDimensions = GetTargetDimensions(vegaLiteCondition);

        if (targetDimensions.Count > 0)
        {
            Dictionary<string, int> columnIndices = new Dictionary<string, int>();
            foreach (string dimension in targetDimensions)
            {
                for (int i = 0; i < dataSource.DimensionCount; i++)
                {
                    if (dataSource[i].Identifier.ToLower() == dimension.ToLower())
                    {
                        columnIndices[dimension] = i;
                        break;
                    }
                }
            }

            Color[] colors = new Color[dataCount];

            for (int dataPointIndex = 0; dataPointIndex < dataCount; dataPointIndex++)
            {
                bool conditionMet = EvaluateComplexCondition(dataSource, columnIndices, dataPointIndex, vegaLiteCondition);
                Color color = conditionMet ? ParseColor(conditionColor) : ParseColor(defaultColor);
                colors[dataPointIndex] = color;
            }

            foreach (var viewElement in scatterplotVisualisation.viewList)
            {
                viewElement.SetColors(colors);
            }

            scatterplotVisualisation.visualisationReference.colourArray = colors;
            scatterplotVisualisation.UpdateVisualisation(ScatterplotVisualisation.PropertyType.Colour);

            DataStore.Instance.SetCurrentColourArray("colourArray", scatterplotVisualisation.visualisationReference.colourArray);
            Debug.Log("Updated colourArray saved to DataStore.");
        }
    }

    private List<string> GetTargetDimensions(string vegaLiteCondition)
    {
        List<string> dimensions = new List<string>();
        string[] conditionParts = vegaLiteCondition.Split(new[] { "||", "&&" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string condition in conditionParts)
        {
            string[] subConditionParts = condition.Split(
                new[] { "==", "!=", ">", "<", ">=", "<=" },
                StringSplitOptions.RemoveEmptyEntries);

            if (subConditionParts.Length == 2)
            {
                string leftOperand = subConditionParts[0].Trim();

                if (leftOperand.StartsWith("datum."))
                {
                    string fieldName = leftOperand.Substring("datum.".Length).Trim();
                    dimensions.Add(fieldName);
                }
                else if (leftOperand.StartsWith("datum['") && leftOperand.EndsWith("']"))
                {
                    string fieldName = leftOperand.Substring(7, leftOperand.Length - 9).Trim();
                    dimensions.Add(fieldName);
                }
            }
        }

        return dimensions;
    }


    private bool EvaluateComplexCondition(DataSource dataSource, Dictionary<string, int> columnIndices, int dataIndex, string complexCondition)
    {
        string[] andConditions = complexCondition.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string andCondition in andConditions)
        {
            string[] orConditions = andCondition.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
            bool orResult = false;

            foreach (string condition in orConditions)
            {
                string trimmedCondition = condition.Trim();
                string dimension = GetDimensionFromCondition(trimmedCondition);

                if (columnIndices.TryGetValue(dimension, out int columnIndex))
                {
                    object value = dataSource.getOriginalValue(dataSource[columnIndex].Data[dataIndex], dataSource[columnIndex].Identifier);
                    bool evaluationResult = EvaluateCSharpCondition(value, trimmedCondition, dataIndex);
                    orResult |= evaluationResult;

                    if (orResult) break;  // Short-circuit OR evaluation
                }
            }

            if (!orResult) return false;  // If any AND condition is false, the whole expression is false
        }

        return true;
    }

    private string GetDimensionFromCondition(string condition)
    {
        string[] parts = condition.Split(new[] { "==", "!=", ">", "<", ">=", "<=" }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0)
        {
            return parts[0].Replace("datum.", "").Trim();
        }
        return string.Empty;
    }








   private bool EvaluateCSharpCondition(object originalValue, string vegaLiteCondition, int dataIndex)
{
    vegaLiteCondition = vegaLiteCondition.Replace("datum.", "");
    vegaLiteCondition = System.Text.RegularExpressions.Regex.Replace(vegaLiteCondition, "\\s+", " ");

    if (originalValue is float || originalValue is int || originalValue is double)
    {
        float floatValue = Convert.ToSingle(originalValue);

        if (vegaLiteCondition.Contains(">=") || vegaLiteCondition.Contains("<=") ||
            vegaLiteCondition.Contains(">") || vegaLiteCondition.Contains("<"))
        {
            float comparisonValue = ExtractNumericValue(vegaLiteCondition);
            if (vegaLiteCondition.Contains(">=")) return floatValue >= comparisonValue;
            if (vegaLiteCondition.Contains("<=")) return floatValue <= comparisonValue;
            if (vegaLiteCondition.Contains(">")) return floatValue > comparisonValue;
            if (vegaLiteCondition.Contains("<")) return floatValue < comparisonValue;
        }
        else if (vegaLiteCondition.Contains("==") || vegaLiteCondition.Contains("!="))
        {
            string rhsRaw = ExtractStringValue(vegaLiteCondition);

            if (rhsRaw.Contains("-"))
            {
                var parts = rhsRaw.Split('-');
                if (parts.Length == 2 &&
                    float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float low) &&
                    float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float high))
                {
                    if (vegaLiteCondition.Contains("==")) return floatValue >= low && floatValue <= high;
                    if (vegaLiteCondition.Contains("!=")) return floatValue < low || floatValue > high;
                }
            }

            if (float.TryParse(rhsRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out float compVal))
            {
                if (vegaLiteCondition.Contains("==")) return Math.Abs(floatValue - compVal) < float.Epsilon;
                if (vegaLiteCondition.Contains("!=")) return Math.Abs(floatValue - compVal) >= float.Epsilon;
            }
        }
    }
    else if (originalValue is string)
    {
        string stringValue = originalValue.ToString();
        string comparisonValue = ExtractStringValue(vegaLiteCondition);

        if (vegaLiteCondition.Contains("=="))
            return string.Equals(stringValue, comparisonValue, StringComparison.OrdinalIgnoreCase);
        if (vegaLiteCondition.Contains("!="))
            return !string.Equals(stringValue, comparisonValue, StringComparison.OrdinalIgnoreCase);
    }

    return false;
}



    private float ExtractNumericValue(string condition)
    {
        string[] parts = condition.Split(
            new[] { ">=", "<=", ">", "<", "==", "!=" },
            StringSplitOptions.RemoveEmptyEntries
        );

        if (parts.Length == 2)
        {
            string rhs = parts[1].Trim().Trim('\'', '"');

            if (float.TryParse(rhs, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                return v;

            if (float.TryParse(rhs, NumberStyles.Float, CultureInfo.CurrentCulture, out v))
                return v;
        }

        throw new FormatException($"Cannot extract numeric value from condition: {condition}");
    }

    private string ExtractStringValue(string condition)
    {
        string[] parts = condition.Split(new[] { "==", "!=" }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            return parts[1].Trim().Trim('\'', '"');
        }
        throw new FormatException($"Cannot extract string value from condition: {condition}");
    }

    void Update()
    {
    }
}




































































        

                






            





