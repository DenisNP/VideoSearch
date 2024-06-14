import torch
import torchaudio
import requests
import tempfile
import os
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from nemo.collections.asr.models import EncDecRNNTBPEModel

from nemo.collections.asr.modules.audio_preprocessing import (
    AudioToMelSpectrogramPreprocessor as NeMoAudioToMelSpectrogramPreprocessor,
)
from nemo.collections.asr.parts.preprocessing.features import (
    FilterbankFeaturesTA as NeMoFilterbankFeaturesTA,
)


app = FastAPI()

class TranscribeRequest(BaseModel):
    url: str

class TranscribeResponse(BaseModel):
    result: str = None
    error: str = None

# Load model
device = "cuda" if torch.cuda.is_available() else "cpu"
model = EncDecRNNTBPEModel.from_config_file("./rnnt_model_config.yaml")
ckpt = torch.load("./rnnt_model_weights.ckpt", map_location="cpu")
model.load_state_dict(ckpt, strict=False)
model.eval()
model = model.to(device)

@app.post("/transcribe", response_model=TranscribeResponse)
async def transcribe_audio(request: TranscribeRequest):
    try:
        # Download the video
        with tempfile.NamedTemporaryFile(delete=False, suffix=".mp4") as tmp_file:
            video_path = tmp_file.name
            response = requests.get(request.url)
            tmp_file.write(response.content)
        
        # Extract audio
        audio_path = video_path.replace(".mp4", ".wav")
        os.system(f"ffmpeg  -hide_banner -loglevel error -i {video_path} -ac 1 -ar 16000 {audio_path}")

        # Transcribe audio
        result = model.transcribe([audio_path])[0]

        # Clean up
        os.remove(video_path)
        os.remove(audio_path)
        
        return TranscribeResponse(result=result[0])
    except Exception as e:
        return TranscribeResponse(error=str(e))
