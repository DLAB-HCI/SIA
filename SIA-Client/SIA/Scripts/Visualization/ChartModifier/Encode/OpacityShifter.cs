// using UnityEngine;
// using Newtonsoft.Json.Linq;
// using IATK;
// using System;
// using System.Linq;
// using System.Collections.Generic;

// public class OpacityShifter : MonoBehaviour
// {
//     private ScatterplotVisualisation scatterplotVisualisation;

//     void Start()
//     {
//         scatterplotVisualisation = GetComponent<ScatterplotVisualisation>();
//         if (scatterplotVisualisation == null)
//         {
//             Debug.LogError("Failed to get ScatterplotVisualisation component");
//         }
//         else
//         {
//             Debug.Log("ScatterplotVisualisation component found");
//         }
//     }

//     public void OpacityShift(string jsonStructure)
//     {
//         if (!string.IsNullOrEmpty(jsonStructure))
//         {
//             JObject spec = JObject.Parse(jsonStructure);

//             if (spec.ContainsKey("encoding") && spec["encoding"].ToObject<JObject>().ContainsKey("opacity"))
//             {
//                 JToken opacityToken = spec["encoding"]["opacity"];
//                 float defaultOpacity = 1f;

//                 if (opacityToken.ToObject<JObject>().ContainsKey("value"))
//                 {
//                     defaultOpacity = (float)opacityToken["value"];
//                 }

//                 if (opacityToken.ToObject<JObject>().ContainsKey("condition") && opacityToken["condition"].ToObject<JObject>().ContainsKey("test"))
//                 {
//                     string vegaLiteCondition = opacityToken["condition"]["test"].ToString();
//                     float conditionOpacity = (float)opacityToken["condition"]["value"];
//                     ApplyConditionalOpacity(vegaLiteCondition, conditionOpacity, defaultOpacity);
//                 }
//                 else
//                 {
//                     UpdateOpacity(defaultOpacity);
//                 }
//             }
//             else
//             {
//                 Debug.LogError("JSON structure is not accurate!");
//             }
//         }
//         else
//         {
//             Debug.LogError("JSON structure is empty!");
//         }
//     }

//     private void UpdateOpacity(float opacity)
//     {
//         var dataSource = scatterplotVisualisation.visualisationReference.dataSource;
//         var dataCount = dataSource.DataCount;
//         Color[] colors = new Color[dataCount];

//         // Update the alpha value of existing colors
//         for (int i = 0; i < dataCount; i++)
//         {
//             Color originalColor = scatterplotVisualisation.visualisationReference.colourArray[i];
//             colors[i] = new Color(originalColor.r, originalColor.g, originalColor.b, opacity);
//         }

//         foreach (var viewElement in scatterplotVisualisation.viewList)
//         {
//             viewElement.SetColors(colors);
//         }

//         scatterplotVisualisation.visualisationReference.colourArray = colors;
//         scatterplotVisualisation.UpdateVisualisation(ScatterplotVisualisation.PropertyType.Colour);

//         // Save color values to DataStore
//         DataStore.Instance.SetCurrentColourArray("colourArray", scatterplotVisualisation.visualisationReference.colourArray);
//         Debug.Log("Updated colourArray saved to DataStore.");
//     }

//     private void ApplyConditionalOpacity(string vegaLiteCondition, float conditionOpacity, float defaultOpacity)
//     {
//         var dataSource = scatterplotVisualisation.visualisationReference.dataSource;
//         var dataCount = dataSource.DataCount;

//         List<string> targetDimensions = GetTargetDimensions(vegaLiteCondition);

//         if (targetDimensions.Count > 0)
//         {
//             Dictionary<string, int> columnIndices = new Dictionary<string, int>();
//             foreach (string dimension in targetDimensions)
//             {
//                 for (int i = 0; i < dataSource.DimensionCount; i++)
//                 {
//                     if (dataSource[i].Identifier.ToLower() == dimension.ToLower())
//                     {
//                         columnIndices[dimension] = i;
//                         break;
//                     }
//                 }
//             }

//             Color[] colors = new Color[dataCount];

//             for (int dataPointIndex = 0; dataPointIndex < dataCount; dataPointIndex++)
//             {
//                 bool conditionMet = EvaluateComplexCondition(dataSource, columnIndices, dataPointIndex, vegaLiteCondition);
//                 float opacity = conditionMet ? conditionOpacity : defaultOpacity;

//                 // Change only the alpha value of existing colors
//                 Color originalColor = scatterplotVisualisation.visualisationReference.colourArray[dataPointIndex];
//                 colors[dataPointIndex] = new Color(originalColor.r, originalColor.g, originalColor.b, opacity);
//             }

//             foreach (var viewElement in scatterplotVisualisation.viewList)
//             {
//                 viewElement.SetColors(colors);
//             }

//             scatterplotVisualisation.visualisationReference.colourArray = colors;
//             scatterplotVisualisation.UpdateVisualisation(ScatterplotVisualisation.PropertyType.Colour);

//             // Save color values to DataStore
//             DataStore.Instance.SetCurrentColourArray("colourArray", scatterplotVisualisation.visualisationReference.colourArray);
//             Debug.Log("Updated colourArray saved to DataStore.");
//         }
//     }

//     private List<string> GetTargetDimensions(string vegaLiteCondition)
//     {
//         List<string> dimensions = new List<string>();
//         string[] conditionParts = vegaLiteCondition.Split(new[] { "||", "&&" }, StringSplitOptions.RemoveEmptyEntries);

//         foreach (string condition in conditionParts)
//         {
//             string[] subConditionParts = condition.Split(new[] { "==", "!=", ">", "<", ">=", "<=" }, StringSplitOptions.RemoveEmptyEntries);

//             if (subConditionParts.Length == 2)
//             {
//                 string leftOperand = subConditionParts[0].Trim();

//                 if (leftOperand.StartsWith("datum."))
//                 {
//                     string fieldName = leftOperand.Substring("datum.".Length);
//                     dimensions.Add(fieldName);
//                 }
//             }
//         }

//         return dimensions;
//     }

//     private bool EvaluateComplexCondition(DataSource dataSource, Dictionary<string, int> columnIndices, int dataIndex, string complexCondition)
//     {
//         string[] andConditions = complexCondition.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);

//         foreach (string andCondition in andConditions)
//         {
//             string[] orConditions = andCondition.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
//             bool orResult = false;

//             foreach (string condition in orConditions)
//             {
//                 string trimmedCondition = condition.Trim();
//                 string dimension = GetDimensionFromCondition(trimmedCondition);

//                 if (columnIndices.TryGetValue(dimension, out int columnIndex))
//                 {
//                     object value = dataSource.getOriginalValue(dataSource[columnIndex].Data[dataIndex], dataSource[columnIndex].Identifier);
//                     bool evaluationResult = EvaluateCSharpCondition(value, trimmedCondition);
//                     orResult |= evaluationResult;

//                     if (orResult) break;  // Short-circuit OR evaluation
//                 }
//             }

//             if (!orResult) return false;  // If any AND condition is false, the whole expression is false
//         }

//         return true;
//     }

//     private string GetDimensionFromCondition(string condition)
//     {
//         string[] parts = condition.Split(new[] { "==", "!=", ">", "<", ">=", "<=" }, StringSplitOptions.RemoveEmptyEntries);
//         if (parts.Length > 0)
//         {
//             return parts[0].Replace("datum.", "").Trim();
//         }
//         return string.Empty;
//     }

//     private bool EvaluateCSharpCondition(object originalValue, string vegaLiteCondition)
//     {
//         vegaLiteCondition = vegaLiteCondition.Replace("datum.", "");

//         if (originalValue is float || originalValue is int || originalValue is double)
//         {
//             float floatValue = Convert.ToSingle(originalValue);
//             float comparisonValue = ExtractNumericValue(vegaLiteCondition);

//             if (vegaLiteCondition.Contains(">="))
//                 return floatValue >= comparisonValue;
//             if (vegaLiteCondition.Contains("<="))
//                 return floatValue <= comparisonValue;
//             if (vegaLiteCondition.Contains(">"))
//                 return floatValue > comparisonValue;
//             if (vegaLiteCondition.Contains("<"))
//                 return floatValue < comparisonValue;
//             if (vegaLiteCondition.Contains("=="))
//                 return Math.Abs(floatValue - comparisonValue) < float.Epsilon;
//             if (vegaLiteCondition.Contains("!="))
//                 return Math.Abs(floatValue - comparisonValue) >= float.Epsilon;
//         }
//         else if (originalValue is string)
//         {
//             string stringValue = originalValue.ToString();
//             string comparisonValue = ExtractStringValue(vegaLiteCondition);

//             if (vegaLiteCondition.Contains("=="))
//                 return string.Equals(stringValue, comparisonValue, StringComparison.OrdinalIgnoreCase);
//             if (vegaLiteCondition.Contains("!="))
//                 return !string.Equals(stringValue, comparisonValue, StringComparison.OrdinalIgnoreCase);
//         }

//         return false;
//     }

//     private float ExtractNumericValue(string condition)
//     {
//         string[] parts = condition.Split(new[] { ">=", "<=", ">", "<", "==", "!=" }, StringSplitOptions.RemoveEmptyEntries);
//         if (parts.Length == 2 && float.TryParse(parts[1].Trim(), out float numericValue))
//         {
//             return numericValue;
//         }
//         throw new FormatException($"Cannot extract numeric value from condition: {condition}");
//     }

//     private string ExtractStringValue(string condition)
//     {
//         string[] parts = condition.Split(new[] { "==", "!=" }, StringSplitOptions.RemoveEmptyEntries);
//         if (parts.Length == 2)
//         {
//             return parts[1].Trim().Trim('\'', '"');
//         }
//         throw new FormatException($"Cannot extract string value from condition: {condition}");
//     }

//     void Update()
//     {
//         // Update logic (if needed)
//     }
// }









































