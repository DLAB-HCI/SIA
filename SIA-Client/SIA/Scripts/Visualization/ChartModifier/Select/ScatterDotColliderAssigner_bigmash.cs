









































// using UnityEngine;
// using IATK;
// using System.Collections.Generic;

// [RequireComponent(typeof(AbstractVisualisation))]
// public class ScatterDotColliderAssigner : MonoBehaviour
// {
//     [Header("Rendering / Collider")]
//     [SerializeField] private string layerName = "ScatterDot";
//     [SerializeField] private float colliderRadius = 0.01f;
//     [SerializeField] private float visualRadius = 0.01f;
//     [SerializeField] private Color dotColor = new Color(0.6509804f, 0.4627451f, 0.1137255f);
//     [Range(0f, 1f)][SerializeField] private float transparency = 0.0f;
//     [SerializeField] private Material dotMaterial;
//     [SerializeField] private bool useCustomMaterial = false;
//     [SerializeField] private bool useRandomColors = false;

//     // Pooled data point objects
//     private GameObject[] dataPointObjects;

//     // Shared resources (prevent leaks)
//     private Material sharedDefaultMaterial;
//     private Mesh sharedSphereMesh;

//     // Cache
//     private AbstractVisualisation viz;
//     private int cachedLayer;
//     private Transform pointsParent;

//     void Awake()
//     {
//         viz = GetComponent<AbstractVisualisation>();
//         if (viz == null)
//         {
//             Debug.LogError("[ScatterDotColliderAssigner] AbstractVisualisation is missing.");
//             enabled = false;
//             return;
//         }
//         cachedLayer = LayerMask.NameToLayer(layerName);
//         if (cachedLayer < 0) cachedLayer = 0; // Safety fallback

//         // Prepare shared resources
//         if (!useCustomMaterial || dotMaterial == null)
//         {
//             sharedDefaultMaterial = BuildDefaultTransparentMaterial(dotColor, transparency);
//         }
//         sharedSphereMesh = CreateSphereMesh();
//     }

//     void Start()
//     {
//         InitializeColliders();
//     }

//     /// <summary>
//     /// Initial creation (pooling). After that, only toggle/update position without destroying.
//     /// </summary>
//     public void InitializeColliders()
//     {
//         if (viz == null || viz.viewList.Count == 0)
//         {
//             Debug.LogError("[ScatterDotColliderAssigner] No view found in the visualization.");
//             return;
//         }

//         // Use View transform as parent
//         pointsParent = viz.viewList[0].transform;

//         var verts = viz.viewList[0].BigMesh.getBigMeshVertices();
//         EnsurePoolSize(verts.Length);

//         for (int i = 0; i < verts.Length; i++)
//         {
//             var go = dataPointObjects[i];

//             // Reassign parent if different (worldPositionStays=false to keep local coordinates)
//             if (go.transform.parent != pointsParent)
//                 go.transform.SetParent(pointsParent, false);

//             go.transform.localPosition = verts[i]; // Local coordinates in View space
//         }

//         Debug.Log($"[ScatterDotColliderAssigner] Initialization complete: pooled {verts.Length} points");
//     }
//     /// <summary>
//     /// Update positions using BigMesh vertices from the current view (SSOT).
//     /// </summary>
//     public void RefreshFromVisualisation()
//     {
//         if (viz == null || viz.viewList.Count == 0)
//         {
//             Debug.LogWarning("[ScatterDotColliderAssigner] RefreshFromVisualisation: visualization/view missing");
//             return;
//         }

//         // Always use the latest View as parent
//         pointsParent = viz.viewList[0].transform;

//         var verts = viz.viewList[0].BigMesh.getBigMeshVertices();
//         EnsurePoolSize(verts.Length);

//         int count = Mathf.Min(verts.Length, dataPointObjects.Length);
//         for (int i = 0; i < count; i++)
//         {
//             var go = dataPointObjects[i];

//             if (go.transform.parent != pointsParent)
//                 go.transform.SetParent(pointsParent, false);

//             go.transform.localPosition = verts[i]; // View local
//         }

//         for (int i = count; i < dataPointObjects.Length; i++)
//         {
//             if (dataPointObjects[i] != null)
//                 dataPointObjects[i].SetActive(false);
//         }
//     }

//     /// <summary>
//     /// Activate/render only live indices (domain/filter mask can be passed here in later stages).
//     /// When hideInactive=true, hide inactive points (SetActive(false)); otherwise keep visible and only toggle Collider/Renderer.
//     /// </summary>
//     public void UpdateActiveByMask(ICollection<int> activeIndices, bool hideInactive = true)
//     {
//         if (dataPointObjects == null) return;

//         // HashSet is recommended for O(1) lookups
//         HashSet<int> active = activeIndices as HashSet<int> ?? new HashSet<int>(activeIndices);

//         for (int i = 0; i < dataPointObjects.Length; i++)
//         {
//             var go = dataPointObjects[i];
//             if (go == null) continue;

//             bool isActive = active.Contains(i);

//             if (hideInactive)
//             {
//                 go.SetActive(isActive);
//             }
//             else
//             {
//                 // Use this when you want to keep rendering but toggle interaction only
//                 var col = go.GetComponent<Collider>();
//                 if (col != null) col.enabled = isActive;

//                 var r = go.GetComponent<Renderer>();
//                 if (r != null) r.enabled = isActive;
//             }
//         }
//     }

//     /// <summary>
//     /// Global interaction toggle (keep visibility, toggle Collider only).
//     /// Useful for global UI states such as Saved Panel.
//     /// </summary>
//     public void SetInteractionEnabled(bool enabled)
//     {
//         if (dataPointObjects == null) return;

//         foreach (var go in dataPointObjects)
//         {
//             if (go == null) continue;
//             var col = go.GetComponent<Collider>();
//             if (col != null) col.enabled = enabled;
//         }
//     }

//     // ---------- Internal utilities ----------

//     /// <summary>
//     /// Ensure pool size matches current vertex count. Create when short, deactivate when extra.
//     /// </summary>
//     private void EnsurePoolSize(int needed)
//     {
//         if (dataPointObjects == null)
//         {
//             dataPointObjects = new GameObject[needed];
//         }

//         // Expansion required
//         if (dataPointObjects.Length < needed)
//         {
//             var newArr = new GameObject[needed];
//             for (int i = 0; i < dataPointObjects.Length; i++)
//                 newArr[i] = dataPointObjects[i];
//             dataPointObjects = newArr;
//         }

//         // Create or reactivate
//         for (int i = 0; i < needed; i++)
//         {
//             if (dataPointObjects[i] == null)
//             {
//                 dataPointObjects[i] = CreatePointObject(i);
//             }
//             else
//             {
//                 dataPointObjects[i].SetActive(true);
//             }
//         }
//     }

//     private GameObject CreatePointObject(int index)
//     {
//         var go = new GameObject($"DataPoint_{index}");

//         // Always parent to the current View
//         var parent = pointsParent != null ? pointsParent : (viz != null && viz.viewList.Count > 0 ? viz.viewList[0].transform : viz.transform);
//         go.transform.SetParent(parent, false);

//         go.transform.localScale = Vector3.one * (visualRadius * 2f);
//         go.layer = cachedLayer;

//         // Collider
//         var col = go.AddComponent<SphereCollider>();
//         col.radius = colliderRadius / Mathf.Max(0.0001f, visualRadius); // Scale compensation

//         // Renderer + Mesh
//         var renderer = go.AddComponent<MeshRenderer>();
//         var mf = go.AddComponent<MeshFilter>();
//         mf.sharedMesh = sharedSphereMesh;

//         if (useCustomMaterial && dotMaterial != null)
//         {
//             renderer.sharedMaterial = dotMaterial;
//         }
//         else if (useRandomColors)
//         {
//             var m = BuildDefaultTransparentMaterial(new Color(Random.value, Random.value, Random.value), transparency);
//             renderer.material = m; // Instance material (random color)
//         }
//         else
//         {
//             renderer.sharedMaterial = sharedDefaultMaterial; // Shared material
//         }

//         return go;
//     }

//     private Material BuildDefaultTransparentMaterial(Color baseColor, float alpha)
//     {
//         var m = new Material(Shader.Find("Standard"));
//         m.SetFloat("_Mode", 3); // Transparent
//         m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
//         m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
//         m.SetInt("_ZWrite", 0);
//         m.DisableKeyword("_ALPHATEST_ON");
//         m.EnableKeyword("_ALPHABLEND_ON");
//         m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
//         m.renderQueue = 3000;

//         var c = baseColor; c.a = Mathf.Clamp01(alpha);
//         m.color = c;
//         return m;
//     }

//     // Simple shared sphere mesh factory
//     private Mesh CreateSphereMesh()
//     {
//         var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
//         var mesh = temp.GetComponent<MeshFilter>().sharedMesh;
//         Destroy(temp);
//         return mesh;
//     }
// }
