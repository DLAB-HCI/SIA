using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class EyeTrackingRay : MonoBehaviour
{
    [SerializeField]
    private float rayDistance = 1.0f;
    [SerializeField]
    private float rayWidth = 0.01f;
    [SerializeField]
    private LayerMask layerToInclude;
    [SerializeField]
    private Color rayColorDefaultState = Color.white;
    [SerializeField]
    private Color rayColorHoverState = Color.black;
    [SerializeField]
    private LineRenderer lineRenderer;

    private List<EyeInteractable> eyeTrackingRays = new List<EyeInteractable>();

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        SetupRay();
    }
    void SetupRay()
    {
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = rayWidth;
        lineRenderer.endWidth = rayWidth;
        lineRenderer.startColor = rayColorDefaultState;
        lineRenderer.endColor = rayColorDefaultState;
        lineRenderer.SetPosition(0, transform.position);
        lineRenderer.SetPosition(1, transform.position + transform.forward * rayDistance);
    }

    void FixedUpdate()
    {
        RaycastHit hit;
        Vector3 rayCastDirection = transform.TransformDirection(Vector3.forward) * rayDistance;

        if (Physics.Raycast(transform.position, rayCastDirection, out hit, rayDistance, layerToInclude))
        {
            UnSelect();
            lineRenderer.startColor = rayColorHoverState;
            lineRenderer.endColor = rayColorHoverState;
            var eyeInteractable = hit.transform.GetComponent<EyeInteractable>();
            if (eyeInteractable != null && !eyeTrackingRays.Contains(eyeInteractable))
            {
                eyeTrackingRays.Add(eyeInteractable);
                eyeInteractable.IsHovered = true;
            }
        }
        else
        {
            lineRenderer.startColor = rayColorDefaultState;
            lineRenderer.endColor = rayColorDefaultState;
            UnSelect(true);
        }
    }
    void UnSelect(bool reset = false)
    {
        foreach (var interactable in eyeTrackingRays)
        {
            if (interactable != null)
                interactable.IsHovered = false;
        }
        if (reset)
        {
            eyeTrackingRays.Clear();
        }
    }
}
