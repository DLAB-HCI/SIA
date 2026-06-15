using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class Panel_HorizontalText : MonoBehaviour
{
    public Text tutorialText;
    public Text leftColumnsText;
    public Text rightColumnsText;

    [Header("  ")]
    [TextArea(2, 10)]
    public string displayText = "\nTutorial\nSay, I am interested in exploring houses with above $200,000 sales price.\nAsk the system to remove all filters so you can view all the data.";

    private static string[] s_columns =
    {
        "PID", "SalePrice", "Zone_by_Residential_Types", "Lot_Area(sq)",
        "Lot_Shape", "Bldg_Type", "Floor_Count", "Overall_Condition", "Year_Built",
        "Year_Remodeling", "Exterior", "Exterior_Condition", "Room_Count",
        "Full_Bath_Count", "Half_Bath_Count", "Basement(sq)", "Heating",
        "Heating_Quality", "Central Air", "Garage_Car_Capacity", "Garage_Area(sq)",
        "Fireplaces_Count", "Utilities", "Surroundings", "Land_Contour", "House_Location"
    };

    private void Awake()
    {
        //    
        if (tutorialText == null)
        {
            var tutGO = new GameObject("TutorialText", typeof(RectTransform), typeof(Text));
            tutGO.transform.SetParent(transform, false);
            tutorialText = tutGO.GetComponent<Text>();

            tutorialText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tutorialText.fontSize = 36;
            tutorialText.color = Color.white;
            tutorialText.alignment = TextAnchor.UpperLeft;
            tutorialText.horizontalOverflow = HorizontalWrapMode.Wrap;
            tutorialText.verticalOverflow = VerticalWrapMode.Overflow;
            tutorialText.raycastTarget = false;

            var rt = tutGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.65f); //  
            rt.anchorMax = new Vector2(0.95f, 0.95f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        //   
        if (leftColumnsText == null)
        {
            var leftGO = new GameObject("LeftColumnsText", typeof(RectTransform), typeof(Text));
            leftGO.transform.SetParent(transform, false);
            leftColumnsText = leftGO.GetComponent<Text>();

            leftColumnsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            leftColumnsText.fontSize = 34;
            leftColumnsText.color = Color.white;
            leftColumnsText.alignment = TextAnchor.UpperLeft;
            leftColumnsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            leftColumnsText.verticalOverflow = VerticalWrapMode.Overflow;
            leftColumnsText.raycastTarget = false;

            var rt = leftGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.05f);
            rt.anchorMax = new Vector2(0.45f, 0.60f); //   
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        //   
        if (rightColumnsText == null)
        {
            var rightGO = new GameObject("RightColumnsText", typeof(RectTransform), typeof(Text));
            rightGO.transform.SetParent(transform, false);
            rightColumnsText = rightGO.GetComponent<Text>();

            rightColumnsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rightColumnsText.fontSize = 34;
            rightColumnsText.color = Color.white;
            rightColumnsText.alignment = TextAnchor.UpperLeft;
            rightColumnsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            rightColumnsText.verticalOverflow = VerticalWrapMode.Overflow;
            rightColumnsText.raycastTarget = false;

            var rt = rightGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.55f, 0.05f);
            rt.anchorMax = new Vector2(0.95f, 0.60f); //   
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ---   ---
        tutorialText.text = displayText + "\n\n--- Columns ---";

        int half = Mathf.CeilToInt(s_columns.Length / 2f);

        StringBuilder sbLeft = new StringBuilder();
        for (int i = 0; i < half; i++)
        {
            sbLeft.AppendLine(s_columns[i]);
        }

        StringBuilder sbRight = new StringBuilder();
        for (int i = half; i < s_columns.Length; i++)
        {
            sbRight.AppendLine(s_columns[i]);
        }

        leftColumnsText.text = sbLeft.ToString();
        rightColumnsText.text = sbRight.ToString();
    }
}
