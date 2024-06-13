import copy
import itertools
import re
import os
import json
from enum import auto, Enum
import dataclasses
from typing import Any, List

from PIL import Image
import cv2
import imageio
import numpy as np
import torch
from torch.utils.data import Dataset
import torchvision.transforms as T
from torchvision.transforms.functional import InterpolationMode
from moviepy.editor import VideoFileClip


from decord import VideoReader, cpu # This is Terrible, if you have this line of import in front of torch, will cause model.to(device) to hang
from transformers import StoppingCriteria, StoppingCriteriaList
from transformers import AutoProcessor, AutoModelForZeroShotObjectDetection

import logging
# Set up logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


IMAGE_TOKEN = "<image>"
device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')

class EasyDict(dict):
    """
    Get attributes

    >>> d = EasyDict({'foo':3})
    >>> d['foo']
    3
    >>> d.foo
    3
    >>> d.bar
    Traceback (most recent call last):
    ...
    AttributeError: 'EasyDict' object has no attribute 'bar'

    Works recursively

    >>> d = EasyDict({'foo':3, 'bar':{'x':1, 'y':2}})
    >>> isinstance(d.bar, dict)
    True
    >>> d.bar.x
    1

    Bullet-proof

    >>> EasyDict({})
    {}
    >>> EasyDict(d={})
    {}
    >>> EasyDict(None)
    {}
    >>> d = {'a': 1}
    >>> EasyDict(**d)
    {'a': 1}

    Set attributes

    >>> d = EasyDict()
    >>> d.foo = 3
    >>> d.foo
    3
    >>> d.bar = {'prop': 'value'}
    >>> d.bar.prop
    'value'
    >>> d
    {'foo': 3, 'bar': {'prop': 'value'}}
    >>> d.bar.prop = 'newer'
    >>> d.bar.prop
    'newer'


    Values extraction

    >>> d = EasyDict({'foo':0, 'bar':[{'x':1, 'y':2}, {'x':3, 'y':4}]})
    >>> isinstance(d.bar, list)
    True
    >>> from operator import attrgetter
    >>> map(attrgetter('x'), d.bar)
    [1, 3]
    >>> map(attrgetter('y'), d.bar)
    [2, 4]
    >>> d = EasyDict()
    >>> d.keys()
    []
    >>> d = EasyDict(foo=3, bar=dict(x=1, y=2))
    >>> d.foo
    3
    >>> d.bar.x
    1

    Still like a dict though

    >>> o = EasyDict({'clean':True})
    >>> o.items()
    [('clean', True)]

    And like a class

    >>> class Flower(EasyDict):
    ...     power = 1
    ...
    >>> f = Flower()
    >>> f.power
    1
    >>> f = Flower({'height': 12})
    >>> f.height
    12
    >>> f['power']
    1
    >>> sorted(f.keys())
    ['height', 'power']

    update and pop items
    >>> d = EasyDict(a=1, b='2')
    >>> e = EasyDict(c=3.0, a=9.0)
    >>> d.update(e)
    >>> d.c
    3.0
    >>> d['c']
    3.0
    >>> d.get('c')
    3.0
    >>> d.update(a=4, b=4)
    >>> d.b
    4
    >>> d.pop('a')
    4
    >>> d.a
    Traceback (most recent call last):
    ...
    AttributeError: 'EasyDict' object has no attribute 'a'
    """

    def __init__(self, d=None, **kwargs):
        if d is None:
            d = {}
        if kwargs:
            d.update(**kwargs)
        for k, v in d.items():
            setattr(self, k, v)
        # Class attributes
        for k in self.__class__.__dict__.keys():
            if not (k.startswith("__") and k.endswith("__")) and not k in ("update", "pop"):
                setattr(self, k, getattr(self, k))

    def __setattr__(self, name, value):
        if isinstance(value, (list, tuple)):
            value = [self.__class__(x) if isinstance(x, dict) else x for x in value]
        elif isinstance(value, dict) and not isinstance(value, self.__class__):
            value = self.__class__(value)
        super(EasyDict, self).__setattr__(name, value)
        super(EasyDict, self).__setitem__(name, value)

    __setitem__ = __setattr__

    def update(self, e=None, **f):
        d = e or dict()
        d.update(f)
        for k in d:
            setattr(self, k, d[k])

    def pop(self, k, d=None):
        if hasattr(self, k):
            delattr(self, k)
        return super(EasyDict, self).pop(k, d)


if __name__ == "__main__":
    import doctest

class SeparatorStyle(Enum):
    """Different separator style."""
    SINGLE = auto()
    TWO = auto()
    MPT = auto()

class MultiModalConvStyle(Enum):
    """Different separator style."""
    MM_ALONE = 'mm_alone'
    MM_INTERLEAF = 'mm_inferleaf'

def dump_json(obj_serializable ,save_dir_path, json_file_name):
    os.makedirs(save_dir_path, exist_ok=True)
    save_path = os.path.join(save_dir_path, json_file_name)
    with open(save_path, 'w', encoding='utf-8') as f:
        json.dump(obj_serializable, f, indent=4, ensure_ascii=False, )

def load_json(load_dir_path, json_file_name):
    
    load_path = os.path.join(load_dir_path, json_file_name)
    if not os.path.exists(load_path):
        return None
    with open(load_path, 'r', encoding='utf-8') as f:
        obj_serializable = json.load(f)
    return obj_serializable



@dataclasses.dataclass
class Conversation(EasyDict):
    """A class that keeps all conversation history."""
    system: str
    roles: List[str]
    messages: List[List[str]]
    sep: List[str]
    mm_token: str
    
    mm_style: MultiModalConvStyle = MultiModalConvStyle.MM_INTERLEAF
    pre_query_prompt: str=None
    post_query_prompt: str=None
    answer_prompt: str=None

    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        if isinstance(self.sep, str):
            self.sep = [self.sep for _ in self.roles]

    def get_prompt(self):
        sep = [self.sep for _ in self.roles] if isinstance(self.sep, str) else self.sep  # if only one sep given, then both sep are the sames
        sep = dict(zip(self.roles, sep))
        ret = self.system + sep[self.roles[0]] if self.system != "" else ""
        for i, (role, message) in enumerate(self.messages):
            # if is last msg(the prompt for assistant), if answer prompt exists, no sep added
            if i+1 == len(self.messages):
                if role != self.roles[-1]: # last role is not the model
                    ret += role + message + sep[role] + self.roles[-1]
                else:
                    ret += role + message
            else:
                ret += role + message + sep[role]
        return ret
    # def get_prompt_multichoice(self):
    #     pass
    def user_query(self, query=None, pre_query_prompt=None, post_query_prompt=None, is_mm=False, num_mm_token=1):
        if post_query_prompt is not None:
            query = f"{query} {post_query_prompt}"

        if pre_query_prompt is not None:
            query = f"{pre_query_prompt} {query}"
        role = self.roles[0]
        # TODO: remove the num_mm_token and hack the self.mm_token outside
        if is_mm:
            mm_str = num_mm_token*self.mm_token[:-1] + self.mm_token[-1]
            if self.mm_style == MultiModalConvStyle.MM_ALONE:
                self._append_message(role, mm_str)
            elif self.mm_style == MultiModalConvStyle.MM_INTERLEAF:
                if self.mm_token not in query:
                    query = f'{mm_str} {query}'
        self._append_message(role, query)
    
    def assistant_response(self, response, pre_query_prompt=None, post_query_prompt=None):
        if post_query_prompt is not None:
            response = f"{response} {post_query_prompt}"

        if pre_query_prompt is not None:
            response = f"{post_query_prompt} {response}"

        role = self.roles[1]
        self._append_message(role, response)
    
    def _append_message(self, role, message):
        message = '' if message is None else message
        self.messages.append([role, message])

    def copy(self):
        return copy.deepcopy(self)

conv_video_chatgpt_v1 = Conversation(
    system="You are Video-ChatGPT, a large vision-language assistant. "
           "You are able to understand the video content that the user provides, and assist the user with a variety of tasks using natural language."
           "Follow the instructions carefully and explain your answers in detail based on the provided video.",
    roles=("USER:", "ASSISTANT:"),
    messages=[],
    sep=[" ","</s>"],
    mm_token='<image>',
    mm_style=MultiModalConvStyle.MM_INTERLEAF,
)


conv_plain_v1 = Conversation(
    system="""You are Pllava, a large vision-language assistant. 
You are able to understand the video content that the user provides, and assist the user with a variety of tasks using natural language.
Follow the instructions carefully and explain your answers in detail based on the provided video.
""",
    roles=("USER:", "ASSISTANT:"),
    messages=[],
    sep=(" ", "</s>"),
    mm_token='<image>'
)

conv_templates = {
    "plain": conv_plain_v1,
}

class ChatPllava:
    print_res=True
    do_sample=False
    def __init__(self, model, processor):
        self.model = model
        self.processor = processor

    def ask(self, text, conv: Conversation, system):
        conv.system = system
        conv.user_query(text, )
        return conv

    def answer(self, conv: Conversation, img_list, max_new_tokens=200, num_beams=1, min_length=1, top_p=0.9,
               repetition_penalty=1.0, length_penalty=1, temperature=1.0):
        torch.cuda.empty_cache()
        prompt = conv.get_prompt()
        if prompt.count(conv.mm_token) < len(img_list):
            diff_mm_num = len(img_list) - prompt.count(conv.mm_token)
            for i in range(diff_mm_num):
                conv.user_query("", is_mm=True)
            prompt = conv.get_prompt()
            
        inputs = self.processor(text=prompt, images=img_list, return_tensors="pt")
        if inputs['pixel_values'] is None:
            inputs.pop('pixel_values')
        inputs = inputs.to(self.model.device)

        with torch.no_grad():
            output_token = self.model.generate(**inputs, media_type='video',
                                        do_sample=self.do_sample,max_new_tokens=max_new_tokens, num_beams=num_beams, min_length=min_length, 
                                        top_p=top_p, repetition_penalty=repetition_penalty, length_penalty=length_penalty, temperature=temperature,
                                        ) # dont need to long for the choice.
            output_text = self.processor.batch_decode(output_token, skip_special_tokens=True, clean_up_tokenization_spaces=False)[0]

        if self.print_res:
            logger.info('###PROMPT: ', prompt)
            logger.info('###LM OUTPUT TEXT', output_text)
        # <|im_start|> encode and then decode would extend a space at folloing, this is insane...
        if conv.roles[-1] == "<|im_start|>assistant\n":
            split_tag = "<|im_start|> assistant\n"
        else:
            split_tag = conv.roles[-1]
        output_text = output_text.split(split_tag)[-1].rstrip(conv.sep[1])
        conv.assistant_response(output_text)
        return output_text, output_token.cpu().numpy(), conv
    
        
    def get_index(self, num_frames, num_segments):
        seg_size = float(num_frames - 1) / num_segments
        start = int(seg_size / 2)
        offsets = np.array([
            start + int(np.round(seg_size * idx)) for idx in range(num_segments)
        ])
        return offsets

    def load_video(self, video_path, num_segments=8, return_msg=False):
        vr = VideoReader(video_path, ctx=cpu(0))
        num_frames = len(vr)
        frame_indices = self.get_index(num_frames, num_segments)
        
        duration = len(vr) // vr.get_avg_fps()
        index = np.linspace(0, len(vr)-1, num=int(duration))
        buffer = vr.get_batch(index).asnumpy()
        # transform
        
        images_group = list()
        for frame in buffer:
            img = Image.fromarray(frame)
            images_group.append(img)
        images_group = list()
        for frame_index in frame_indices:
            img = Image.fromarray(vr[frame_index].asnumpy())
            images_group.append(img)
        if return_msg:
            fps = float(vr.get_avg_fps())
            sec = ", ".join([str(round(f / fps, 1)) for f in frame_indices])
            # " " should be added in the start and end
            msg = f"The video contains {len(frame_indices)} frames sampled at {sec} seconds."
            return images_group, msg
        else:
            return images_group

    def upload_video(self, image, conv: Conversation, img_list: list[list], num_segments=None):
        num_segments = self.model.config.num_frames if num_segments is None else num_segments 
        if isinstance(image, str):  # is a image path
            vid, msg = self.load_video(image, num_segments=num_segments, return_msg=True)
        else:
            raise NotImplementedError
        print("Input video shape:", len(vid), *vid[0].size)
        img_list.append(vid)
        conv.user_query("", is_mm=True)
        msg = "Received."
        # self.conv.append_message(self.conv.roles[1], msg)
        return msg, img_list, conv
    
    def upload_img(self, image, conv, img_list):
        assert False
        img = image#Image.open(image)#.convert('RGB')
        transform = T.Compose(
            [
                T.Resize(
                    (224, 224), interpolation=InterpolationMode.BICUBIC
                ),
                T.ToTensor(),
                T.Normalize((0.48145466, 0.4578275, 0.40821073), (0.26862954, 0.26130258, 0.27577711)),
            ]
        )

        img = transform(img).unsqueeze(0).unsqueeze(0).cuda()
        image_emb, _ = self.model.encode_img(img, "Observe the image and answer the question.")
        img_list.append(image_emb)
        conv.messages.append([
            conv.roles[0],
            f"<Image><ImageHere></Image>\n"
        ])
        msg = "Received."
        # self.conv.append_message(self.conv.roles[1], msg)
        return msg,img_list, conv

class StoppingCriteriaSub(StoppingCriteria):
    def __init__(self, stops=[], encounters=1):
        super().__init__()
        self.stops = stops
    def __call__(self, input_ids: torch.LongTensor, scores: torch.FloatTensor):
        for stop in self.stops:
            if torch.all((stop == input_ids[0][-len(stop):])).item():
                return True
        return False
