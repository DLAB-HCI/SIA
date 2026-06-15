using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using IATK;

public class AngleShifter : MonoBehaviour
{
    private ScatterplotVisualisation scatterplotVisualisation;

    void Start()
    {
        scatterplotVisualisation = GetComponent<ScatterplotVisualisation>();
    }

    public void AngleShift(string jsonStructure)
    {
        if (!string.IsNullOrEmpty(jsonStructure))
        {
            JObject spec = JObject.Parse(jsonStructure);
            print("data: " + spec);

            ValidateAndFixAngleValue(spec, "encoding.x.angle");
            ValidateAndFixAngleValue(spec, "encoding.y.angle");
            ValidateAndFixAngleValue(spec, "encoding.z.angle");

            if (spec.ContainsKey("encoding"))
            {
                JObject encoding = spec["encoding"].ToObject<JObject>();

                if (encoding.ContainsKey("x") && encoding["x"].ToObject<JObject>().ContainsKey("angle"))
                {
                    float xAngle = encoding["x"]["angle"].ToObject<float>();
                    RotateVisualization("x", xAngle);
                }

                if (encoding.ContainsKey("y") && encoding["y"].ToObject<JObject>().ContainsKey("angle"))
                {
                    float yAngle = encoding["y"]["angle"].ToObject<float>();
                    RotateVisualization("y", yAngle);
                }

                if (encoding.ContainsKey("z") && encoding["z"].ToObject<JObject>().ContainsKey("angle"))
                {
                    float zAngle = encoding["z"]["angle"].ToObject<float>();
                    RotateVisualization("z", zAngle);
                }
            }
        }
        else
        {
             Debug.LogError("JSON structure is empty!");
        }
    }

    private void ValidateAndFixAngleValue(JObject spec, string path)
    {
        JToken angleToken = spec.SelectToken(path);
        if (angleToken != null)
        {
            if (double.TryParse(angleToken.ToString(), out double angleValue))
            {
                spec[path] = angleValue;
            }
            else
            {
                spec[path] = 0; //    
            }
        }
    }

    private void RotateVisualization(string axis, float angle)
    {
        Vector3 rotationAxis = Vector3.zero;

        switch (axis)
        {
            case "x":
                rotationAxis = Vector3.right;
                break;
            case "y":
                rotationAxis = Vector3.up;
                break;
            case "z":
                rotationAxis = Vector3.forward;
                break;
        }

        scatterplotVisualisation.transform.Rotate(rotationAxis, angle, Space.World);
        Debug.Log($"Rotated {axis} by {angle} degrees");
    }

    void Update()
    {
        
    }
}
