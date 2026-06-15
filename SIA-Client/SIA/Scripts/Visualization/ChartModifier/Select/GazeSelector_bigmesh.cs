



















        


    
        
    


















    






// using UnityEngine;
// using IATK;
// using System.Collections.Generic;
// using System.Globalization;
// using System;
// using System.Collections;

// public class GazeSelector : MonoBehaviour
// {
//     [Header("Eye Interactors")]
//     public Transform leftEyeInteractor;
//     public Transform rightEyeInteractor;

//     [Header("Head Transform")]
//     public Transform headTransform;

//     [Header("Gaze Raycast Settings")]
//     public float maxDistance = 10f;
//     public LayerMask dotLayer;

//     [Header("ScatterPlot Reference")]
//     public AbstractVisualisation scatterplotVisualisation;

//     [Header("ScatterPlot Data (IATK)")]
//     public CSVDataSource dataSource;
//     private float[] orderData;
//     private float[] pidData;

//     [Header("Visual Feedback")]
//     public Color hoverColor = Color.black;
//     public float hoverScale = 1.2f;
//     private Vector3 scatterMinLocal;
//     private Vector3 scatterMaxLocal;
//     public Color dotColor = new Color(0.6509804f, 0.4627451f, 0.1137255f); 
//     // public Color dotColor = color.red; 
//     private GameObject lastHovered;
//     private Vector3 originalScale;
//     private static MaterialPropertyBlock _mpb; // Prevent excessive material instance creation
//     private Dictionary<GameObject, Vector3> dotOriginalScales = new Dictionary<GameObject, Vector3>();

//     [Header("UI Display")]
//     public GazeHeadDisplay infoDisplay;  // Script to display gaze info on UI

//     // Fields to store updated gaze information
//     private int currId = -1;
//     private int currPid = -1;
//     private Vector3 currPos;
//     private string lastGazeJson = "";

//     [Header("Nearby Points")]
//     public int maxNearbyPoints = 10;
//     public float nearbyRadius = 0.5f;
//     private readonly List<int> nearbyIndices = new List<int>();
//     private readonly List<NearbyPointInfo> nearbyPoints = new List<NearbyPointInfo>();
//     private int prevId = -1;
//     public struct GazeHoverData
//     {
//         public int id;
//         public int pid;
//         public Vector3 targetCenterWorld; // World center of the data point
//         public Transform head;            // Head transform (for 45-degree direction calculations)

//         // For tooltip: axis names/values
//         public string xName, yName, zName;
//         public string xValue, yValue, zValue;
//     }

//     [Header("Tooltip Axes (column names)")]
//     public string xFieldName;
//     public string yFieldName;
//     public string zFieldName;

//     // Added event: subscribers render line/tooltip
//     public static event Action<GazeHoverData> OnGazeHover;
//     string[] axesFields;
//     private Dictionary<int, Renderer> rmap = new Dictionary<int, Renderer>();

//     void Start()
//     {
//         // 0) Auto-assign scatterplotVisualisation
//         if (scatterplotVisualisation == null)
//         {
//             scatterplotVisualisation = GetComponent<AbstractVisualisation>();
//             if (scatterplotVisualisation == null)
//             {
//                 scatterplotVisualisation = FindObjectOfType<AbstractVisualisation>();
//             }
//         }
//         if (scatterplotVisualisation == null || scatterplotVisualisation.viewList == null || scatterplotVisualisation.viewList.Count == 0)
//         {
//             Debug.LogError("[GazeSelector] Could not find ScatterPlot component!");
//             enabled = false;
//             return;
//         }

//         // 1) Check dataSource
//         if (dataSource == null)
//         {
//             Debug.LogError("[GazeSelector] dataSource is not set!");
//             enabled = false;
//             return;
//         }
//         try
//         {
//             orderData = dataSource["Order"].Data;
//             pidData = dataSource["PID"].Data;
//         }
//         catch
//         {
//             Debug.LogError("[GazeSelector] 'Order' or 'PID' column is missing in dataSource!");
//             enabled = false;
//             return;
//         }

//         // 2) Auto-assign eye interactors
//         TryAutoAssignEyes();

//         // 3) Auto-assign headTransform (use Camera.main)
//         if (headTransform == null && Camera.main != null)
//         {
//             headTransform = Camera.main.transform;
//             Debug.Log("[GazeSelector] headTransform auto-assigned to Camera.main.");
//         }

//         ////Calculate ScatterPlot bounds
//         var verts = scatterplotVisualisation.viewList[0].BigMesh.getBigMeshVertices();
//         if (verts != null && verts.Length > 0)
//         {
//             scatterMinLocal = scatterMaxLocal = verts[0];
//             for (int i = 1; i < verts.Length; i++)
//             {
//                 scatterMinLocal = Vector3.Min(scatterMinLocal, verts[i]);
//                 scatterMaxLocal = Vector3.Max(scatterMaxLocal, verts[i]);
//             }
//         }
//         else
//         {
//             Debug.LogError("[GazeSelector] Failed to load ScatterPlot vertices");
//         }

//         rmap.Clear();
//         foreach (var r in scatterplotVisualisation.GetComponentsInChildren<Renderer>(true))
//         {
//             var n = r.transform.name;
//             if (!n.StartsWith("DataPoint_")) continue;
//             var p = n.Split('_');
//             if (p.Length >= 2 && int.TryParse(p[1], out int i))
//                 rmap[i] = r; // i = index
//         }
//     }

//     void Update()
//     {

        
//         if (leftEyeInteractor == null || rightEyeInteractor == null || headTransform == null)
//         {
//             TryAutoAssignEyes();
//             if (headTransform == null && Camera.main != null) headTransform = Camera.main.transform;
//             if (leftEyeInteractor == null || rightEyeInteractor == null || headTransform == null) return;
//         }

//         Vector3 origin = (leftEyeInteractor.position + rightEyeInteractor.position) * 0.5f;
//         Vector3 fwd = ((leftEyeInteractor.forward + rightEyeInteractor.forward) * 0.5f).normalized;

//         GameObject hitDot = null;
//         currId = -1;
//         currPid = -1;
//         currPos = Vector3.zero;
    
//         bool gazeHit = Physics.Raycast(origin, fwd, out RaycastHit hit, maxDistance, dotLayer);
        
//         if (gazeHit)
// {
//     Debug.Log($"[GazeSelector] Hit {hit.collider.name} at {hit.point}");
// }
// else
// {
//     Debug.Log("[GazeSelector] Raycast miss");
// }
    
//         if (gazeHit)
//         {
//             hitDot = hit.collider?.gameObject;

//             // Store original scale (only once)
//             if (hitDot != null && !dotOriginalScales.ContainsKey(hitDot))
//                 dotOriginalScales[hitDot] = hitDot.transform.localScale;

//             // Restore previous hover/nearby state
//             ClearLastHovered();
//             ClearNearbyDotsColor();

//             lastHovered = hitDot;

//             if (hitDot != null && hitDot.TryGetComponent<Renderer>(out var rend))
//             {
//                 SetHover(rend, true);
//                 SetTargetBlack(rend);
//             }
//             // Always apply hoverScale from original size
//             if (hitDot != null && dotOriginalScales.ContainsKey(hitDot))
//                 hitDot.transform.localScale = dotOriginalScales[hitDot] * hoverScale;

//             // Build and store gaze data
//             int idx = ParseDotIndex(hitDot?.name);
//             if (idx >= 0 && idx < orderData.Length)
//             {
//                 currId = ConvertToInt(dataSource.getOriginalValue(orderData[idx], "Order"));
//                 currPid = ConvertToInt(dataSource.getOriginalValue(pidData[idx], "PID"));
//                 currPos = hit.point;
//                 FindNearbyPoints(idx);
//                 SetNearbyDotsGreen();

//                 var payload = BuildGazeHoverData(idx, hitDot);
//                 Debug.Log($"[GazeSelector] hover id={payload.id} | X=({payload.xName}:{payload.xValue}) Y=({payload.yName}:{payload.yValue}) Z=({payload.zName}:{payload.zValue})");
//                 OnGazeHover?.Invoke(payload);

//                 // // Emit event
//                 // OnGazeHover?.Invoke(BuildGazeHoverData(idx, hitDot));

//                 // // Emit event (tooltip/line are rendered by subscribers)
//                 // OnGazeHover?.Invoke(new GazeHoverData {
//                 //     id = currId,
//                 //     pid = currPid,
//                 //     targetCenterWorld = hitDot.transform.position,
//                 //     head = headTransform != null ? headTransform : (Camera.main != null ? Camera.main.transform : null)
//                 // });
//             }

//             Vector3 hp = headTransform.position;
//             GetNearestScatterPlane(hp, out string planeName, out Vector3 planeWorldPos);

//             string posX = F3(currPos.x), posY = F3(currPos.y), posZ = F3(currPos.z);
//             string hpX = F3(hp.x), hpY = F3(hp.y), hpZ = F3(hp.z);
//             string planeX = F3(planeWorldPos.x), planeY = F3(planeWorldPos.y), planeZ = F3(planeWorldPos.z);
//             string nearbyPointsJson = CreateNearbyPointsJson();

//             // string json =
//             //     "{"
//             //     + $"\"GAZE\":{{\"id\":{currId},\"pid\":{currPid},\"pos\":[{posX},{posY},{posZ}]}}," 
//             //     + $"\"head\":{{\"pos\":[{hpX},{hpY},{hpZ}],\"plane\":\"{planeName}\",\"planePos\":[{planeX},{planeY},{planeZ}]}}," 
//             //     + $"\"nearby\":{nearbyPointsJson}"
//             //     + "}";

//             // After planeName is resolved, right before building JSON:
//             ResolveAxisFieldNames(out var xName, out var yName, out var zName);

//             switch (planeName)
//             {
//                 case "YZ_XMin":
//                 case "YZ_XMax":
//                     axesFields = new string[] { yName, zName };
//                     break;
//                 case "XY_ZMin":
//                 case "XY_ZMax":
//                     axesFields = new string[] { xName, yName };
//                     break;
//                 default:
//                     axesFields = new string[] { xName, yName };
//                     break;
//             }
//             string json =
//                 "{"
//                 + $"\"GAZE\":{{\"id\":{currId},\"pid\":{currPid},\"pos\":[{posX},{posY},{posZ}]}},"
//                 + $"\"head\":{{"
//                     + $"\"pos\":[{hpX},{hpY},{hpZ}],"
//                     + "\"viewPlane\":{"
//                         + $"\"axesFields\":[\"{axesFields[0]}\",\"{axesFields[1]}\"],"
//                         + $"\"planeName\":\"{planeName}\","
//                         + $"\"distance\":{F3(Vector3.Distance(headTransform.position, planeWorldPos))},"
//                         + $"\"planePos\":[{planeX},{planeY},{planeZ}]"
//                     + "}"
//                 + "},"
//                 + $"\"nearby\":{nearbyPointsJson}"
//                 + "}";

//             // Pretty-print outgoing JSON for logs (if Newtonsoft is available)
//             try
//             {
//                 var pretty = Newtonsoft.Json.Linq.JToken.Parse(json)
//                             .ToString(Newtonsoft.Json.Formatting.Indented);
//                 Debug.Log($"[GazeSelector] JSON=\n{pretty}");
//             }
//             catch
//             {
//                 Debug.Log($"[GazeSelector] JSON(raw)={json}");
//             }

//             // [GazeSelector] JSON=
//             // {
//             //   "GAZE": {
//             //     "id": 957,
//             //     "pid": 916176128,
//             //     "pos": [
//             //       0.547,
//             //       1.278,
//             //       1.311
//             //     ]
//             //   },
//             //   "head": {
//             //     "pos": [
//             //       -0.008,
//             //       1.087,
//             //       0.017
//             //     ],
//             //     "viewPlane": {
//             //       "axesFields": [
//             //         "Year_Built",
//             //         "SalePrice"
//             //       ],
//             //       "planeName": "YZ_XMin",
//             //       "distance": 0.436,
//             //       "planePos": [
//             //         -0.444,
//             //         1.087,
//             //         0.017
//             //       ]
//             //     }
//             //   },
//             //   "nearby": []
//             // }

//             lastGazeJson = json;
//             // infoDisplay?.UpdateTarget(lastGazeJson);
//         }
//         else
//         {
//             // On raycast miss, keep current color/scale and show the last gaze value
//             // infoDisplay?.UpdateTarget(lastGazeJson);
//             // ClearLastHovered();
//             // ClearNearbyDotsColor();
//         }
//     }
// private void ResolveAxisFieldNames(out string xName, out string yName, out string zName)
// {
//     xName = xFieldName;
//     yName = yFieldName;
//     zName = zFieldName;

//     var visRef = scatterplotVisualisation != null ? scatterplotVisualisation.visualisationReference : null;

//     if (string.IsNullOrEmpty(xName) && visRef != null && visRef.xDimension != null)
//         xName = visRef.xDimension.Attribute;
//     if (string.IsNullOrEmpty(yName) && visRef != null && visRef.yDimension != null)
//         yName = visRef.yDimension.Attribute;
//     if (string.IsNullOrEmpty(zName) && visRef != null && visRef.zDimension != null)
//         zName = visRef.zDimension.Attribute;
// }
    
// private GazeHoverData BuildGazeHoverData(int idx, GameObject hitDot)
//     {
//         // 0) Use inspector values first; if empty, fallback to visualisationReference
//         string xName = xFieldName;
//         string yName = yFieldName;
//         string zName = zFieldName;

//         var visRef = scatterplotVisualisation != null ? scatterplotVisualisation.visualisationReference : null;

//         if (string.IsNullOrEmpty(xName) && visRef != null && visRef.xDimension != null)
//             xName = visRef.xDimension.Attribute;
//         if (string.IsNullOrEmpty(yName) && visRef != null && visRef.yDimension != null)
//             yName = visRef.yDimension.Attribute;
//         if (string.IsNullOrEmpty(zName) && visRef != null && visRef.zDimension != null)
//             zName = visRef.zDimension.Attribute;

//         // 1) Safely read values (using CSVDataSource indexer)
//         string xVal = "", yVal = "", zVal = "";

//         if (!string.IsNullOrEmpty(xName) && TryGetColumn(xName, out var xCol) && idx >= 0 && idx < xCol.Length)
//         {
//             float n = xCol[idx];
//             object orig = dataSource.getOriginalValue(n, xName);
//             xVal = FormatValue(orig);
//         }
//         if (!string.IsNullOrEmpty(yName) && TryGetColumn(yName, out var yCol) && idx >= 0 && idx < yCol.Length)
//         {
//             float n = yCol[idx];
//             object orig = dataSource.getOriginalValue(n, yName);
//             yVal = FormatValue(orig);
//         }
//         if (!string.IsNullOrEmpty(zName) && TryGetColumn(zName, out var zCol) && idx >= 0 && idx < zCol.Length)
//         {
//             float n = zCol[idx];
//             object orig = dataSource.getOriginalValue(n, zName);
//             zVal = FormatValue(orig);
//         }

//         // 2) Event payload
//         return new GazeHoverData
//         {
//             id = currId,
//             pid = currPid,
//             targetCenterWorld = hitDot.transform.position,
//             head = headTransform ?? Camera.main?.transform,
//             xName = xName,
//             yName = yName,
//             zName = zName,
//             xValue = xVal,
//             yValue = yVal,
//             zValue = zVal
//         };
//     }


//    private void SetTargetBlack(Renderer r)
//     {
//         if (_mpb == null) _mpb = new MaterialPropertyBlock();
//         r.GetPropertyBlock(_mpb);
//         if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor"))
//             _mpb.SetColor("_BaseColor", Color.black);
//         else
//             _mpb.SetColor("_Color", Color.black);
//         r.SetPropertyBlock(_mpb);
//     }

//     private void SetNearbyDotsGreen()
//     {
//         if (rmap == null || rmap.Count == 0) return;
//         foreach (int idx in nearbyIndices)
//             if (rmap.TryGetValue(idx, out var rend))
//                 SetDotColor(rend, new Color(0f, 1f, 0f, 1f)); // Green, alpha 1
//     }

//     private void ClearNearbyDotsColor()
//     {
//         if (rmap == null || rmap.Count == 0) return;
//         Color baseColor = new Color(dotColor.r, dotColor.g, dotColor.b, 0f);
//         foreach (int idx in nearbyIndices)
//             if (rmap.TryGetValue(idx, out var rend))
//                 SetDotColor(rend, baseColor);
//         nearbyIndices.Clear();
//     }


//     private void SetDotColor(Renderer r, Color color)
//     {
//         if (_mpb == null) _mpb = new MaterialPropertyBlock();
//         r.GetPropertyBlock(_mpb);
//         if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor"))
//             _mpb.SetColor("_BaseColor", color);
//         else
//             _mpb.SetColor("_Color", color);
//         r.SetPropertyBlock(_mpb);
//     }
//     void OnDisable()
//     {
//         ClearLastHovered();
//     }

//     // =========================
//     // Helper methods
//     // =========================
//     // CSVDataSource has no HasColumn, so use safe indexer try/catch access
//     private bool TryGetColumn(string colName, out float[] col)
//     {
//         col = null;
//         if (dataSource == null || string.IsNullOrEmpty(colName)) return false;
//         try
//         {
//             col = dataSource[colName].Data; // Throws if the column does not exist
//             return col != null;
//         }
//         catch
//         {
//             return false;
//         }
//     }

//     private string FormatValue(object orig)
//     {
//         if (orig == null) return "";
//         switch (orig)
//         {
//             case float f:  return f.ToString("G6", CultureInfo.InvariantCulture);
//             case double d: return d.ToString("G6", CultureInfo.InvariantCulture);
//             case int i:    return i.ToString(CultureInfo.InvariantCulture);
//             default:       return orig.ToString();
//         }
//     }

//     private void TryAutoAssignEyes()
//     {
//         if (leftEyeInteractor != null && rightEyeInteractor != null) return;

//         var cam = Camera.main;
//         if (cam == null) return;

//         var leftEye = cam.transform.Find("LeftEyeAnchor");
//         var rightEye = cam.transform.Find("RightEyeAnchor");
//         if (leftEye != null) leftEyeInteractor = leftEye;
//         if (rightEye != null) rightEyeInteractor = rightEye;
//         if (leftEye != null || rightEye != null)
//             Debug.Log("[GazeSelector] Eye Interactors auto-assigned");
//     }

//     private static string F3(float v)
//     {
//         if (float.IsNaN(v) || float.IsInfinity(v)) v = 0f;
//         return v.ToString("F3", CultureInfo.InvariantCulture);
//     }

//     private void SetHover(Renderer r, bool on)
//     {
//         if (_mpb == null) _mpb = new MaterialPropertyBlock();
//         r.GetPropertyBlock(_mpb);
//         if (on)
//         {
//             if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor"))
//                 _mpb.SetColor("_BaseColor", hoverColor);
//             else
//                 _mpb.SetColor("_Color", hoverColor);
//         }
//         else
//         {
//             _mpb.Clear(); // Reset
//         }
//         r.SetPropertyBlock(_mpb);
//     }

//     // Find nearby points
//     private void FindNearbyPoints(int selectedIndex)
//     {
//         nearbyPoints.Clear();
//         nearbyIndices.Clear();

//         if (scatterplotVisualisation == null ||
//             scatterplotVisualisation.viewList == null ||
//             scatterplotVisualisation.viewList.Count == 0 ||
//             scatterplotVisualisation.viewList[0].BigMesh == null)
//         {
//             Debug.LogError("[GazeSelector] Visualization data not found.");
//             return;
//         }

//         Vector3[] vertices = scatterplotVisualisation.viewList[0].BigMesh.getBigMeshVertices();
//         if (selectedIndex < 0 || selectedIndex >= vertices.Length)
//         {
//             Debug.LogError($"[GazeSelector] Selected index ({selectedIndex}) is out of range.");
//             return;
//         }

//         Vector3 selectedPosition = vertices[selectedIndex];
//         List<KeyValuePair<int, float>> distancePairs = new List<KeyValuePair<int, float>>(vertices.Length);

//         // Calculate distance to all data points (local coordinates)
//         for (int i = 0; i < vertices.Length; i++)
//         {
//             if (i == selectedIndex) continue;

//             float distance = Vector3.Distance(selectedPosition, vertices[i]);

//             // Only consider points within a certain radius
//             if (distance <= nearbyRadius)
//             {
//                 distancePairs.Add(new KeyValuePair<int, float>(i, distance));
//             }
//         }

//         // Sort by distance
//         distancePairs.Sort((a, b) => a.Value.CompareTo(b.Value));

//         // Take up to maxNearbyPoints nearest
//         int count = Mathf.Min(maxNearbyPoints, distancePairs.Count);
//         for (int i = 0; i < count; i++)
//         {
//             int idx = distancePairs[i].Key;
//             nearbyIndices.Add(idx);

//             if (idx < orderData.Length)
//             {
//                 int id = ConvertToInt(dataSource.getOriginalValue(orderData[idx], "Order"));
//                 int pid = ConvertToInt(dataSource.getOriginalValue(pidData[idx], "PID"));

//                 nearbyPoints.Add(new NearbyPointInfo(
//                     id,
//                     pid,
//                     vertices[idx],
//                     distancePairs[i].Value
//                 ));
//             }
//         }

//         Debug.Log($"[GazeSelector] Found {count} nearby points (based on ID: {currId})");
//     }

//     // Create JSON for nearby points array
//     private string CreateNearbyPointsJson()
//     {
//         if (nearbyPoints == null || nearbyPoints.Count == 0)
//             return "[]";

//         List<string> pointInfos = new List<string>(nearbyPoints.Count);

//         foreach (var point in nearbyPoints)
//         {
//             if (point == null) continue;

//             float x = (float.IsNaN(point.pos.x) || float.IsInfinity(point.pos.x)) ? 0f : point.pos.x;
//             float y = (float.IsNaN(point.pos.y) || float.IsInfinity(point.pos.y)) ? 0f : point.pos.y;
//             float z = (float.IsNaN(point.pos.z) || float.IsInfinity(point.pos.z)) ? 0f : point.pos.z;
//             float d = (float.IsNaN(point.distance) || float.IsInfinity(point.distance)) ? 0f : point.distance;

//             string entry =
//                 "{"
//                 + $"\"id\":{point.id},\"pid\":{point.pid},"
//                 + $"\"pos\":[{F3(x)},{F3(y)},{F3(z)}],"
//                 + $"\"distance\":{d.ToString("F5", System.Globalization.CultureInfo.InvariantCulture)}"
//                 + "}";

//             pointInfos.Add(entry);
//         }

//         return "[" + string.Join(",", pointInfos) + "]";
//     }

//     // ConvertToInt utility
//     int ConvertToInt(object o)
//     {
//         if (o is float f) return Mathf.RoundToInt(f);
//         if (o is double d) return Mathf.RoundToInt((float)d);
//         if (o is int i) return i;
//         int parsed;
//         return int.TryParse(o.ToString(), out parsed) ? parsed : -1;
//     }

//     public string GetCurrentGazeJson()
//     {
//         return lastGazeJson;
//     }

//     // Find nearest scatter plot side (label adjustment: X-plane=YZ_*, Z-plane=XY_*)
//     void GetNearestScatterPlane(Vector3 headWorld, out string planeName, out Vector3 planeWorldPos)
//     {
//         // (A) World → Local
//         Vector3 headLocal = scatterplotVisualisation.transform.InverseTransformPoint(headWorld);

//         // (B) Distance to 4 side planes
//         float dLeft = Mathf.Abs(headLocal.x - scatterMinLocal.x); // x = min
//         float dRight = Mathf.Abs(headLocal.x - scatterMaxLocal.x); // x = max
//         float dBack = Mathf.Abs(headLocal.z - scatterMinLocal.z); // z = min
//         float dFront = Mathf.Abs(headLocal.z - scatterMaxLocal.z); // z = max

//         // (C) Select side with min distance
//         int side = 0;
//         float minD = dLeft;
//         if (dRight < minD) { side = 1; minD = dRight; }
//         if (dBack < minD) { side = 2; minD = dBack; }
//         if (dFront < minD) { side = 3; }

//         Vector3 planeLocal;
//         switch (side)
//         {
//             case 0: // left  (x = min)
//                 planeName = "YZ_XMin";
//                 planeLocal = new Vector3(scatterMinLocal.x, headLocal.y, headLocal.z);
//                 break;
//             case 1: // right (x = max)
//                 planeName = "YZ_XMax";
//                 planeLocal = new Vector3(scatterMaxLocal.x, headLocal.y, headLocal.z);
//                 break;
//             case 2: // back  (z = min)
//                 planeName = "XY_ZMin";
//                 planeLocal = new Vector3(headLocal.x, headLocal.y, scatterMinLocal.z);
//                 break;
//             default: // front (z = max)
//                 planeName = "XY_ZMax";
//                 planeLocal = new Vector3(headLocal.x, headLocal.y, scatterMaxLocal.z);
//                 break;
//         }

//         // (D) Local → World
//         planeWorldPos = scatterplotVisualisation.transform.TransformPoint(planeLocal);
//     }

//     void ClearLastHovered()
//     {
//         if (!lastHovered) return;

//         // Restore original scale
//         if (dotOriginalScales.ContainsKey(lastHovered))
//             lastHovered.transform.localScale = dotOriginalScales[lastHovered];

//         if (lastHovered.TryGetComponent<Renderer>(out var rend))
//             SetHover(rend, false);

//         lastHovered = null;
//     }


//     int ParseDotIndex(string name)
//     {
//         if (string.IsNullOrEmpty(name)) return -1;
//         var parts = name.Split('_');
//         return (parts.Length >= 2 && int.TryParse(parts[1], out int i)) ? i : -1;
//     }

//     // Class to store nearby point information
//     [System.Serializable]
//     private class NearbyPointInfo
//     {
//         public int id;
//         public int pid;
//         public Vector3 pos;
//         public float distance;

//         public NearbyPointInfo(int id, int pid, Vector3 pos, float distance)
//         {
//             this.id = id;
//             this.pid = pid;
//             this.pos = pos;
//             this.distance = distance;
//         }
//     }
// }














































