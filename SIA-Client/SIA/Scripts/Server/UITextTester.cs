using UnityEngine;
using TMPro;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;   // JObject 사용 시 필요
using System.IO; // 꼭 추가
using System.Collections.Generic;
namespace SIA.Visualization
{
    using Newtonsoft.Json;

    public class UITextTester : MonoBehaviour
    {
        [Header("Existing UI Text Fields")]
        public TMP_Text notiTextUI;
        public TMP_Text inputTextUI;

        [Header("Extra UI fields for each JSON key")]
        public TMP_Text uncertainty;
        public TMP_Text embodiment;
        public TMP_Text spatialContextResolver;
        public TMP_Text feedForward;

        // ---- helpers ----
        static string SafeJoin<T>(IEnumerable<T> seq, string sep = ", ")
        {
            if (seq == null) return "";
            return string.Join(sep, seq.Select(v => v?.ToString()));
        }

        // ---- APIs used by SpeechSender ----
        public void DisplayFileSent(string notiText)
        {
            Debug.Log($"[UITextTester] DisplayFilㅁㅁㅁㅁeSent: {notiText}");
            if (notiTextUI != null) notiTextUI.text = "File to Server: " + notiText;
        }

        public void DisplayInput(string inputText)
        {
            Debug.Log($"[UITextTester] DisplayInput: {inputText}");
            if (inputTextUI != null) inputTextUI.text = "Input (to Whisper/GPT): " + inputText;
        }

        // ---- Main JSON renderer ----
        public void DisplayParsedChatResult(string json)
        {
            Debug.Log($"[UITextTester] DisplayParsedChatResult JSON: {json}");
            try
            {
                var root = JObject.Parse(json); // 신뢰 소스

                // 1) Uncertainty  —— task_LLM_original 우선, 없으면 task로 폴백
                if (uncertainty != null)
                {
                    string task =
                        (string?)root.SelectToken("$.module_spec.result.updated_module_spec.speechPatternAnalyzer.uncertainty.calibrated_task")
                        ?? (string?)root.SelectToken("$.updated_module_spec.speechPatternAnalyzer.uncertainty.calibrated_task")
                        ?? (string?)root.SelectToken("$.module_spec.result.updated_module_spec.speechPatternAnalyzer.uncertainty.task_LLM_original")
                        ?? (string?)root.SelectToken("$.updated_module_spec.speechPatternAnalyzer.uncertainty.task_LLM_original")
                        ?? "unknown";

                    double? conf =
                        (double?)root.SelectToken("$.module_spec.result.updated_module_spec.speechPatternAnalyzer.uncertainty.calibrated_confidence");
                    double? ent =
                        (double?)root.SelectToken("$.module_spec.result.updated_module_spec.speechPatternAnalyzer.uncertainty.calibrated_entropy");

                    string label; double value;
                    if (conf.HasValue) { label = "Confidence"; value = conf.Value; }
                    else if (ent.HasValue) { label = "Entropy"; value = ent.Value; }
                    else { label = "Entropy"; value = 0; }

                    uncertainty.text = $"[Uncertainty]\nTask (LLM): {task}\n{label}: {value:F4}";
                }


                // 2) Embodiment (kebab-case 태그 직접 매핑)
                if (embodiment != null)
                {
                    string intensity =
                        (string?)root.SelectToken("$.module_spec.result.updated_module_spec.speechPatternAnalyzer.embodiment.intensity")
                        ?? "unknown";

                    var tagsTok = root.SelectToken("$.module_spec.result.updated_module_spec.speechPatternAnalyzer.embodiment.tags");
                    int DP = (int?)tagsTok?["Deictic-Point"] ?? 0;
                    int DB = (int?)tagsTok?["Deictic-Boundary"] ?? 0;
                    int DT = (int?)tagsTok?["Deictic-Time"] ?? 0;
                    int SDIR = (int?)tagsTok?["Spatial-Direction"] ?? 0;
                    int SDIS = (int?)tagsTok?["Spatial-Distance"] ?? 0;
                    int SB = (int?)tagsTok?["Spatial-Body"] ?? 0;
                    int SA = (int?)tagsTok?["Spatial-Area"] ?? 0;

                    string tagsText = $"DP: {DP}, DB: {DB}, DT: {DT}, SDIR: {SDIR}, SDIS: {SDIS}, SB: {SB}, SA: {SA}";
                    embodiment.text = $"[Embodiment]\nIntensity: {intensity}\nTags: {tagsText}";
                }

                Debug.Log($"[RAW JSON]\n{json}");

                // 3) SpatialContextResolver
                if (spatialContextResolver != null)
                {
                    string targetId =
                        (string?)root.SelectToken("$.module_spec.result.updated_module_spec.spatialContextResolver.gaze.target.id")
                        ?? "none";

                    var posArr = (root.SelectToken("$.module_spec.result.updated_module_spec.spatialContextResolver.gaze.target.pos") as JArray)
                                    ?.Select(t => (float)t).ToArray();

                    var headPos = (root.SelectToken("$.module_spec.result.updated_module_spec.spatialContextResolver.head.pos") as JArray)
                                    ?.Select(t => (float)t).ToArray();

                    // 🔧 viewPlane는 객체
                    var vpTok = root.SelectToken("$.module_spec.result.updated_module_spec.spatialContextResolver.head.viewPlane");

                    string[] axesFields = (vpTok?["axesFields"] as JArray)?.Select(t => (string)t).ToArray();
                    string planeName = (string?)vpTok?["planeName"] ?? "";
                    double? distance = (double?)vpTok?["distance"];
                    float[] planePos = (vpTok?["planePos"] as JArray)?.Select(t => (float)t).ToArray();

                    spatialContextResolver.text =
                        "[SpatialContextResolver]\n"
                        + $"Target: {targetId}\n"
                        + $"Position: [{SafeJoin(posArr)}]\n"
                        + $"Head_Position: [{SafeJoin(headPos)}]\n"
                        + $"viewPlane.axesFields: [{SafeJoin(axesFields)}]\n"
                        + $"viewPlane.planeName: {planeName}\n"
                        + $"viewPlane.distance: {(distance.HasValue ? distance.Value.ToString("F3") : "n/a")}\n"
                        + $"viewPlane.planePos: [{SafeJoin(planePos)}]";
                }
                // 4) FeedForward (updated_module_spec 우선 → gpt_response_json 대체)
                if (feedForward != null)
                {
                    var c1Tok = root.SelectToken("$.module_spec.result.updated_module_spec.dataComposer.feedforward.candidates.c1")
                             ?? root.SelectToken("$.module_spec.result.gpt_response_json.feedforward.candidates.c1");

                    if (c1Tok != null)
                    {
                        var ids = (c1Tok["element_ids"] as JArray)?.Select(t => (int)t).ToArray();
                        var centerPos = (c1Tok["center_id"]?["pos"] as JArray)?.Select(t => (float)t).ToArray();

                        var fx = (c1Tok["filter"]?["x"] as JArray)?.Select(t => (string)t).ToArray();
                        var fy = (c1Tok["filter"]?["y"] as JArray)?.Select(t => (string)t).ToArray();
                        var fz = (c1Tok["filter"]?["z"] as JArray)?.Select(t => (string)t).ToArray();
                        var ft = (c1Tok["filter"]?["type"] as JArray)?.Select(t => (string)t).ToArray();

                        var axes = (c1Tok["navigate"]?["newViewPlane"]?["axes"] as JArray)?.Select(t => (string)t).ToArray();
                        string navField = c1Tok["navigate"]?["newField"] != null
                            ? $"x: {c1Tok["navigate"]["newField"]["x"]}, y: {c1Tok["navigate"]["newField"]["y"]}, z: {c1Tok["navigate"]["newField"]["z"]}"
                            : "none";

                        string binText = "bin: ";
                        var bx = c1Tok["bin"]?["x"]; var by = c1Tok["bin"]?["y"]; var bz = c1Tok["bin"]?["z"];
                        string bxEdges = SafeJoin((bx?["edges"] as JArray)?.Select(t => (float)t).ToArray());
                        string byEdges = SafeJoin((by?["edges"] as JArray)?.Select(t => (float)t).ToArray());
                        string bzEdges = SafeJoin((bz?["edges"] as JArray)?.Select(t => (float)t).ToArray());
                        binText += $"x: (enable={bx?["enable"]}, step={bx?["step"]}) | ";
                        binText += $"y: (enable={by?["enable"]}, step={by?["step"]}) | ";
                        binText += $"z: (enable={bz?["enable"]}, step={bz?["step"]})";
                        if (!string.IsNullOrEmpty(bxEdges)) binText += $"\nedges x: [{bxEdges}]";
                        if (!string.IsNullOrEmpty(byEdges)) binText += $"\nedges y: [{byEdges}]";
                        if (!string.IsNullOrEmpty(bzEdges)) binText += $"\nedges z: [{bzEdges}]";

                        string insight = (string?)c1Tok["insight"] ?? "none";

                        feedForward.text =
                            $"[FeedForward]\nid: [{SafeJoin(ids)}]\ncenter_id.pos: [{SafeJoin(centerPos)}]\n" +
                            $"filter: x: [{SafeJoin(fx)}], y: [{SafeJoin(fy)}], z: [{SafeJoin(fz)}], type: [{SafeJoin(ft)}]\n" +
                            $"navigate.axes: [{SafeJoin(axes)}], field: [{navField}]\n{binText}\ninsight: {insight}";
                    }
                    else
                    {
                        feedForward.text = "[FeedForward] missing";
                    }
                }
                SaveExtractedToFile();
            }
            catch (Exception e)
            {
                Debug.LogError($"[UITextTester] JSON parse error: {e.Message}\n{e.StackTrace}");
                if (uncertainty != null) uncertainty.text = "[Uncertainty] parse error";
                if (embodiment != null) embodiment.text = "[Embodiment] parse error";
                if (spatialContextResolver != null) spatialContextResolver.text = "[SpatialContextResolver] parse error";
                if (feedForward != null) feedForward.text = $"[FeedForward] parse error: {e.Message}";
            }
        }    
    
        // 📌 따로 뽑은 텍스트 저장하는 함수
        void SaveExtractedToFile()
        {
            // 저장할 폴더 (Assets/DebugLogs)
            string folder = Path.Combine(Application.dataPath, "DebugLogs");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // ---- 수집할 텍스트 모음 ----
            List<string> lines = new List<string>();
            if (uncertainty != null) lines.Add(uncertainty.text);
            if (embodiment != null) lines.Add(embodiment.text);
            if (spatialContextResolver != null) lines.Add(spatialContextResolver.text);
            if (feedForward != null) lines.Add(feedForward.text);

            string textContent = string.Join("\n\n", lines);

            // ---- 로깅(JSON 형식) ----
            var logData = new
            {
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                uncertainty = uncertainty?.text,
                embodiment = embodiment?.text,
                spatialContextResolver = spatialContextResolver?.text,
                feedForward = feedForward?.text
            };

            string jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(
                logData, Newtonsoft.Json.Formatting.Indented);

            string timestampedJsonPath = Path.Combine(folder, $"parsed_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(timestampedJsonPath, jsonContent);
            Debug.Log($"[UITextTester] JSON log saved: {timestampedJsonPath}");

            // ---- 모니터링용(텍스트, 항상 최신) ----
            string liveTxtPath = Path.Combine(folder, "parsed_latest.txt");
            File.WriteAllText(liveTxtPath, textContent);
            Debug.Log($"[UITextTester] Live text saved: {liveTxtPath}");
        }


        // --------- (선택) JsonUtility 모델은 fallback 용도로 남겨도 됨 ---------
        [Serializable] public class ChatResult { public ModuleSpec module_spec; }
        [Serializable] public class ModuleSpec { public bool success; public GptResponseJson gpt_response_json; public UpdatedModuleSpec updated_module_spec; }
        [Serializable] public class GptResponseJson { public Feedforward feedforward; }
        [Serializable] public class UpdatedModuleSpec { public SpeechPatternAnalyzer speechPatternAnalyzer; public SpatialContextResolverModel spatialContextResolver; public DataComposer dataComposer; }
        [Serializable] public class SpeechPatternAnalyzer { public UncertaintyModel uncertainty; public EmbodimentModel embodiment; }
        [Serializable] public class UncertaintyModel { public string task; public float entropy; }
        [Serializable] public class EmbodimentModel { public string intensity; public Tags tags; }
        [Serializable] public class Tags { public int DP, DB, DT, SDIR, SDIS, SB, SA; }
        [Serializable] public class SpatialContextResolverModel { public Gaze gaze; public Head head; }
        [Serializable] public class Gaze { public Target target; }
        [Serializable] public class Target { public string id; public float[] pos; }
        [Serializable] public class Head { public float[] pos; public ViewPlane viewPlane; }
        [Serializable] public class ViewPlane { public string[] axes; }
        [Serializable] public class DataComposer { public Feedforward feedforward; }
        [Serializable] public class Feedforward { public Candidates candidates; }
        [Serializable] public class Candidates { public C1 c1; }
        [Serializable] public class C1 { public int[] element_ids; public CenterId center_id; public Filter filter; public Navigate navigate; public Bin bin; public string insight; }
        [Serializable] public class CenterId { public float[] pos; }
        [Serializable] public class Filter { public string[] x, y, z, type; }
        [Serializable] public class Navigate { public NewViewPlane newViewPlane; public NewField newField; }
        [Serializable] public class NewViewPlane { public string[] axes; }
        [Serializable] public class NewField { public string x, y, z; }
        [Serializable] public class BinAxis { public bool enable; public int? step; public float[] edges; }
        [Serializable] public class Bin { public BinAxis x, y, z; }

    }
}
