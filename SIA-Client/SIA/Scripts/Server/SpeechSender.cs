using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using System.IO;
using SIA.Audio;
using System;
using EmbodiedNLI.Visualization;
using SIA.Visualization;
using UnityEngine.InputSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class SpeechSender : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    public string serverUrl = "Your Server Url Here"; // Set this in the Inspector
    public UITextTester uiTextTester;
    public Inspector Inspector; // Link from Inspector

    private readonly string fileName = "output.wav";
    private readonly int duration = 60;
    private AudioClip clip;
    private InputAction recordAction;
    private bool isRecording;
    public bool IsWaitingServer { get; private set; } // Waiting for server response

    private float time;
    public GazeSelector gazeSelector;

    // Added: Variables related to gaze data capture
    private bool capturingGaze = false;
    private TargetData recordingGazeData = null;
    private string micDevice; // Device name selected in StartRecording
    private Panel_HudUI hudUI;
    // Get Panel_HubText from HUD panel object
    private Panel_HubText hubText;

    private void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        dropdown.options.Add(new TMP_Dropdown.OptionData("Microphone not supported on WebGL"));
#else
        foreach (var device in Microphone.devices)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(device));
        }
        // recordButton.onClick.AddListener(StartRecording);
        dropdown.onValueChanged.AddListener(ChangeMicrophone);

        var index = PlayerPrefs.GetInt("user-mic-device-index");
        dropdown.SetValueWithoutNotify(index);
#endif
        // Auto-connect GazeSelector
        if (gazeSelector == null)
        {
            gazeSelector = FindObjectOfType<GazeSelector>();
            if (gazeSelector != null)
                Debug.Log("[SpeechSender] Automatically found and connected GazeSelector.");
            else
                Debug.LogError("[SpeechSender] Could not find GazeSelector in the scene. Please connect it manually in the Inspector.");
        }

        // Find HUD panel by name (or assign directly if needed)
        var hudPanelGO = GameObject.Find("HUDPanel");
        if (hudPanelGO != null)
        {
            hudUI = hudPanelGO.GetComponent<Panel_HudUI>();
            hubText = hudPanelGO.GetComponent<Panel_HubText>();
        }
        else
        {
            Debug.LogWarning("[SpeechSender] HUDPanel not found in the scene.");
        }

    }

    private void ChangeMicrophone(int index)
    {
        PlayerPrefs.SetInt("user-mic-device-index", index);
    }

    private void OnEnable()
    {
        recordAction = new InputAction(
            type: InputActionType.Button
        );

        // Right-hand trigger
        recordAction.AddBinding("<XRController>{RightHand}/trigger");

        // Left-hand trigger
        recordAction.AddBinding("<XRController>{LeftHand}/trigger");

        recordAction.Enable();
    }


    private void OnDisable()
    {
        recordAction.Disable();
    }
    private void StartRecording()
    {
        isRecording = true;
        time = 0f;

        // Use selected dropdown device (null means default device)
        micDevice = null;
        if (dropdown != null && dropdown.options.Count > 0)
        {
            var idx = Mathf.Clamp(PlayerPrefs.GetInt("user-mic-device-index", 0), 0, dropdown.options.Count - 1);
            micDevice = dropdown.options[idx].text;
        }

    #if !UNITY_WEBGL
        // Stop existing recording on the same device before restarting
        if (Microphone.IsRecording(micDevice)) Microphone.End(micDevice);

        // loop = true; actual stop happens when trigger is released.
        clip = Microphone.Start(micDevice, true, duration, 44100);
    #endif

        capturingGaze = true;
        recordingGazeData = null;

    }


    private void EndRecording()
    {
        Debug.Log("Recording stopped (trigger released)");
        isRecording = false;
        IsWaitingServer = true; // Enter waiting state for server response
        StartCoroutine(FinishRecordingAndSend());
    }

    private IEnumerator FinishRecordingAndSend()
    {
    #if !UNITY_WEBGL
        // 1) Wait for valid samples (some devices can return pos==0 right after Start)
        int pos = 0;
        float safety = 0f;
        while ((pos = Microphone.GetPosition(micDevice)) <= 0 && safety < 1.0f)
        {
            safety += Time.unscaledDeltaTime; // Wait up to 1 second
            yield return null;
        }

        // 2) End recording on the same device
        Microphone.End(micDevice);
    #endif

        if (clip == null)
            yield break;

        // 3) Trim clip to the actual recorded length
        AudioClip trimmed = clip;
    #if !UNITY_WEBGL
        if (pos > 0 && pos < clip.samples)
        {
            float[] full = new float[clip.samples * clip.channels];
            clip.GetData(full, 0);

            float[] cut = new float[pos * clip.channels];
            Array.Copy(full, cut, cut.Length);

            trimmed = AudioClip.Create("trimmed", pos, clip.channels, clip.frequency, false);
            trimmed.SetData(cut, 0);
        }
    #endif

        byte[] wav = SaveWav.Save(fileName, trimmed);
        uiTextTester?.DisplayFileSent("file uploaded");
        StartCoroutine(UploadAudioToServer(wav));
        clip = null;
    }
private void Update()
{
    // Start recording on trigger press
    if (recordAction.WasPressedThisFrame())
    {
        if (!isRecording)
            StartRecording();
        if (hudUI != null)
        {
            hudUI.SetRecordingIcon(true);
            hudUI.SetProgress01(0f);
            hudUI.SetStatus("Ready");
        }
    }

    // Stop recording on trigger release
    if (recordAction.WasReleasedThisFrame())
    {
        if (isRecording)
            EndRecording();
        if (hudUI != null)
        {
            hudUI.SetRecordingIcon(false);
            hudUI.SetProgress01(0f);
            hudUI.SetStatus("Ready");
        }
    }

        // Update recording progress and HUD state
        if (hudUI != null)
        {
            if (isRecording)
            {
                time += Time.deltaTime;
                // Keep progress bar controlled by upload/process stages
                hudUI.SetStatus("Recording...");
                if (time >= duration)
                {
                    time = 0;
                    isRecording = false;
                    EndRecording();
                }
            }
            else if (IsWaitingServer)
            {
                // During upload/wait, progress is controlled in UploadAudioToServer
                hudUI.SetStatus("Uploading...");
            }
            else
            {
                // hudUI.SetProgress01(0f);
                hudUI.SetStatus("Ready");
            }
    }

    if (capturingGaze && gazeSelector != null)
    {
        CaptureGaze();
    }
}
    
    private void CaptureGaze()
    {
        string gazeJson = gazeSelector.GetCurrentGazeJson();
        if (!string.IsNullOrEmpty(gazeJson))
        {
            try
            {
                string jsonContent = gazeJson.Substring(gazeJson.IndexOf('{'));
                int endBraceIndex = jsonContent.LastIndexOf('}');
                if (endBraceIndex >= 0)
                    jsonContent = jsonContent.Substring(0, endBraceIndex + 1);

                TargetData currentGaze = JsonUtility.FromJson<TargetData>(jsonContent);
                if (currentGaze?.head?.viewPlane != null)
                {
                    currentGaze.head.plane = currentGaze.head.viewPlane.planeName;
                    currentGaze.head.planePos = currentGaze.head.viewPlane.planePos;
                }

                if (currentGaze != null && currentGaze.GAZE != null &&
                    !string.IsNullOrEmpty(currentGaze.GAZE.id) && currentGaze.GAZE.id != "-1")
                {
                    recordingGazeData = currentGaze;
                    Debug.Log($"[Recording] Saved gaze data - ID: {currentGaze.GAZE.id}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Recording] Gaze processing error: {e.Message}");
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Server communications
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////
    IEnumerator UploadAudioToServer(byte[] audioBytes)
    {
        string url = serverUrl + "/transcribe";
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", audioBytes, "recorded.wav", "audio/wav");

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            www.SetRequestHeader("Accept", "application/json");

            // Initial state
            IsWaitingServer = true;
            hudUI?.SetRecordingIcon(false);
            hudUI?.SetStatus("Uploading...");

            // Reset progress to minimum
            hudUI?.SetProgress01(0f);

            var op = www.SendWebRequest();
            yield return op;

            IsWaitingServer = false;

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Whisper request failed: " + www.error);
                uiTextTester?.DisplayParsedChatResult("Whisper error");
                hudUI?.SetStatus("Upload failed");

                // Reset progress even on failure.
                hudUI?.SetProgress01(0f);
                yield break;
            }

            // Completed transcribe upload
            hudUI?.SetProgress01(0f);
            hudUI?.SetStatus("Transcribed");

            var tr = JsonUtility.FromJson<TranscribeResult>(www.downloadHandler.text);
            uiTextTester?.DisplayInput(tr.transcript);
            hubText?.DisplayUserSpeech(tr.transcript);

            // Next stage
            hudUI?.SetStatus("Processing...");
            // hudUI?.SetProgress01(0.1f);
            
            // Animate from 0.1f to 0.95f while waiting for process response
            StartCoroutine(AnimateProgress(hudUI, 0.1f, 0.95f, 30f));

            StartCoroutine(SendToChat(tr.transcript));
        }
    }

    private IEnumerator AnimateProgress(Panel_HudUI hud, float from, float to, float duration)
    {
        if (hud == null) yield break;
        float t = 0f;
        hud.SetProgress01(from);

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Lerp(from, to, t / duration);
            hud.SetProgress01(p);
            yield return null;
        }

        hud.SetProgress01(to); // Final correction
    }

    IEnumerator SendToChat(string text)
    {
        string chatUrl = serverUrl + "/process";


        // 1. Use stored meaningful gaze data captured during recording
        TargetData targetData = recordingGazeData;

        // 2. If recorded gaze data exists, use it
        if (targetData != null && targetData.GAZE != null && targetData.GAZE.id != "-1")
        {
            Debug.Log($"[SendToChat] Using stored gaze data from recording. ID={targetData.GAZE.id}");
        }
        // 3. If no recorded data, use current gaze data
        else if (gazeSelector != null)
        {
            Debug.Log("[SendToChat] No stored gaze data from recording. Using current gaze data.");

            string gazeJson = gazeSelector.GetCurrentGazeJson();

            if (!string.IsNullOrEmpty(gazeJson))
            {
                try
                {
                    // Extract JSON object part only
                    string jsonContent = gazeJson.Substring(gazeJson.IndexOf('{'));
                    int endBraceIndex = jsonContent.LastIndexOf('}');
                    if (endBraceIndex >= 0)
                    {
                        jsonContent = jsonContent.Substring(0, endBraceIndex + 1);
                    }

                    // Parse directly into TargetData
                    targetData = JsonUtility.FromJson<TargetData>(jsonContent);
                    
                    // Sync legacy fields from viewPlane (targetData path)
                    if (targetData?.head?.viewPlane != null) {
                        targetData.head.plane   = targetData.head.viewPlane.planeName;
                        targetData.head.planePos = targetData.head.viewPlane.planePos;
                    }

                    Debug.Log($"[SendToChat] Current gaze data - GAZE ID: {targetData.GAZE?.id}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SendToChat] Gaze JSON parsing error: {e.Message}");
                }
            }
        }

        // 3-1) Build chart spec dynamically
        var chartSpecComp = FindObjectOfType<chartSpec_app>();
        if (chartSpecComp == null)
        {
            Debug.LogError("[SendToChat] chartSpec_app component not found in scene.");
            yield break;
        }
        JObject chartSpecObj = JObject.Parse(chartSpecComp.GetScatterplotSpec());

        // 3) Build final payload before send
        var payload = new {
            text = text,
            target = targetData,
            chartSpec = chartSpecObj
        };

        // Serialize payload
        string jsonPayload = JsonConvert.SerializeObject(payload);
        Debug.Log($"[SendToChat] Sending request to server. payloadLength={jsonPayload.Length}");

        // 5. Reset recordingGazeData for the next recording
        recordingGazeData = null;

        using (UnityWebRequest req = new UnityWebRequest(chatUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            string Fulldata = req.downloadHandler.text;

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("GPT request failed: " + req.error);
                Debug.LogError("GPT request failed. Response body omitted for security.");
                if (uiTextTester != null)
                    uiTextTester.DisplayParsedChatResult("GPT error");

            }
            else
            {
                Debug.Log($"GPT response OK. responseLength={Fulldata?.Length ?? 0}");


                // Handle NULL or empty response
                if (string.IsNullOrEmpty(Fulldata) || Fulldata.Trim().Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning("Received NULL or empty response from server.");

                    if (uiTextTester != null)
                    {
                        uiTextTester.DisplayInput("No response from server.");
                    }

                    yield break;
                }

                hudUI?.SetProgress01(1f);

                if (uiTextTester == null)
                {
                    Debug.LogError("SpeechSender: uiTextTester is null! Please connect it in the Inspector.");
                }
                else
                {
                    uiTextTester.DisplayParsedChatResult(Fulldata);
                }

                // Send JSON to Inspector
                if (Inspector != null)
                    Inspector.ASTparsor(Fulldata);

                // Apply new chart spec from server response
                try
                {
                    JObject fullObj = JObject.Parse(Fulldata);
                    JToken chartSpecFromServer = fullObj["chart_spec"];
                    
                    if (chartSpecFromServer != null)
                    {
                        // Handle both direct spec and array format
                        JObject newChartSpec = chartSpecFromServer is JArray arr && arr.Count > 1 
                            ? (JObject)arr[1] 
                            : (JObject)chartSpecFromServer;
                        
                        if (newChartSpec != null && chartSpecComp != null)
                        {
                            Debug.Log("[SpeechSender] Applying new chart spec from server.");
                            chartSpecComp.ApplyChartSpec(newChartSpec);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("[SpeechSender] Failed to apply chart spec: " + e.Message);
                }

                // Save current chart spec
                if (chartSpecComp != null)
                {
                    string specJson = chartSpecComp.GetScatterplotSpec();

                    // Refresh legend from response chart_spec if available
                   if (hudUI != null)
                    {
                        try
                        {
                            JObject fullObj = JObject.Parse(Fulldata);

                            // Extract chart_spec
                            JToken chartSpecTok = fullObj["chart_spec"];
                            if (chartSpecTok is JArray arr && arr.Count > 1)
                            {
                                // The second element is the concrete spec
                                chartSpecTok = arr[1];
                            }

                            if (chartSpecTok != null)
                            {
                                Debug.Log("[SpeechSender] chartSpec found in server response.");
                                hudUI.RefreshLegendFromJson(chartSpecTok.ToString());
                            }
                            else
                            {
                                Debug.LogWarning("[SpeechSender] chartSpec not found in server response.");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("[SpeechSender] Failed to parse server response JSON: " + e.Message);
                        }
                    }

                    chartSpecComp.SaveScatterplotSpec("chartSpec_app.json");
                    Debug.Log("[SpeechSender] Saved chartSpec after server response.");

                    StartCoroutine(UploadSavedChartSpec());
                }    
            }
            if (hubText != null)
            {
                hubText.DisplayFeedForwardInsight(Fulldata);
            }
        }
    }
    private IEnumerator UploadSavedChartSpec()
    {
        string filePath = Path.Combine(Application.dataPath,
            "SIA/Scripts/Visualization/SpecCompiler/chartSpec_app.json");

        if (!File.Exists(filePath))
        {
            Debug.LogWarning("[SpeechSender] chartSpec_app.json not found. Skipping upload.");
            yield break;
        }

        string chartSpecJson = File.ReadAllText(filePath);

        // Wrap chartSpec only
        var payload = new {
            chartSpec = JsonConvert.DeserializeObject(chartSpecJson)
        };

        string json = JsonConvert.SerializeObject(payload);

        string url = serverUrl + "/updateChartSpec";

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[SpeechSender] UploadSavedChartSpec failed: " + req.error);
                Debug.LogError("[SpeechSender] UploadSavedChartSpec response body omitted for security.");
            }
            else
            {
                Debug.Log("[SpeechSender] Uploaded saved chartSpec to server.");
            }
        }
    }

    [System.Serializable]
    private class TranscribeResult
    {
        public string transcript;
    }


    [System.Serializable]
    private class TextPayload
    {
        public string text;
        public TargetData target;
        public JObject chartSpec;

        public TextPayload(string t, TargetData targetData = null, JObject spec = null)
        {
            text = t;
            target = targetData;
            chartSpec = spec;
        }
    }


    [System.Serializable]
    private class NearbyPoint
    {
        public int id;
        public int pid;
        public float[] pos;
        public float distance;
    }

    [System.Serializable]
    private class TargetData
    {
        public GazeInfo GAZE;
        public HeadInfo head;
        public NearbyPoint[] nearby;
    }

    [System.Serializable]
    private class GazeInfo
    {
        public string id;
        public string pid;
        public float[] pos;
    }

    [System.Serializable]
    private class HeadInfo
    {
        public float[] pos;
        public string plane;
        public float[] planePos;
        public ViewPlane viewPlane;
    }

    [System.Serializable]
    private class GazeWrapper
    {
        public TargetData target;
    }
    
    [Serializable] private class ViewPlane
    {
        public string[] axesFields;
        public string planeName;
        public float distance;
        public float[] planePos;
    }
}