using UnityEngine;
using IATK;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class Selector : MonoBehaviour
{
public float selectionRadius = 0.01f;
public Color selectedColor = Color.red;
public float transparency = 0f;
public Color nearbyColors = Color.yellow;
public float nearbyTransparency = 0.3f;
public AbstractVisualisation scatterplotVisualisation;
public float hoverScaleFactor = 1.5f;
public float maxSelectionDistance = 0.1f; //    (   )

private Camera mainCamera;

private Text dashboardText;
private string dashboardContent = "";
private RectTransform dashboardRectTransform;
private int screenWidth;
private int screenHeight;
private GameObject hoveredPoint;
public float sphereScaleFactor = 0.5f; //    factor
private GameObject tooltipObject;
private Text tooltipText;
private RectTransform tooltipRectTransform;
private float tooltipOffset = 20f; //     '
private Vector3 lastMousePosition;
private Dictionary<int, GameObject> dataPointObjects = new Dictionary<int, GameObject>();
private bool lastColliderInteractionState = true;
private HashSet<int> scaleActivePoints = new HashSet<int>();
private HashSet<int> filterActivePoints = new HashSet<int>();

public IReadOnlyCollection<int> ScaleActivePoints => scaleActivePoints;
public IReadOnlyCollection<int> FilterActivePoints => filterActivePoints;

private HashSet<int> nearbyPointsIndices = new HashSet<int>();
private int? selectedIndex = null;
private List<int> nearbyIndices = new List<int>();
private GameObject selectedPoint = null;

private Dictionary<int, GameObject> nearbyPoints = new Dictionary<int, GameObject>();


public Dictionary<int, Vector3> GetColliderPositions()
{
var positions = new Dictionary<int, Vector3>();
foreach (var kvp in dataPointObjects)
{
    int index = kvp.Key;
    GameObject pointObject = kvp.Value;
    if (pointObject != null)
    {
        positions[index] = pointObject.transform.position;
    }
}
return positions;
}
public Vector3 GetCurrentDataPointPosition(int index)
{
if (dataPointObjects.TryGetValue(index, out GameObject pointObject))
{
    return pointObject.transform.position;
}
return Vector3.zero;
}
public Dictionary<int, GameObject> DataPointObjects
{
    get { return dataPointObjects; }
}

public bool IsDataPointActive(int index)
{
    if (dataPointObjects.TryGetValue(index, out GameObject pointObject))
    {
        return pointObject.activeSelf;
    }
    return false;
}

void Start()
{
    if (scatterplotVisualisation == null)
    {
        scatterplotVisualisation = GetComponent<AbstractVisualisation>();
        Debug.Log(scatterplotVisualisation != null ? "AbstractVisualisation found" : "AbstractVisualisation not found");
    }
    
    GameObject pcCameraObject = GameObject.Find("pcCamera");
    if (pcCameraObject != null)
    {
        mainCamera = pcCameraObject.GetComponent<Camera>();
        if (mainCamera != null)
        {
            Debug.Log("pcCamera  .");
        }
        else
        {
            Debug.LogError("pcCamera  Camera  .");
            enabled = false; // Camera    .
            return;
        }
    }
    else
    {
        Debug.LogError("Hierarchy 'pcCamera'     .");
        enabled = false; // pcCamera    .
        return;
    }

    lastMousePosition = Input.mousePosition;

    if (scatterplotVisualisation == null || mainCamera == null)
    {
        Debug.LogError("Required components are missing. Disabling Selector.");
        enabled = false;
        return;
    }
    
    AddColliders();
    SetupUI();
    UpdateUIPosition();
    SetupTooltip();
}
void SetupTooltip()
{
    Canvas canvas = FindObjectOfType<Canvas>();
    if (canvas == null)
    {
        Debug.LogError("Canvas not found. Creating a new one.");
        GameObject canvasObject = new GameObject("TooltipCanvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<GraphicRaycaster>();
    }

    tooltipObject = new GameObject("Tooltip");
    tooltipObject.transform.SetParent(canvas.transform, false);

    tooltipText = tooltipObject.AddComponent<Text>();
    Font arialFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
    if (arialFont == null)
    {
        Debug.LogWarning("Arial font not found. Using default font.");
        tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
    else
    {
        tooltipText.font = arialFont;
    }
    
    if (tooltipText.font == null)
    {
        Debug.LogError("Failed to set any font. Text may not be visible.");
    }

    tooltipText.fontSize = 18;
    tooltipText.color = Color.black;
    tooltipText.alignment = TextAnchor.UpperLeft;
    tooltipText.fontStyle = FontStyle.Bold;
    tooltipRectTransform = tooltipText.GetComponent<RectTransform>();
    tooltipRectTransform.anchorMin = new Vector2(0, 1);
    tooltipRectTransform.anchorMax = new Vector2(0, 1);
    tooltipRectTransform.pivot = new Vector2(0, 1);
    tooltipRectTransform.sizeDelta = new Vector2(200, 150);

    tooltipObject.SetActive(false);
}
void UpdateTooltip(GameObject hoveredPoint)
{
    if (hoveredPoint == null || tooltipText == null || tooltipRectTransform == null)
    {
        Debug.LogWarning("UpdateTooltip: Some objects are null. Skipping update.");
        return;
    }

    int index = int.Parse(hoveredPoint.name.Substring("DataPoint_".Length));
    string tooltipContent = GetDataPointInfo(index);

    tooltipText.text = tooltipContent;
    UpdateTooltipPosition();

    tooltipObject.SetActive(true);
}

void UpdateTooltipPosition()
{
    if (tooltipRectTransform != null)
    {
        Vector2 mousePosition = Input.mousePosition;
        tooltipRectTransform.position = new Vector2(mousePosition.x + tooltipOffset, mousePosition.y - tooltipOffset);
    }
}

void HideTooltip()
{
    if (tooltipObject != null)
    {
        tooltipObject.SetActive(false);
    }
}

string GetDataPointInfo(int index)
{
    if (scatterplotVisualisation == null || scatterplotVisualisation.visualisationReference == null || scatterplotVisualisation.visualisationReference.dataSource == null)
    {
        return "Data not available";
    }

    DataSource dataSource = scatterplotVisualisation.visualisationReference.dataSource;
    DimensionFilter xDimension = scatterplotVisualisation.visualisationReference.xDimension;
    DimensionFilter yDimension = scatterplotVisualisation.visualisationReference.yDimension;
    DimensionFilter zDimension = scatterplotVisualisation.visualisationReference.zDimension;

    if (xDimension == null || yDimension == null || zDimension == null)
    {
        return "Dimension data not available";
    }

    string xValue = GetFormattedValue(dataSource, xDimension.Attribute, index);
    string yValue = GetFormattedValue(dataSource, yDimension.Attribute, index);
    string zValue = GetFormattedValue(dataSource, zDimension.Attribute, index);

    return $@"ID: {index}
{xDimension.Attribute}: {xValue}
{yDimension.Attribute}: {yValue}
{zDimension.Attribute}: {zValue}";
}

string GetFormattedValue(DataSource dataSource, string attribute, int index)
{
if (dataSource[attribute] == null || index >= dataSource[attribute].Data.Length)
{
    return "N/A";
}

object rawValue = dataSource[attribute].Data[index];

float floatValue;
if (rawValue is float f)
{
    floatValue = f;
}
else if (rawValue is double d)
{
    floatValue = (float)d;
}
else if (rawValue is int i)
{
    floatValue = (float)i;
}
else if (float.TryParse(rawValue.ToString(), out float result))
{
    floatValue = result;
}
else
{
    return rawValue.ToString(); //     
}

object originalValue = dataSource.getOriginalValue(floatValue, attribute);

if (originalValue is float fValue)
{
    return $"{fValue:F2}";
}
else if (originalValue is double dValue)
{
    return $"{dValue:F2}";
}
else if (originalValue is int iValue)
{
    return iValue.ToString();
}
else if (originalValue is string sValue)
{
    return sValue;
}
else
{
    return originalValue.ToString();
}
}
void SetupUI()
{
    GameObject canvasObject = new GameObject("DashboardCanvas");
    Canvas canvas = canvasObject.AddComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvasObject.AddComponent<GraphicRaycaster>();

    GameObject textObject = new GameObject("DashboardText");
    textObject.transform.SetParent(canvasObject.transform, false);

    dashboardText = textObject.AddComponent<Text>();
    dashboardText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
    dashboardText.fontSize = 14;
    dashboardText.color = Color.black;
    dashboardText.alignment = TextAnchor.UpperLeft;
    dashboardText.supportRichText = true;

    dashboardRectTransform = dashboardText.GetComponent<RectTransform>();
    dashboardRectTransform.anchorMin = new Vector2(0, 0);
    dashboardRectTransform.anchorMax = new Vector2(0, 0);
    dashboardRectTransform.pivot = new Vector2(0, 0);

    screenWidth = Screen.width;
    screenHeight = Screen.height;

}

void UpdateUIPosition()
{
    if (dashboardRectTransform != null)
    {
        float xPos = 120;
        float yPos = 10;
        float width = 900;
        float height = 300;

        dashboardRectTransform.anchoredPosition = new Vector2(xPos, yPos);
        dashboardRectTransform.sizeDelta = new Vector2(width, height);

        dashboardText.fontSize = Mathf.Max(12, Mathf.FloorToInt(Screen.height / 60f));
    }
}

void AddColliders()
{
    Vector3[] vertices = scatterplotVisualisation.viewList[0].BigMesh.getBigMeshVertices();
    for (int i = 0; i < vertices.Length; i++)
    {
        GameObject pointObject = new GameObject($"DataPoint_{i}");
        pointObject.transform.SetParent(scatterplotVisualisation.transform);
        pointObject.transform.localPosition = vertices[i];

        SphereCollider pointCollider = pointObject.AddComponent<SphereCollider>();
        pointCollider.radius = selectionRadius;

        dataPointObjects[i] = pointObject;
    }
    Debug.Log($"Added {vertices.Length} SphereColliders for data points");
}

void Update()
{
    bool isSavedPanelActive = DataStore.Instance.IsSavedPanelActive();

    if (isSavedPanelActive)
    {
        if (lastColliderInteractionState != false)
        {
            UpdateColliderInteractionState(false);
            lastColliderInteractionState = false;
        }
    }
    else
    {
        bool currentColliderInteractionState = DataStore.Instance.GetColliderInteractionEnabled();

        if (currentColliderInteractionState != lastColliderInteractionState)
        {
            UpdateColliderInteractionState(currentColliderInteractionState);
            lastColliderInteractionState = currentColliderInteractionState;
        }
    }

    if (Input.GetMouseButtonDown(0))
    {
        ClearSelectedPoints();
        PerformSelection();
    }

    HandleMouseHover();

    if (Screen.width != screenWidth || Screen.height != screenHeight)
    {
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        UpdateUIPosition();
    }

    if (dashboardText != null)
    {
        dashboardText.text = dashboardContent;
    }

    if (hoveredPoint != null)
    {
        UpdateTooltipPosition();
    }

    UpdateColliderPositions();
    UpdateCollider();
    UpdateSelectedAndNearbyTransparency();
}
void UpdateSelectedAndNearbyTransparency()
{
    if (selectedPoint != null && selectedIndex.HasValue)
    {
        bool isSelectedActive = scaleActivePoints.Contains(selectedIndex.Value) && filterActivePoints.Contains(selectedIndex.Value);
        Renderer selectedRenderer = selectedPoint.GetComponent<Renderer>();
        if (selectedRenderer != null)
        {
            Color selectedColor = selectedRenderer.material.color;
            selectedColor.a = isSelectedActive ? transparency : 0f;
            selectedRenderer.material.color = selectedColor;
        }
    }

    foreach (var kvp in nearbyPoints)
    {
        int index = kvp.Key;
        GameObject nearbyPoint = kvp.Value;
        if (nearbyPoint != null)
        {
            bool isNearbyActive = scaleActivePoints.Contains(index) && filterActivePoints.Contains(index);
            Renderer nearbyRenderer = nearbyPoint.GetComponent<Renderer>();
            if (nearbyRenderer != null)
            {
                Color nearbyColor = nearbyRenderer.material.color;
                nearbyColor.a = isNearbyActive ? nearbyTransparency : 0f;
                nearbyRenderer.material.color = nearbyColor;
            }
        }
    }
}

void UpdateColliderPositions()
{
    Vector3[] currentVertices = scatterplotVisualisation.viewList[0].BigMesh.getBigMeshVertices();
    for (int i = 0; i < currentVertices.Length; i++)
    {
        if (dataPointObjects.TryGetValue(i, out GameObject pointObject))
        {
            pointObject.transform.localPosition = currentVertices[i];
        }
    }
}

void UpdateCollider()
{
if (scatterplotVisualisation == null || 
    scatterplotVisualisation.visualisationReference == null || 
    scatterplotVisualisation.visualisationReference.dataSource == null)
{
    Debug.LogError("Scatterplot visualization or data source is null");
    return;
}

var dataSource = scatterplotVisualisation.visualisationReference.dataSource;

if (!dataSource.IsLoaded)
{
    Debug.LogError("Data source is not loaded.");
    return;
}

float baseColliderSize = 0.1f;
float interactionMargin = 0f;

UpdateDataPointPositions(baseColliderSize, interactionMargin);
}
void UpdateDataPointPositions(float baseColliderSize, float interactionMargin)
{
var vis = scatterplotVisualisation.visualisationReference;
var dataSource = vis.dataSource;

string xField = vis.xDimension.Attribute;
string yField = vis.yDimension.Attribute;
string zField = vis.zDimension.Attribute;

float xScaleMin = vis.xDimension.minScale;
float xScaleMax = vis.xDimension.maxScale;
float yScaleMin = vis.yDimension.minScale;
float yScaleMax = vis.yDimension.maxScale;
float zScaleMin = vis.zDimension.minScale;
float zScaleMax = vis.zDimension.maxScale;

float xFilterMin = Mathf.Lerp(xScaleMin, xScaleMax, vis.xDimension.minFilter);
float xFilterMax = Mathf.Lerp(xScaleMin, xScaleMax, vis.xDimension.maxFilter);
float yFilterMin = Mathf.Lerp(yScaleMin, yScaleMax, vis.yDimension.minFilter);
float yFilterMax = Mathf.Lerp(yScaleMin, yScaleMax, vis.yDimension.maxFilter);
float zFilterMin = Mathf.Lerp(zScaleMin, zScaleMax, vis.zDimension.minFilter);
float zFilterMax = Mathf.Lerp(zScaleMin, zScaleMax, vis.zDimension.maxFilter);

scaleActivePoints.Clear();
filterActivePoints.Clear();

for (int i = 0; i < dataSource.DataCount; i++)
{
    if (dataPointObjects.TryGetValue(i, out GameObject pointObject))
    {
        float xValue = dataSource[xField].Data[i];
        float yValue = dataSource[yField].Data[i];
        float zValue = dataSource[zField].Data[i];

        Vector3 originalPosition = new Vector3(xValue, yValue, zValue);

        Vector3 newPosition = new Vector3(
            Mathf.InverseLerp(xScaleMin, xScaleMax, xValue),
            Mathf.InverseLerp(yScaleMin, yScaleMax, yValue),
            Mathf.InverseLerp(zScaleMin, zScaleMax, zValue)
        );

        bool isInScaleRange = newPosition.x >= -interactionMargin && newPosition.y >= -interactionMargin && newPosition.z >= -interactionMargin &&
                                newPosition.x <= 1 + interactionMargin && newPosition.y <= 1 + interactionMargin && newPosition.z <= 1 + interactionMargin;

        bool isInFilterRange = xValue >= xFilterMin && xValue <= xFilterMax &&
                                yValue >= yFilterMin && yValue <= yFilterMax &&
                                zValue >= zFilterMin && zValue <= zFilterMax;

        if (isInScaleRange)
        {
            scaleActivePoints.Add(i);
        }

        if (isInFilterRange)
        {
            filterActivePoints.Add(i);
        }

        pointObject.transform.localPosition = newPosition;

        SetupRendererAndCollider(pointObject, isInScaleRange, isInFilterRange, baseColliderSize, originalPosition);
    }
}

}
void SetupRendererAndCollider(GameObject pointObject, bool isInScaleRange, bool isInFilterRange, float baseColliderSize, Vector3 originalPosition)
{
bool isSavedPanelActive = DataStore.Instance.IsSavedPanelActive();

if (isSavedPanelActive)
{
    pointObject.SetActive(false);
    return;
}

bool isActive = isInFilterRange;

pointObject.SetActive(isActive);
}

void UpdateCollider_Scale(float baseColliderSize, float interactionMargin)
{
var vis = scatterplotVisualisation.visualisationReference;
var dataSource = vis.dataSource;

float xScaleChange = vis.xDimension.maxScale - vis.xDimension.minScale;
float yScaleChange = vis.yDimension.maxScale - vis.yDimension.minScale;
float zScaleChange = vis.zDimension.maxScale - vis.zDimension.minScale;

scaleActivePoints.Clear();

for (int i = 0; i < dataSource.DataCount; i++)
{
    if (dataPointObjects.TryGetValue(i, out GameObject pointObject))
    {
        Vector3 originalPosition = Vector3.zero;
        Vector3 newPosition = Vector3.zero;

        if (vis.xDimension != null)
        {
            float xData = dataSource[vis.xDimension.Attribute].Data[i];
            originalPosition.x = float.IsNaN(xData) || float.IsInfinity(xData) ? 0f : xData;
            newPosition.x = (originalPosition.x - vis.xDimension.minScale) / xScaleChange;
        }
        if (vis.yDimension != null)
        {
            float yData = dataSource[vis.yDimension.Attribute].Data[i];
            originalPosition.y = float.IsNaN(yData) || float.IsInfinity(yData) ? 0f : yData;
            newPosition.y = (originalPosition.y - vis.yDimension.minScale) / yScaleChange;
        }
        if (vis.zDimension != null)
        {
            float zData = dataSource[vis.zDimension.Attribute].Data[i];
            originalPosition.z = float.IsNaN(zData) || float.IsInfinity(zData) ? 0f : zData;
            newPosition.z = (originalPosition.z - vis.zDimension.minScale) / zScaleChange;
        }

        if (float.IsNaN(newPosition.x) || float.IsInfinity(newPosition.x) ||
            float.IsNaN(newPosition.y) || float.IsInfinity(newPosition.y) ||
            float.IsNaN(newPosition.z) || float.IsInfinity(newPosition.z))
        {
            Debug.LogWarning($"Invalid position calculated for DataPoint_{i}: {newPosition}");
            pointObject.SetActive(false);
            continue;
        }

        pointObject.transform.localPosition = newPosition;

        if (newPosition.x >= -interactionMargin && newPosition.y >= -interactionMargin && newPosition.z >= -interactionMargin &&
            newPosition.x <= 1 + interactionMargin && newPosition.y <= 1 + interactionMargin && newPosition.z <= 1 + interactionMargin)
        {
            scaleActivePoints.Add(i);
        }

    }
}
}
void UpdateCollider_Filter(float baseColliderSize, float interactionMargin)
{
var vis = scatterplotVisualisation.visualisationReference;
var dataSource = vis.dataSource;

string xField = vis.xDimension.Attribute;
string yField = vis.yDimension.Attribute;
string zField = vis.zDimension.Attribute;

Vector2 xFilter = new Vector2(
    (float)dataSource.getOriginalValue(vis.xDimension.minFilter, xField),
    (float)dataSource.getOriginalValue(vis.xDimension.maxFilter, xField)
);
Vector2 yFilter = new Vector2(
    (float)dataSource.getOriginalValue(vis.yDimension.minFilter, yField),
    (float)dataSource.getOriginalValue(vis.yDimension.maxFilter, yField)
);
Vector2 zFilter = new Vector2(
    (float)dataSource.getOriginalValue(vis.zDimension.minFilter, zField),
    (float)dataSource.getOriginalValue(vis.zDimension.maxFilter, zField)
);


filterActivePoints.Clear();  //     

int totalPoints = 0;
int filteredPoints = 0;

for (int i = 0; i < dataSource.DataCount; i++)
{
    if (dataPointObjects.TryGetValue(i, out GameObject pointObject))
    {
        totalPoints++;

        float xValue = (float)dataSource.getOriginalValue(dataSource[xField].Data[i], xField);
        float yValue = (float)dataSource.getOriginalValue(dataSource[yField].Data[i], yField);
        float zValue = (float)dataSource.getOriginalValue(dataSource[zField].Data[i], zField);

        bool isInFilterRange = xValue >= xFilter.x && xValue <= xFilter.y &&
                                yValue >= yFilter.x && yValue <= yFilter.y &&
                                zValue >= zFilter.x && zValue <= zFilter.y;

        if (isInFilterRange)
        {
            filterActivePoints.Add(i);
            filteredPoints++;
        }

        if (i % 1000 == 0)  // 1000  
        {
        }
    }
}

}

private Mesh CreateSphereMesh()
{
GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
Mesh sphereMesh = tempSphere.GetComponent<MeshFilter>().mesh;
Destroy(tempSphere);
return sphereMesh;
}

private float SafeDivide(float a, float b)
{
if (Mathf.Approximately(b, 0f))
{
    return 0f;
}
return a / b;
}

private bool IsWithinAxisBounds(Vector3 position, DimensionFilter xDim, DimensionFilter yDim, DimensionFilter zDim)
{
return position.x >= xDim.minScale && position.x <= xDim.maxScale &&
        position.y >= yDim.minScale && position.y <= yDim.maxScale &&
        position.z >= zDim.minScale && position.z <= zDim.maxScale;
}

void UpdateColliderInteractionState(bool enabled)
{
ClearSelectedPoints();
ClearNearbyPoints();
int changedCount = 0;
foreach (var kvp in dataPointObjects)
{
    GameObject pointObject = kvp.Value;
    if (pointObject != null)
    {
        pointObject.SetActive(enabled);
        changedCount++;
    }
}
}

void CreateSelectedPoint(int index, Vector3 position)
{
    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    sphere.transform.SetParent(scatterplotVisualisation.transform);
    sphere.transform.position = position;
    sphere.transform.localScale = Vector3.one * selectionRadius * sphereScaleFactor;

    Renderer renderer = sphere.GetComponent<Renderer>();
    Material material = new Material(Shader.Find("Standard"));
    material.color = new Color(selectedColor.r, selectedColor.g, selectedColor.b, transparency);
    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
    material.SetInt("_ZWrite", 0);
    material.DisableKeyword("_ALPHATEST_ON");
    material.EnableKeyword("_ALPHABLEND_ON");
    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    material.renderQueue = 3000;
    renderer.material = material;


    if (selectedPoint != null)
    {
        Destroy(selectedPoint);
    }

    selectedPoint = sphere;
}

void ClearSelectedPoints()
{
    if (selectedPoint != null)
    {
        Destroy(selectedPoint);
        selectedPoint = null;
    }
    ClearNearbyPoints();
    Debug.Log("Cleared all selected and nearby points");
}

void FilterNearbyData(int selectedIndex)
{
    if (scatterplotVisualisation == null || scatterplotVisualisation.visualisationReference == null)
    {
        Debug.LogError("scatterplotVisualisation or visualisationReference is null");
        return;
    }

    DataSource dataSource = scatterplotVisualisation.visualisationReference.dataSource;
    if (dataSource == null)
    {
        Debug.LogError("dataSource is null");
        return;
    }

    DimensionFilter xDimension = scatterplotVisualisation.visualisationReference.xDimension;
    DimensionFilter yDimension = scatterplotVisualisation.visualisationReference.yDimension;
    DimensionFilter zDimension = scatterplotVisualisation.visualisationReference.zDimension;

    if (xDimension == null || yDimension == null || zDimension == null)
    {
        Debug.LogError("One or more dimensions are null");
        return;
    }

    Vector3 selectedPosition = new Vector3(
        dataSource[xDimension.Attribute].Data[selectedIndex],
        dataSource[yDimension.Attribute].Data[selectedIndex],
        dataSource[zDimension.Attribute].Data[selectedIndex]
    );

    float nearbyRadius = 0.1f; // This is in normalized space, so 0.1 means 10% of the full range

    List<int> filteredIndices = new List<int>();
        filteredIndices.Add(selectedIndex); // Add selectedIndex as the first element

        int dataLength = dataSource[xDimension.Attribute].Data.Length;
        for (int i = 0; i < dataLength; i++)
        {
            if (i == selectedIndex) continue; // Skip the selected point itself

            Vector3 currentPosition = new Vector3(
                dataSource[xDimension.Attribute].Data[i],
                dataSource[yDimension.Attribute].Data[i],
                dataSource[zDimension.Attribute].Data[i]
            );

            float distance = Vector3.Distance(selectedPosition, currentPosition);

            if (distance <= nearbyRadius)
            {
                filteredIndices.Add(i);
            }
        }

        UpdateDashboard(selectedIndex, filteredIndices, new Dictionary<string, Vector2>
        {
            { xDimension.Attribute, new Vector2(selectedPosition.x - nearbyRadius, selectedPosition.x + nearbyRadius) },
            { yDimension.Attribute, new Vector2(selectedPosition.y - nearbyRadius, selectedPosition.y + nearbyRadius) },
            { zDimension.Attribute, new Vector2(selectedPosition.z - nearbyRadius, selectedPosition.z + nearbyRadius) }
        });

        nearbyIndices = filteredIndices;


        DataStore.Instance.SetSelectedData("NearbyPointIndices", filteredIndices);

        DataStore.Instance.DebugLogAllData();

}

void UpdateDashboard(int selectedIndex, List<int> filteredIndices, Dictionary<string, Vector2> dimensionRanges)
{


}

void CreateNearbyPoints(List<int> indices)
{
    ClearNearbyPoints();

    int maxVisualizedPoints = Mathf.Min(10, indices.Count);
    List<int> topIndices = indices.Take(maxVisualizedPoints).ToList();

    foreach (int index in topIndices)
    {
        if (dataPointObjects.TryGetValue(index, out GameObject pointObject))
        {
            Renderer renderer = pointObject.GetComponent<Renderer>();
            if (renderer == null)
            {
                renderer = pointObject.AddComponent<MeshRenderer>();
                pointObject.AddComponent<MeshFilter>().mesh = CreateSphereMesh();
            }

            SphereCollider collider = pointObject.GetComponent<SphereCollider>();
            if (collider == null)
            {
                collider = pointObject.AddComponent<SphereCollider>();
            }

            renderer.enabled = true;
            if (collider != null) collider.enabled = true;

            Material material = renderer.material;
            material.color = new Color(nearbyColors.r, nearbyColors.g, nearbyColors.b, nearbyTransparency); //  
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;

            float visualScale = selectionRadius * sphereScaleFactor;
            pointObject.transform.localScale = Vector3.one * visualScale;

            if (collider != null) collider.radius = selectionRadius;

            nearbyPoints[index] = pointObject;
        }
    }
}
void ClearNearbyPoints()
{
    foreach (var kvp in nearbyPoints)
    {
        if (dataPointObjects.TryGetValue(kvp.Key, out GameObject pointObject))
        {
            Renderer renderer = pointObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

            SphereCollider collider = pointObject.GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }
    nearbyPoints.Clear();
}


GameObject FindClosestPoint()
{
    GameObject closestPoint = null;
    float closestDistance = float.MaxValue;

    Vector3 mousePosition = Input.mousePosition;

    foreach (var kvp in dataPointObjects)
    {
        int index = kvp.Key;
        GameObject pointObject = kvp.Value;

        if (!scaleActivePoints.Contains(index) || !filterActivePoints.Contains(index))
            continue;

        if (!pointObject.activeSelf)
            continue;

        Vector3 screenPos = mainCamera.WorldToScreenPoint(pointObject.transform.position);

        if (screenPos.z <= 0)
            continue;

        float distance = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), new Vector2(mousePosition.x, mousePosition.y));

        float adjustedSelectionRadius = selectionRadius * Screen.width;

        if (distance < closestDistance && distance <= adjustedSelectionRadius)
        {
            closestDistance = distance;
            closestPoint = pointObject;
        }
    }

    return closestPoint;
}

void HandleMouseHover()
{
    if (Input.mousePosition != lastMousePosition)
    {
        GameObject closestPoint = FindClosestPoint();
        
        if (closestPoint != null)
        {
            if (hoveredPoint != closestPoint)
            {
                hoveredPoint = closestPoint;
                UpdateTooltip(hoveredPoint);
            }
            else
            {
                UpdateTooltipPosition();
            }
        }
        else
        {
            hoveredPoint = null;
            HideTooltip();
        }

        lastMousePosition = Input.mousePosition;
    }
}

void PerformSelection()
{
    GameObject closestPoint = FindClosestPoint();
    
    if (closestPoint != null)
    {
        int index = int.Parse(closestPoint.name.Substring("DataPoint_".Length));
        Vector3 dataPointPosition = closestPoint.transform.position;

        selectedIndex = index;

        DataStore.Instance.AddSelectedPointIndex(index);        
        

        DataStore.Instance.DebugLogAllData();
    }
    else
    {
        Debug.Log("No point selected");
    }
}
}
