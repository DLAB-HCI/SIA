using UnityEngine;
using IATK;
using System.Collections.Generic;
using System.Linq;

public class DataLabel : MonoBehaviour
{
    [Header("Visualisation Settings")]
    public AbstractVisualisation scatterplotVisualisation;
    public float labelOffset = 0.5f;

    [Header("Label Settings")]
    public float textSize;
    public int fontSize;
    public Color normalColor = Color.black;
    public Color highlightColor = Color.red;

    [Header("Line Settings")]
    public float lineWidth = 0.001f;
    public Color lineColor = Color.black;

    [Header("Layout Settings")]
    public float overlapThreshold = 0.2f;
    public float spacingMultiplier = 1.5f;

    private List<GameObject> labels = new List<GameObject>();
    private List<LineRenderer> lines = new List<LineRenderer>();
    private Selector selector;
    private string selectedDataIndex = "";
    private DataStore dataStore;

    void Start()
    {
        InitializeComponents();
    }

    void Update()
    {
        UpdateLabels();
        UpdateSelectedDataIndex();
    }

    void InitializeComponents()
    {
        if (scatterplotVisualisation == null)
        {
            scatterplotVisualisation = FindObjectOfType<AbstractVisualisation>();
        }

        selector = FindObjectOfType<Selector>();
        if (selector == null)
        {
            Debug.LogError("Selector instance not found.");
        }

        dataStore = DataStore.Instance;
        if (dataStore == null)
        {
            Debug.LogError("DataStore instance not found.");
        }
    }

    void UpdateSelectedDataIndex()
    {
        if (dataStore == null)
        {
            Debug.LogError("DataStore is null in UpdateSelectedDataIndex.");
            selectedDataIndex = "";
            return;
        }

        List<int> nearbyPointIndices = dataStore.GetSelectedData<List<int>>("NearbyPointIndices");
        var selectedIndices = nearbyPointIndices?.Take(1).ToList() ?? new List<int>();

        if (selectedIndices.Count > 0)
        {
            selectedDataIndex = selectedIndices[0].ToString();
        }
        else
        {
            selectedDataIndex = "";
        }
    }

public void UpdateLabels()
{
    List<string> savedIds = DataStore.Instance.GetSavedData<List<string>>("SavedIds") ?? new List<string>();
    List<string> allIds = new List<string>(savedIds);

    if (!string.IsNullOrEmpty(selectedDataIndex) && !allIds.Contains(selectedDataIndex))
    {
        allIds.Add(selectedDataIndex);
    }

    List<string> detailIds = DataStore.Instance.GetDetailData<List<string>>("DetailIds") ?? new List<string>();



    while (labels.Count < allIds.Count)
    {
        CreateLabelObject();
    }
    while (labels.Count > allIds.Count)
    {
        DestroyLabelObject(labels.Count - 1);
    }

    List<Vector3> usedPositions = new List<Vector3>();
    bool isSavedPanelActive = DataStore.Instance.IsSavedPanelActive();

    for (int i = 0; i < allIds.Count; i++)
    {
        string id = allIds[i];
        if (int.TryParse(id, out int index))
        {
            bool isInFilterRange = selector != null && selector.FilterActivePoints.Contains(index);
            bool isActive = isInFilterRange;

            if (isSavedPanelActive)
            {
                if (savedIds.Contains(id))
                {
                    labels[i].SetActive(true);
                    lines[i].gameObject.SetActive(true);
                }
                else
                {
                    labels[i].SetActive(false);
                    lines[i].gameObject.SetActive(false);
                }
            }
            else
            {
                if (isActive)
                {
                    bool isInScaleRange = selector.ScaleActivePoints.Contains(index);

                    if (isInScaleRange)
                    {
                        labels[i].SetActive(true);
                        lines[i].gameObject.SetActive(true);

                        bool isDetailId = detailIds.Contains(id);
                        UpdateLabel(labels[i], lines[i], index, usedPositions, isDetailId);
                    }
                    else
                    {
                        labels[i].SetActive(false);
                        lines[i].gameObject.SetActive(false);
                    }
                }
                else
                {
                    labels[i].SetActive(false);
                    lines[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            Debug.LogWarning($"Failed to parse ID: {id}");
        }
    }
}


    void CreateLabelObject()
    {
        GameObject labelObj = new GameObject("LabelObject");
        labelObj.transform.SetParent(transform);
        
        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.characterSize = textSize;
        textMesh.fontSize = fontSize;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.color = normalColor;

        labelObj.AddComponent<Billboard>();

        labels.Add(labelObj);

        GameObject lineObj = new GameObject("ConnectionLine");
        lineObj.transform.SetParent(transform);
        LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = lineRenderer.endColor = lineColor;
        lineRenderer.startWidth = lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 2;
        lines.Add(lineRenderer);
    }

    void DestroyLabelObject(int index)
    {
        if (index >= 0 && index < labels.Count)
        {
            Destroy(labels[index]);
            labels.RemoveAt(index);
        }

        if (index >= 0 && index < lines.Count)
        {
            Destroy(lines[index].gameObject);
            lines.RemoveAt(index);
        }
    }

void UpdateLabel(GameObject labelObj, LineRenderer lineRenderer, int index, List<Vector3> usedPositions, bool isDetailId)
{
    Vector3 dataPosition = GetDataPointPosition(index);
    Vector3 labelPosition = dataPosition + Vector3.up * (labelOffset * 0.5f);

    labelPosition = AdjustLabelPosition(labelPosition, usedPositions, textSize, labelObj);
    usedPositions.Add(labelPosition);

    labelObj.transform.position = labelPosition;

    bool isSavedPanelActive = DataStore.Instance.IsSavedPanelActive();
    
    TextMesh textMesh = labelObj.GetComponent<TextMesh>();
    if (textMesh != null)
    {
        if (index.ToString() == selectedDataIndex)
        {
            textMesh.text = $"House ID {index}";
            textMesh.color = highlightColor;
            textMesh.fontStyle = FontStyle.Bold;
        }
        else
        {
            textMesh.text = $"{index}";
            textMesh.color = normalColor;
            textMesh.fontStyle = FontStyle.Normal;
        }

        textMesh.characterSize = isDetailId ? textSize * 0.12f : textSize * 0.08f;
        textMesh.fontSize = isDetailId ? Mathf.RoundToInt(fontSize * 6.0f) : Mathf.RoundToInt(fontSize * 6.0f);
    }

    lineRenderer.SetPosition(0, dataPosition);
    lineRenderer.SetPosition(1, labelPosition);

    if (isSavedPanelActive && index.ToString() == selectedDataIndex)
    {
        Color lineColorWithAlpha = lineColor;
        lineColorWithAlpha.a = 0f;
        lineRenderer.startColor = lineColorWithAlpha;
        lineRenderer.endColor = lineColorWithAlpha;
    }
    else
    {
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }

}



Vector3 AdjustLabelPosition(Vector3 originalPosition, List<Vector3> usedPositions, float labelWidth, GameObject labelObj)
{
    Vector3 adjustedPosition = originalPosition;
    bool overlapping;
    int maxAttempts = 20;
    int attempts = 0;

    Vector3[] offsetDirections = new Vector3[]
    {
        Vector3.zero,
        new Vector3(labelWidth * spacingMultiplier, 0, 0),
        new Vector3(-labelWidth * spacingMultiplier, 0, 0),
        new Vector3(0, labelWidth * spacingMultiplier, 0),
        new Vector3(0, -labelWidth * spacingMultiplier, 0),
        new Vector3(labelWidth * spacingMultiplier, labelWidth * spacingMultiplier, 0),
        new Vector3(-labelWidth * spacingMultiplier, labelWidth * spacingMultiplier, 0),
        new Vector3(labelWidth * spacingMultiplier, -labelWidth * spacingMultiplier, 0),
        new Vector3(-labelWidth * spacingMultiplier, -labelWidth * spacingMultiplier, 0),
        new Vector3(labelWidth * spacingMultiplier * 0.5f, labelWidth * spacingMultiplier * 0.5f, 0),
        new Vector3(-labelWidth * spacingMultiplier * 0.5f, labelWidth * spacingMultiplier * 0.5f, 0),
        new Vector3(labelWidth * spacingMultiplier * 0.5f, -labelWidth * spacingMultiplier * 0.5f, 0),
        new Vector3(-labelWidth * spacingMultiplier * 0.5f, -labelWidth * spacingMultiplier * 0.5f, 0),
        new Vector3(labelWidth * spacingMultiplier * 0.707f, labelWidth * spacingMultiplier * 0.707f, 0),
        new Vector3(-labelWidth * spacingMultiplier * 0.707f, labelWidth * spacingMultiplier * 0.707f, 0),
        new Vector3(labelWidth * spacingMultiplier * 0.707f, -labelWidth * spacingMultiplier * 0.707f, 0),
        new Vector3(-labelWidth * spacingMultiplier * 0.707f, -labelWidth * spacingMultiplier * 0.707f, 0)
    };

    do
    {
        overlapping = false;

        foreach (Vector3 usedPosition in usedPositions)
        {
            if (Vector3.Distance(adjustedPosition, usedPosition) < overlapThreshold)
            {
                overlapping = true;
                break;
            }
        }

        if (overlapping)
        {
            int directionIndex = attempts % offsetDirections.Length;
            adjustedPosition = originalPosition + offsetDirections[directionIndex];
        }

        attempts++;
    } while (overlapping && attempts < maxAttempts);

    return adjustedPosition;
}



    Vector3 GetDataPointPosition(int index)
    {
        if (selector != null)
        {
            return selector.GetCurrentDataPointPosition(index);
        }
        return Vector3.zero;
    }
}
