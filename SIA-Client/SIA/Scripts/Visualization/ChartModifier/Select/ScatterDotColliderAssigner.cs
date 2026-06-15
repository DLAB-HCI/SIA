using UnityEngine;
using IATK;
using System.Collections.Generic;


[RequireComponent(typeof(AbstractVisualisation))]
public class ScatterDotColliderAssigner : MonoBehaviour
{
    [Header("Rendering / Collider")]
    [SerializeField] private string layerName = "ScatterDot";
    [SerializeField] private float colliderRadius = 0.01f;
    [SerializeField] private float visualRadius = 0.01f;
    [SerializeField] private Color dotColor = new Color(0.6509804f, 0.4627451f, 0.1137255f);
    [Range(0f, 1f)][SerializeField] private float transparency = 0.0f;
    [SerializeField] private Material dotMaterial;
    [SerializeField] private bool useCustomMaterial = false;
    [SerializeField] private bool useRandomColors = false;

    private GameObject[] dataPointObjects;

    private Material sharedDefaultMaterial;
    private Mesh sharedSphereMesh;

    private AbstractVisualisation viz;
    private int cachedLayer;
    private Transform pointsParent;

    void Awake()
    {
        viz = GetComponent<AbstractVisualisation>();
        if (viz == null)
        {
            Debug.LogError("[ScatterDotColliderAssigner] AbstractVisualisation .");
            enabled = false;
            return;
        }
        cachedLayer = LayerMask.NameToLayer(layerName);
        if (cachedLayer < 0) cachedLayer = 0; // 

        if (!useCustomMaterial || dotMaterial == null)
        {
            sharedDefaultMaterial = BuildDefaultTransparentMaterial(dotColor, transparency);
        }
        sharedSphereMesh = CreateSphereMesh();
    }

    void Start()
    {
        StartCoroutine(DelayedInit());
    }

private System.Collections.IEnumerator DelayedInit()
{
    yield return new WaitForSeconds(0.5f); //  WaitForEndOfFrame
    InitializeColliders();
}

public void InitializeColliders()
{
    Debug.Log($"[DEBUG] viewList.Count={viz.viewList.Count}");
    Vector3[] verts = null;
    if (viz.viewList.Count > 0 && viz.viewList[0].BigMesh != null)
    {
        verts = viz.viewList[0].BigMesh.getBigMeshVertices();
        Debug.Log($"[DEBUG] verts.Length={verts.Length}");
    }
    else
    {
        Debug.Log("[DEBUG] BigMesh  viewList[0] null.");
        return;
    }

    if (viz == null || viz.viewList.Count == 0)
    {
        Debug.LogError("[ScatterDotColliderAssigner]   .");
        return;
    }

    pointsParent = viz.viewList[0].transform;

    EnsurePoolSize(verts.Length);

    for (int i = 0; i < verts.Length; i++)
    {
        var go = dataPointObjects[i];

        if (go.transform.parent != pointsParent)
            go.transform.SetParent(pointsParent, false);

        go.transform.localPosition = verts[i]; //  View   
    }

    Debug.Log($"[ScatterDotColliderAssigner]  : {verts.Length}  ");
}






    public void RefreshFromVisualisation()
    {
        if (viz == null || viz.viewList.Count == 0)
        {
            Debug.LogWarning("[ScatterDotColliderAssigner] RefreshFromVisualisation: / ");
            return;
        }

        pointsParent = viz.viewList[0].transform;

        var verts = viz.viewList[0].BigMesh.getBigMeshVertices();
        EnsurePoolSize(verts.Length);

        int count = Mathf.Min(verts.Length, dataPointObjects.Length);
        for (int i = 0; i < count; i++)
        {
            var go = dataPointObjects[i];

            if (go.transform.parent != pointsParent)
                go.transform.SetParent(pointsParent, false);

            go.transform.localPosition = verts[i]; //  View 
        }

        for (int i = count; i < dataPointObjects.Length; i++)
        {
            if (dataPointObjects[i] != null)
                dataPointObjects[i].SetActive(false);
        }
    }

    public void UpdateActiveByMask(ICollection<int> activeIndices, bool hideInactive = true)
    {
        if (dataPointObjects == null) return;

        HashSet<int> active = activeIndices as HashSet<int> ?? new HashSet<int>(activeIndices);

        for (int i = 0; i < dataPointObjects.Length; i++)
        {
            var go = dataPointObjects[i];
            if (go == null) continue;

            bool isActive = active.Contains(i);

            if (hideInactive)
            {
                go.SetActive(isActive);
            }
            else
            {
                var col = go.GetComponent<Collider>();
                if (col != null) col.enabled = isActive;

                var r = go.GetComponent<Renderer>();
                if (r != null) r.enabled = isActive;
            }
        }
    }

    public void SetInteractionEnabled(bool enabled)
    {
        if (dataPointObjects == null) return;

        foreach (var go in dataPointObjects)
        {
            if (go == null) continue;
            var col = go.GetComponent<Collider>();
            if (col != null) col.enabled = enabled;
        }
    }


    private void EnsurePoolSize(int needed)
    {
        if (dataPointObjects == null)
        {
            dataPointObjects = new GameObject[needed];
        }

        if (dataPointObjects.Length < needed)
        {
            var newArr = new GameObject[needed];
            for (int i = 0; i < dataPointObjects.Length; i++)
                newArr[i] = dataPointObjects[i];
            dataPointObjects = newArr;
        }

        for (int i = 0; i < needed; i++)
        {
            if (dataPointObjects[i] == null)
            {
                dataPointObjects[i] = CreatePointObject(i);
            }
            else
            {
                dataPointObjects[i].SetActive(true);
            }
        }
    }

    private GameObject CreatePointObject(int index)
    {
        var go = new GameObject($"DataPoint_{index}");

        var parent = pointsParent != null ? pointsParent : (viz != null && viz.viewList.Count > 0 ? viz.viewList[0].transform : viz.transform);
        go.transform.SetParent(parent, false);

        go.transform.localScale = Vector3.one * (visualRadius * 2f);
        go.layer = cachedLayer;

        var col = go.AddComponent<SphereCollider>();
        col.radius = colliderRadius / Mathf.Max(0.0001f, visualRadius); //  

        var renderer = go.AddComponent<MeshRenderer>();
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = sharedSphereMesh;

        if (useCustomMaterial && dotMaterial != null)
        {
            renderer.sharedMaterial = dotMaterial;
        }
        else if (useRandomColors)
        {
            var m = BuildDefaultTransparentMaterial(new Color(Random.value, Random.value, Random.value), transparency);
            renderer.material = m; // ()
        }
        else
        {
            renderer.sharedMaterial = sharedDefaultMaterial; // 
        }

        return go;
    }

    private Material BuildDefaultTransparentMaterial(Color baseColor, float alpha)
    {
        var m = new Material(Shader.Find("Standard"));
        m.SetFloat("_Mode", 3); // Transparent
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.renderQueue = 3000;

        var c = baseColor; c.a = Mathf.Clamp01(alpha);
        m.color = c;
        return m;
    }

    private Mesh CreateSphereMesh()
    {
        var temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        Destroy(temp);
        return mesh;
    }

    
    
}
