using UnityEngine;
using Newtonsoft.Json.Linq;
using IATK;
using System;

[RequireComponent(typeof(Visualisation))]
public class SizeShifter : MonoBehaviour
{
    private float size = 0.3f;
    private float minSize = 0.1f;
    private float maxSize = 1f;
    private string sizeDimension = "Undefined";

    private Visualisation visualisation;

    private void Awake()
    {
        visualisation = GetComponent<Visualisation>();
    }

    public void SizeShift(string jsonStructure)
    {
        if (string.IsNullOrEmpty(jsonStructure))
        {
            Debug.LogError("JSON structure is empty or null!");
            return;
        }

        try
        {
            JObject spec = JObject.Parse(jsonStructure);

            if (spec.ContainsKey("encoding") && spec["encoding"] is JObject encoding)
            {
                if (encoding.ContainsKey("size") && encoding["size"] is JObject sizeEncoding)
                {
                    string sizeField = sizeEncoding["field"]?.Value<string>() ?? "Undefined";

                    float sizeValue = sizeEncoding["value"]?.Value<float>() ?? size;

                    SetSize(sizeValue);

                    float extractedMinSize = minSize;
                    float extractedMaxSize = maxSize;

                    if (sizeEncoding.ContainsKey("scale") && sizeEncoding["scale"] is JObject scale)
                    {
                        if (scale.ContainsKey("range") && scale["range"] is JArray range && range.Count == 2)
                        {
                            extractedMinSize = range[0]?.Value<float>() ?? minSize;
                            extractedMaxSize = range[1]?.Value<float>() ?? maxSize;
                            SetMinSize(extractedMinSize);
                            SetMaxSize(extractedMaxSize);
                        }
                    }

                    SetSizeDimension(sizeField);

                    ApplySizeChanges();

                    Debug.Log($"Size updated: size={size}, minSize={minSize}, maxSize={maxSize}, sizeDimension={sizeDimension}");
                }
                else
                {
                    Debug.LogError("Size encoding not found in the JSON structure.");
                }
            }
            else
            {
                Debug.LogError("Encoding section not found in the JSON structure.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse JSON structure: {ex.Message}");
        }
    }

    private void ApplySizeChanges()
    {
        if (visualisation != null)
        {
            visualisation.size = size;
            visualisation.minSize = minSize;
            visualisation.maxSize = maxSize;
            visualisation.sizeDimension = sizeDimension;

            visualisation.updateViewProperties(AbstractVisualisation.PropertyType.Size);
            visualisation.updateViewProperties(AbstractVisualisation.PropertyType.SizeValues);
        }
        else
        {
            Debug.LogError("Visualization component is not assigned.");
        }
    }

    public void SetSize(float newSize)
    {
        size = Mathf.Clamp(newSize, 0.001f, 1f);
    }

    public void SetMinSize(float newMinSize)
    {
        minSize = Mathf.Clamp(newMinSize, 0.001f, 1f);
    }

    public void SetMaxSize(float newMaxSize)
    {
        maxSize = Mathf.Clamp(newMaxSize, 0.001f, 1f);
    }

    public void SetSizeDimension(string newSizeDimension)
    {
        sizeDimension = string.IsNullOrEmpty(newSizeDimension) ? "Undefined" : newSizeDimension;
    }
}







































