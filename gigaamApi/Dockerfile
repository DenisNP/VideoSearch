FROM nvcr.io/nvidia/nemo:23.10

RUN mkdir -p /workspace/data
WORKDIR /workspace/data

RUN wget https://n-ws-q0bez.s3pd12.sbercloud.ru/b-ws-q0bez-jpv/GigaAM/{rnnt_model_weights.ckpt,tokenizer_all_sets.tar}
RUN tar -xf tokenizer_all_sets.tar && rm tokenizer_all_sets.tar

RUN pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu118
RUN pip install Cython
RUN pip install git+https://github.com/NVIDIA/NeMo.git@1fa961ba03ab5f8c91b278640e29807079373372#egg=nemo_toolkit[all]
RUN pip install -U soundfile
RUN pip install pyannote.audio==3.2.0
RUN pip install fastapi pydantic keybert[flair]
RUN pip uninstall transformer-engine -y
RUN pip install transformers

COPY . /workspace/data

RUN apt-get update && apt-get install --no-install-recommends -y \
    git \
    git-lfs \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

RUN GIT_REPO_URL="https://huggingface.co/ai-forever/ruBert-base" \
    && mkdir -p /workspace/data/models \
    && git lfs install \
    && git clone $GIT_REPO_URL /workspace/data/models/ruBert-base \
    && rm -rf /workspace/data/models/ruBert-base/.git \
    && echo "Git ruBERT-base models downloaded successfully to models/ruBERT-base directory."

# Define the command to run the application
CMD ["fastapi", "run", "main.py", "--proxy-headers", "--port", "80"]