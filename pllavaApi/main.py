import logging
import os
import tempfile
import requests
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import Optional

import torch
import torchvision
from PIL import Image
import numpy as np
from decord import VideoReader, cpu

from .utils.model_utils import load_pllava, pllava_answer
from .utils.eval_utils import ChatPllava, conv_templates

app = FastAPI()

# Set up logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class VideoDescriptionRequest(BaseModel):
    url: str
    prompt: Optional[str] = "Provide a list of keywords describing this video"

class VideoDescriptionResponse(BaseModel):
    result: Optional[str] = None
    error: Optional[str] = None

# ========================================
#             Model Initialization
# ========================================
def init_model():
    logger.info('Initializing PLLaVA')
    model, processor = load_pllava(
                            repo_id="MODELS/pllava-7b",
                            num_frames=4,
                            use_lora=True,
                            weight_dir="MODELS/pllava-7b",
                            lora_alpha=4,
                            pooling_shape=(16,12,12),
                            use_multi_gpus=False)

    model = model.to(torch.device(0))
    model = model.eval()

    logger.info('Model and processor initialized and moved to CUDA')
    return model, processor

RESOLUTION = 360 #
model, processor = init_model()
chat = ChatPllava(model, processor)
INIT_CONVERSATION = conv_templates["plain"]

SYSTEM = """You are Pllava, a large vision-language assistant. 
You are able to understand the video content that the user provides, and assist the user with a variety of tasks using natural language.
Follow the instructions carefully and explain your answers in detail based on the provided video.
"""

################

def get_index(num_frames, num_segments):
    seg_size = float(num_frames - 1) / num_segments
    start = int(seg_size / 2)
    offsets = np.array([
        start + int(np.round(seg_size * idx)) for idx in range(num_segments)
    ])
    return offsets

def load_video(video_path, num_segments=8, return_msg=False, num_frames=4, resolution=336):
    transforms = torchvision.transforms.Resize(size=resolution)
    vr = VideoReader(video_path, ctx=cpu(0), num_threads=1)
    num_frames = len(vr)
    frame_indices = get_index(num_frames, num_segments)
    images_group = list()
    for frame_index in frame_indices:
        img = Image.fromarray(vr[frame_index].asnumpy())
        images_group.append(transforms(img))
    if return_msg:
        fps = float(vr.get_avg_fps())
        sec = ", ".join([str(round(f / fps, 1)) for f in frame_indices])
        # " " should be added in the start and end
        msg = f"The video contains {len(frame_indices)} frames sampled at {sec} seconds."
        return images_group, msg
    else:
        return images_group
        
def infer(model, processor, vid_path, num_frames=4, conv_mode="plain", prompt="Provide a list of keywords describing this video"):
    
    if num_frames != 0:
        vid, msg = load_video(vid_path, num_segments=num_frames, return_msg=True, resolution=RESOLUTION)
    else:
        vid, msg = None, 'num_frames is 0, not inputing image'
    img_list = vid
    conv = conv_templates[conv_mode].copy()
    conv.user_query(prompt, is_mm=True)
    llm_response, conv = pllava_answer(conv=conv,
                                       model=model,
                                       processor=processor,
                                       do_sample=False,
                                       img_list=img_list,
                                       max_new_tokens=256,
                                       print_res=False)
    
    return llm_response, conv

#####################

@app.post("/describe", response_model=VideoDescriptionResponse)
async def describe_video(request: VideoDescriptionRequest):
    try:
        logger.info(f'Received request for URL: {request.url}')
        
        # Download the video
        with tempfile.NamedTemporaryFile(delete=False) as tmp_file:
            video_path = tmp_file.name
            response = requests.get(request.url)
            tmp_file.write(response.content)
        
        # Process the video
        response = infer(model,
                        processor,
                        video_path,
                        num_frames=4,
                        conv_mode="plain",
                        prompt=request.prompt)
        
        # Delete the video
        os.remove(video_path)
        
        logger.info(response)
        
        logger.info('Video described successfully')
        return VideoDescriptionResponse(result=response[0])
    except Exception as e:
        logger.error(f'Error describing video: {str(e)}')
        return VideoDescriptionResponse(error=str(e))