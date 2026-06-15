from fastapi import FastAPI, UploadFile, File, HTTPException
from pydantic import BaseModel
import openai
import json
import os
import asyncio
import tempfile
import math
import numpy as np

from dataComposer.data_composer import update_chart_spec
from feedforwardGenerator.feedforward import update_module_spec_feedforward
from speechPatternAnalyzer.embodiment_estimator import update_module_spec_embodiment
from speechPatternAnalyzer.uncertainty_estimator import update_module_spec_uncertainty
from speechPatternAnalyzer.uncertainty_calibration import calibrate_LLMProtoNet
from speechPatternAnalyzer.speech_act_extractor import extract_speech_act
from projector.projector import update_module_spec_projector
from validator.validator import validate_chart_spec, save_after_validation, save_before_validation
import speechPatternAnalyzer.prototypical_network.network as network
from spatialContextResolver.spatial_context import update_module_spec_spatial

######################################################
# <FastAPI server>
######################################################
app = FastAPI()

from fastapi.middleware.cors import CORSMiddleware

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)
######################################################
# </FastAPI server>
######################################################

def _sanitize_json_floats(obj):
    """Recursively replace NaN/Inf float values with None for JSON-safe responses."""
    if isinstance(obj, dict):
        return {k: _sanitize_json_floats(v) for k, v in obj.items()}
    if isinstance(obj, list):
        return [_sanitize_json_floats(v) for v in obj]
    if isinstance(obj, tuple):
        return tuple(_sanitize_json_floats(v) for v in obj)
    if isinstance(obj, np.ndarray):
        return _sanitize_json_floats(obj.tolist())
    if isinstance(obj, np.generic):
        obj = obj.item()
    if isinstance(obj, float):
        if math.isnan(obj) or math.isinf(obj):
            return None
        return obj
    return obj

######################################################
# <OpenAI: get>
######################################################
@app.get("/")
def read_root():
    return {"message": "Hello from OpenAI server!!!$$$$"}

auth_file_path = os.path.join(os.path.expanduser("~"), ".openai", "auth.json")

with open(auth_file_path) as f:
    config = json.load(f)
    api_key = config.get("api_key")

if not api_key:
    raise ValueError("API key is missing in the auth.json file")

openai.api_key = api_key
openai.base_url = "https://us.api.openai.com/v1/"  # added for regional endpoint

class TextWrapper(BaseModel):
    text: str                # utterance text
    target: dict = None      # gaze + head data
    chartSpec: dict = None

async def run_in_thread(func, *args, **kwargs):
    loop = asyncio.get_running_loop()
    return await loop.run_in_executor(None, lambda: func(*args, **kwargs))

async def estimate_embodiment(result_obj, module_spec_path):
    return await run_in_thread(update_module_spec_embodiment, result_obj, module_spec_path)

async def estimate_uncertainty(result_obj, module_spec_path):
    return await run_in_thread(update_module_spec_uncertainty, result_obj, module_spec_path)

######################################################
# </OpenAI: get>
######################################################

######################################################
# <OpenAI: transcribe>
######################################################
# 1. Receive an audio file, transcribe with Whisper, and save debug file
@app.post("/transcribe")
async def transcribe_audio(file: UploadFile = File(...)):
    # 1) Read audio bytes received from client
    audio_bytes = await file.read()

    # 2) Save the raw bytes to debug_upload.wav for debugging
    debug_path = "debug_upload.wav"
    with open(debug_path, "wb") as f:
        f.write(audio_bytes)

    tmp_path = None
    try:
        # 3) Create a NamedTemporaryFile and write audio bytes into it, then get the path (tmp_path).
        #    If delete=False, the file won't be removed after the 'with' block ends.
        with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as tmp:
            tmp.write(audio_bytes)
            tmp.flush()
            tmp_path = tmp.name

        # 4) After closing the temp file, reopen it in 'rb' mode to get an io.IOBase instance.
        with open(tmp_path, "rb") as audio_file:
            # 5) Now file=audio_file (an io.IOBase), so the SDK will recognize it properly.
            transcript = openai.audio.transcriptions.create(
                model="whisper-1",
                file=audio_file,
            )
            transcribed_text = transcript.text

    except Exception as e:
        return {"error": f"Whisper error: {str(e)}"}

    finally:
        # 6) Explicitly delete the temp file created with delete=False
        if tmp_path is not None and os.path.exists(tmp_path):
            os.remove(tmp_path)

    return {"transcript": transcribed_text}

######################################################
# </OpenAI: transcribe>
######################################################

######################################################
# <OpenAI: process>
######################################################
@app.post("/process")
async def process_text(data: TextWrapper):
    try:
        #---------------------------------
        # Data from HMD
        #---------------------------------
        input_text = data.text
        client_data = data.target
        chartSpec_from_client = data.chartSpec

        #---------------------------------
        # Spec file paths
        #---------------------------------
        module_spec_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "moduleSpec.json")
        chart_spec_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "chartSpec.json")

        # Save chartSpec from client
        with open(chart_spec_path, "w", encoding="utf-8") as f:
            json.dump(chartSpec_from_client, f, ensure_ascii=False, indent=2)

        # Load and update moduleSpec with utterance
        with open(module_spec_path, "r", encoding="utf-8") as f:
            module_spec = json.load(f)
        module_spec["speechPatternAnalyzer"]["utterance"] = input_text
        with open(module_spec_path, "w", encoding="utf-8") as f:
            json.dump(module_spec, f, ensure_ascii=False, indent=2)

        #---------------------------------
        # Spatial context (gaze/head tracking)
        #---------------------------------
        if client_data:
            await run_in_thread(update_module_spec_spatial, client_data, module_spec_path)

        #---------------------------------
        # Speech act extraction
        # related module: speechPatternAnalyzer.speech_act_extractor
        #---------------------------------
        speech_act_obj = await run_in_thread(extract_speech_act, input_text, module_spec_path)

        processed_text = {
            "speech_acts": speech_act_obj,
            "utterance": input_text,
            "client_data": client_data
        }

        #---------------------------------
        # Uncertainty + Embodiment + ProtoNet in parallel
        # related modules:
        # - speechPatternAnalyzer.uncertainty_estimator
        # - speechPatternAnalyzer.embodiment_estimator
        # - speechPatternAnalyzer.prototypical_network.network
        # - speechPatternAnalyzer.uncertainty_calibration
        #---------------------------------
        unc_result, emb_result, unc_network_result = await asyncio.gather(
            estimate_uncertainty(processed_text, module_spec_path),
            estimate_embodiment(processed_text, module_spec_path),
            run_in_thread(network.predict_task_and_confidence, input_text, module_spec_path)
        )

        # LLM + network calibration
        calibrated_unc_result = await calibrate_LLMProtoNet(
            unc_result,
            unc_network_result,
            module_spec_path=module_spec_path
        )

        #---------------------------------
        # Projector: classifies intent -> projection_result for downstream modules
        # related module: projector.projector
        #---------------------------------
        projection_result = await run_in_thread(
            update_module_spec_projector,
            emb_result, calibrated_unc_result, processed_text,
            module_spec_path, chart_spec_path
        )

        #---------------------------------
        # Feedforward Generator / Data Composer (sequential to avoid race conditions)
        # related modules:
        # - dataComposer.data_composer
        # - feedforwardGenerator.feedforward
        #---------------------------------
        data_composer_result, is_reset = await run_in_thread(update_chart_spec, chart_spec_path, module_spec_path, projection_result)
        feedforward_result = await update_module_spec_feedforward(projection_result, module_spec_path)

        # Load latest moduleSpec
        with open(module_spec_path, "r", encoding="utf-8") as f:
            latest_module_spec = json.load(f)

        #---------------------------------
        # Chart spec validation
        # related module: validator.validator
        #---------------------------------
        try:
            save_before_validation(data_composer_result)
            result_msg, spec_dict = validate_chart_spec(data_composer_result)
            if spec_dict is None:
                result_msg = "FAILED: Extraction failed, reverted to baseline"
                spec_dict = chartSpec_from_client
            save_after_validation(data_composer_result, spec_dict, result_msg)
        except Exception as e:
            result_msg = f"FAILED: Unexpected error ({str(e)})"
            spec_dict = chartSpec_from_client

        #---------------------------------
        # Return all results to client
        #---------------------------------
        return _sanitize_json_floats({
            "module_spec": latest_module_spec,
            "feedforward_result": feedforward_result,
            "chart_status": result_msg,
            "chart_spec": spec_dict,
            "is_reset": is_reset
        })

    except HTTPException:
        raise
    except Exception as e:
        import traceback
        print(f"Error during OpenAI API call: {e}")
        traceback.print_exc()
        return {"error": str(e)}

#######################
# </OpenAI: process>
#######################

#######################
# <OpenAI: updateChartSpec>
#######################
class ChartSpecWrapper(BaseModel):
    chartSpec: dict

@app.post("/updateChartSpec")
async def update_chart_spec_only(data: ChartSpecWrapper):
    try:
        chartSpec_from_client = data.chartSpec
        chart_spec_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "chartSpec.json")
        with open(chart_spec_path, "w", encoding="utf-8") as f:
            json.dump(chartSpec_from_client, f, ensure_ascii=False, indent=2)
        return {"status": "ok", "message": "ChartSpec updated"}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))
#######################
# </OpenAI: updateChartSpec>
#######################

@app.api_route("/{full_path:path}", methods=["GET", "POST"])
def catch_all(full_path: str):
    return {"message": f"You hit {full_path}"}

# Server run command
# uvicorn main_server:app --reload --port 8000
# uvicorn main_server:app --reload --host 0.0.0.0 --port 8000