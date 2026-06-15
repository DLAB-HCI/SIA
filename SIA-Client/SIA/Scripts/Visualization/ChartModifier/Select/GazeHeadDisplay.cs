using UnityEngine;
using TMPro;

public class GazeHeadDisplay : MonoBehaviour
{
    [Header("UI Text Field (assign in Inspector)")]
    public TMP_Text gazeHead;

    public void UpdateTarget(string json)
    {
        if (gazeHead == null)
        {
            Debug.LogWarning("GazeTargetUIDisplay: displayText is not assigned.");
            return;
        }

        gazeHead.text = json;
    }
}
