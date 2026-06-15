using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

public class Panel_HudUI : MonoBehaviour
{
    private const string SpecCompilerRelativePath = "SIA/Scripts/Visualization/SpecCompiler";

    public Image progressBar;
    public Text statusText;
    public Image recordIcon;      //   
    public Image serverIcon;      //   
    private Sprite micOnSprite;
    private Sprite micOffSprite;
    private static Sprite solidSprite; //  
    public Panel_HubLegend legend;  // Inspector  or  GetComponent<Panel_HubLegend>()

    private static string GetCurrentChartSpecPath()
    {
        return Path.Combine(Application.dataPath, SpecCompilerRelativePath, "chartSpec_current.json");
    }

    private static Sprite GetSolidSprite()
    {
        if (solidSprite != null) return solidSprite;

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply(false, true); //    OK

        solidSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1),
                                    new Vector2(0.5f, 0.5f), 1f);
        solidSprite.name = "GeneratedSolid1x1";
        return solidSprite;
    }
    private void Awake()
    {
        micOnSprite = LoadSpriteRobust("DataSets/mic_on");   //  subSpriteName 
        micOffSprite = LoadSpriteRobust("DataSets/mic_off");
        if (progressBar) progressBar.fillAmount = 0f;
        Debug.Log($"micOnSprite: {(micOnSprite ? "OK" : "NULL")}, micOffSprite: {(micOffSprite ? "OK" : "NULL")}");
    }


    //     
    public void SetRecordingIcon(bool isRecording)
    {
        if (recordIcon != null)
            recordIcon.sprite = isRecording ? micOnSprite : micOffSprite;
    }
    public void Init(Image bar, Text txt)
    {
        progressBar = bar;
        statusText = txt;
    }
    void Start()
    {
        //       ()
        if (micOnSprite == null) micOnSprite = Resources.Load<Sprite>("DataSets/mic_on");
        if (micOffSprite == null) micOffSprite = Resources.Load<Sprite>("DataSets/mic_off");

        if (recordIcon != null)
        {
            recordIcon.preserveAspect = true;
            SetRecordingIcon(false); //   OFF
        }

        //  Inspector     
        if (legend == null)
            legend = GetComponent<Panel_HubLegend>();
        if (legend == null)
            legend = gameObject.AddComponent<Panel_HubLegend>();

        //  HUD   
        string jsonPath = GetCurrentChartSpecPath();

        if (legend == null)
            legend = gameObject.AddComponent<Panel_HubLegend>();

        //     
        if (legend != null)
        {
            legend.InitLegendPanel();
            legend.AddLegendEntry(
                new Color(0.7294118f, 0.6901961f, 0.6745098f),
                "All Houses",
                new Vector2(0, 0)   //   (60, -40)  (0,0)  
            );
        }

    }
    public void RefreshLegend(string jsonPath)
    {
        if (legend != null)
            legend.RefreshLegendFromSpec(jsonPath);
    }


    //     
    public void UpdateLegendFromChart(Color[] colors, string[] labels)
    {
        if (legend != null)
            legend.UpdateLegendFromColors(colors, labels);
    }
    public static Panel_HudUI CreateHudPanelUI(GameObject panel)
    {
        var solid = GetSolidSprite();

        //   
        var bgGO = new GameObject("ProgressBarBG", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(panel.transform, false);
        var bgImg = bgGO.GetComponent<Image>();
        bgImg.sprite = solid;          //   
        bgImg.type = Image.Type.Simple;
        bgImg.color = Color.white;
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.0f, 0.99f);
        bgRT.anchorMax = new Vector2(1.0f, 1.0f);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        bgRT.sizeDelta = new Vector2(0, 2);

        //    (Filled)
        var progressGO = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image));
        progressGO.transform.SetParent(panel.transform, false);
        var progressImg = progressGO.GetComponent<Image>();
        progressImg.sprite = solid;     //   
        progressImg.type = Image.Type.Filled;
        progressImg.fillMethod = Image.FillMethod.Horizontal;
        progressImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        progressImg.fillAmount = 0f;
        progressImg.color = new Color(9f / 255f, 102f / 255f, 255f / 255f, 1.0f);

        var progressRT = progressGO.GetComponent<RectTransform>();
        progressRT.anchorMin = new Vector2(0.0f, 0.99f);
        progressRT.anchorMax = new Vector2(1.0f, 1.0f);
        progressRT.offsetMin = Vector2.zero;
        progressRT.offsetMax = Vector2.zero;
        progressRT.sizeDelta = new Vector2(0, 2);


        //  
        var textGO = new GameObject("StatusText", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(panel.transform, false);
        var statusText = textGO.GetComponent<Text>();
        statusText.text = "waiting...";
        statusText.color = Color.white;
        statusText.fontSize = 32;
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0.5f);
        textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.anchoredPosition = Vector2.zero;

        //   
        var iconGO = new GameObject("RecordIcon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(panel.transform, false);
        var iconImg = iconGO.GetComponent<Image>();
        var iconRT = iconGO.GetComponent<RectTransform>();

        //   
        iconRT.anchorMin = new Vector2(0f, 1.3f);    //  
        iconRT.anchorMax = new Vector2(0f, 1.3f);    //  
        iconRT.pivot = new Vector2(0f, 1.3f);    //   
        iconRT.sizeDelta = new Vector2(40, 40);      //  
        iconRT.anchoredPosition = new Vector2(0, 0); //     

        iconImg.preserveAspect = true;           //   
        iconImg.raycastTarget = false;           // () / 

        //  Panel_HudUI    
        var uiComp = panel.GetComponent<Panel_HudUI>();
        if (uiComp == null) uiComp = panel.AddComponent<Panel_HudUI>();

        uiComp.Init(progressImg, statusText);
        uiComp.recordIcon = iconImg;

        //      (      )
        uiComp.SetRecordingIcon(false);

        return uiComp;
    }

    // Panel_HudUI.cs
    public void SetProgress01(float p)
    {
        if (!progressBar) return;
        if (progressBar.type != Image.Type.Filled)
        {
            progressBar.type = Image.Type.Filled;
            progressBar.fillMethod = Image.FillMethod.Horizontal;
            progressBar.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
        progressBar.fillAmount = Mathf.Clamp01(p);
    }

    public void SetStatus(string s)
    {
        if (statusText) statusText.text = s;
    }

    private static Sprite LoadSpriteRobust(string path, string subSpriteNameIfMultiple = null)
    {
        // 1)  Sprite 
        var s = Resources.Load<Sprite>(path);
        if (s != null) return s;

        // 2) Multiple Sprite() 
        var arr = Resources.LoadAll<Sprite>(path);
        if (arr != null && arr.Length > 0)
        {
            if (!string.IsNullOrEmpty(subSpriteNameIfMultiple))
            {
                foreach (var sp in arr) if (sp.name == subSpriteNameIfMultiple) return sp;
            }
            return arr[0]; //   
        }

        // 3) Texture2D   Sprite  (Import Texture )
        var tex = Resources.Load<Texture2D>(path);
        if (tex != null)
        {
            var created = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            created.name = System.IO.Path.GetFileName(path);
            return created;
        }

        Debug.LogError($"[HUD] Sprite not found at '{path}'. " +
                    $": Assets/Resources/{path}.png, TextureType=Sprite or Texture, Editor X, ");
        return null;
    }

    // Panel_HudUI.cs
    public void RefreshLegendFromJson(string json)
    {
        if (legend == null) return;

        try
        {
            JObject spec = JObject.Parse(json);
            var colorSpec = spec.SelectToken("$.encoding.color.condition");
            if (colorSpec == null)
            {
                Debug.LogWarning("[HUD] No color.condition found in JSON, skipping legend update");
                return;
            }

            List<Color> colors = new List<Color>();
            List<string> labels = new List<string>();

            foreach (var cond in colorSpec)
            {
                string hex = cond["value"]?.ToString();
                string test = cond["test"]?.ToString();
                if (string.IsNullOrEmpty(hex) || string.IsNullOrEmpty(test)) continue;

                if (ColorUtility.TryParseHtmlString(hex, out var col))
                    colors.Add(col);
                else
                    colors.Add(Color.gray);

                labels.Add(legend.ExtractCategoryLabel(test));

            }

            legend.UpdateLegendFromColors(colors.ToArray(), labels.ToArray());
            Debug.Log($"[HUD]  Legend updated with {labels.Count} entries");
        }
        catch (Exception e)
        {
            Debug.LogError("[HUD]  Failed to parse legend JSON: " + e.Message);
        }
    }
}