using UnityEngine;
using UnityEngine.Events; // <-- Needed for UnityEvent
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class EyeInteractable : MonoBehaviour
{
    public bool IsHovered { get; set; }

    [SerializeField]
    private UnityEvent<GameObject> onObjectHover;

    [SerializeField]
    private Material OnHoverActiveMaterial;
    [SerializeField]
    private Material OnHoverInactiveMaterial;

    private MeshRenderer meshRenderer;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null && OnHoverInactiveMaterial != null)
            meshRenderer.material = OnHoverInactiveMaterial;
    }

    void Update()
    {
        if (meshRenderer == null) return;

        if (IsHovered)
        {
            if (OnHoverActiveMaterial != null)
                meshRenderer.material = OnHoverActiveMaterial;
            onObjectHover?.Invoke(gameObject);
        }
        else
        {
            if (OnHoverInactiveMaterial != null)
                meshRenderer.material = OnHoverInactiveMaterial;
        }
    }
}
