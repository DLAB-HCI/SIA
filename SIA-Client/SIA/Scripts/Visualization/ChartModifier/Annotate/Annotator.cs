using UnityEngine;
using IATK;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class Annotator : MonoBehaviour
{
    public AbstractVisualisation scatterplotVisualisation;
    private Visualisation visualisationReference;
    private List<GameObject> annotationObjects = new List<GameObject>();
    public float textSize;
    public int fontSize;

    private bool areAnnotationsVisible = true;

    void Start()
    {
        InitializeVisualization();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            ToggleAnnotationsVisibility();
        }
    }

    private void InitializeVisualization()
    {
        scatterplotVisualisation = GetComponent<AbstractVisualisation>();
        if (scatterplotVisualisation == null)
        {
            enabled = false;
            return;
        }

        visualisationReference = scatterplotVisualisation.visualisationReference;
        if (visualisationReference == null)
        {
            enabled = false;
            return;
        }
    }

    public void Annotation(string jsonStructure)
    {
        ClearAnnotations();
        CreateAnnotationsFromDataStore();
    }

    private void ClearAnnotations()
    {
        foreach (var obj in annotationObjects)
        {
            if (obj != null) Destroy(obj);
        }
        annotationObjects.Clear();
    }

    private void CreateAnnotationsFromDataStore()
    {
        string jsonData = DataStore.Instance.GetTransformerData<string>("FullJsonData");
        if (string.IsNullOrEmpty(jsonData))
        {
            return;
        }

        JObject json = JObject.Parse(jsonData);
        JArray annotations = json["annotations"] as JArray;

        if (annotations == null)
        {
            return;
        }

        foreach (JObject annotation in annotations)
        {
            CreateAnnotation(annotation);
        }
    }

    private void CreateAnnotation(JObject annotation)
    {
        string field = annotation["field"].ToString();
        JObject line = annotation["line"] as JObject;
        JObject text = annotation["text"] as JObject;

        if (line != null)
        {
            CreateLineAnnotation(field, line);
        }

        if (text != null)
        {
            CreateTextAnnotation(field, text);
        }
    }

    private void CreateLineAnnotation(string field, JObject lineData)
    {
        Vector3 start = GetPositionFromFields(lineData["start"] as JObject);
        Vector3 end = GetPositionFromFields(lineData["end"] as JObject);
        Color color = ParseColor(lineData["color"].ToString());
        float width = (float)lineData["width"] * 1f;

        GameObject lineObject = DrawLine(start, end, color, width);
        lineObject.name = $"AnnotationLine_{field}";
        annotationObjects.Add(lineObject);
    }

    private void CreateTextAnnotation(string field, JObject textData)
    {
        Vector3 position = GetPositionFromFields(textData["position"] as JObject);
        string textTemplate = textData["text"].ToString();
        Color color = ParseColor(textData["color"].ToString());
        float fontSize = (float)textData["fontSize"] * 1f;

        foreach (var key in DataStore.Instance.GetTransformerDataKeys())
        {
            if (string.IsNullOrEmpty(key))
            {
                UnityEngine.Debug.LogError("Encountered an empty key in GetTransformerDataKeys().");
                continue; // Skip empty keys
            }
            if (textTemplate.Contains(key))
            {
                float normalizedValue = DataStore.Instance.GetTransformerData<float>(key);
                float value = DenormalizeValue(field, normalizedValue);

                string operatorFullName = ConvertOperatorToFullName(key);

                string finalText;
                if (key.StartsWith("percentile_"))
                {
                    string percentileKey = $"{key}_percentile";
                    float percentilePercentage = DataStore.Instance.GetTransformerData<float>(percentileKey);
                    finalText = $"{operatorFullName} {field}: {value.ToString("F0")} ({percentilePercentage.ToString("F0")}%)";
                }
                else
                {
                    finalText = $"{operatorFullName} {field}: {value.ToString("F0")}";
                }

                textTemplate = textTemplate.Replace(key, finalText);
            }
        }

        GameObject textObject = CreateTextMesh(position, textTemplate, color, fontSize);
        textObject.name = $"AnnotationText_{field}";
        annotationObjects.Add(textObject);
    }

    private string ConvertOperatorToFullName(string key)
    {
        if (key.StartsWith("mean_"))
            return "MEAN";
        else if (key.StartsWith("min_"))
            return "MIN.";
        else if (key.StartsWith("max_"))
            return "MAX";
        else if (key.StartsWith("sum_"))
            return "SUM";
        else if (key.StartsWith("range_"))
            return "RANGE";
        else if (key.StartsWith("percentile_"))
            return "PERCENTILE";  
        else if (key.StartsWith("median_"))
            return "MEDIAN";        
        else if (key.StartsWith("count_"))
            return "COUNT";
        else if (key.StartsWith("stdev_"))
            return "STANDARD DEVIATION";
        else
            return key;
    }

    private int DenormalizeValue(string field, float normalizedValue)
    {
        if (scatterplotVisualisation?.visualisationReference == null)
        {
            return Mathf.FloorToInt(normalizedValue);
        }

        var vis = scatterplotVisualisation.visualisationReference;
        var dataSource = vis.dataSource;

        DataSource.DimensionData dimension;
        try
        {
            dimension = dataSource[field];
        }
        catch (System.Exception)
        {
            return Mathf.FloorToInt(normalizedValue);
        }

        DimensionFilter dimensionFilter = null;
        if (field == vis.xDimension.Attribute)
            dimensionFilter = vis.xDimension;
        else if (field == vis.yDimension.Attribute)
            dimensionFilter = vis.yDimension;
        else if (field == vis.zDimension.Attribute)
            dimensionFilter = vis.zDimension;

        if (dimensionFilter == null)
        {
            return Mathf.FloorToInt(normalizedValue);
        }

        float scaleMin = dimensionFilter.minScale;
        float scaleMax = dimensionFilter.maxScale;

        float denormalizedValue = Mathf.Lerp(scaleMin, scaleMax, normalizedValue);

        object originalValue = dataSource.getOriginalValue(denormalizedValue, field);
        int finalValue = (originalValue is float floatValue) ? Mathf.RoundToInt(floatValue) :
                        (originalValue is double doubleValue) ? Mathf.RoundToInt((float)doubleValue) :
                        Mathf.RoundToInt(denormalizedValue);
        return finalValue;
    }

    private Vector3 GetPositionFromFields(JObject positionData)
    {
        float x = GetValueForField(positionData["x"].ToString());
        float y = GetValueForField(positionData["y"].ToString());
        float z = GetValueForField(positionData["z"].ToString());

        Vector3 position = new Vector3(x, y, z);
        return position;
    }

    private float GetValueForField(string field)
    {
        if (field.StartsWith("mean_") || field.StartsWith("min_") || field.StartsWith("max_") || field.StartsWith("sum_") || field.StartsWith("range_") || field.StartsWith("percentile_") || field.StartsWith("median_") || field.StartsWith("count_") || field.StartsWith("stdev_"))
        {
            float normalizedValue = DataStore.Instance.GetTransformerData<float>(field);
            return normalizedValue;
        }
        else if (float.TryParse(field, out float result))
        {
            return result;
        }
        else
        {
            return 0f;
        }
    }

    private Color ParseColor(string colorString)
    {
        if (ColorUtility.TryParseHtmlString(colorString, out Color color))
        {
            return color;
        }
        return Color.white;
    }

    private GameObject DrawLine(Vector3 start, Vector3 end, Color color, float width)
    {
        GameObject lineObject = new GameObject("AnnotationLine");
        lineObject.transform.SetParent(scatterplotVisualisation.transform, false);
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = lineRenderer.endColor = color;
        lineRenderer.startWidth = lineRenderer.endWidth = width;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = false;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        return lineObject;
    }

    private GameObject CreateTextMesh(Vector3 position, string text, Color color, float fontSize)
    {
        GameObject textObject = new GameObject("AnnotationText");
        textObject.transform.SetParent(scatterplotVisualisation.transform, false);
        textObject.transform.localPosition = position;

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.color = color;
        textMesh.fontSize = Mathf.RoundToInt(fontSize * 40000.0f);
        textMesh.anchor = TextAnchor.MiddleLeft;
        textMesh.alignment = TextAlignment.Left;
        textMesh.characterSize = 0.01f;

        textObject.AddComponent<Billboard>();

        return textObject;
    }

    private void ToggleAnnotationsVisibility()
    {
        areAnnotationsVisible = !areAnnotationsVisible;

        foreach (var annotation in annotationObjects)
        {
            var renderer = annotation.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color annotationColor = renderer.material.color;
                annotationColor.a = areAnnotationsVisible ? 1.0f : 0.0f;
                renderer.material.color = annotationColor;
            }
        }
    }
}

public class Billboard : MonoBehaviour
{
    void Update()
    {
        transform.LookAt(Camera.main.transform);
        transform.Rotate(0, 180, 0);
    }
}
