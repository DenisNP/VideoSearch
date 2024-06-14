FROM nvidia/cuda:12.1.1-cudnn8-runtime-ubuntu22.04

ARG DEBIAN_FRONTEND=noninteractive

ENV PYTHONUNBUFFERED=1

RUN apt-get update && apt-get install --no-install-recommends -y \
    build-essential \
    python3.10 \
    python3-dev \
    python3-pip \
    git \
    git-lfs \
    wget \
    ffmpeg \
    nano \
    gcc \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

RUN useradd -m -u 1000 user

ENV HOME=/home/user \
    PATH=/home/user/.local/bin:$PATH \
    PYTHONPATH=$HOME/app

USER user
WORKDIR $HOME/app

RUN GIT_REPO_URL="https://huggingface.co/ermu2001/pllava-7b.git" \
    && mkdir -p $HOME/app/MODELS \
    && git lfs install \
    && git clone $GIT_REPO_URL $HOME/app/MODELS/pllava-7b \
    && rm -rf $HOME/app/MODELS/pllava-7b/.git \
    && echo "Git PLLaVA-7B models downloaded successfully to MODELS/pllava-7b directory."

COPY --chown=user . $HOME/app

COPY ./requirements.txt /code/requirements.txt
RUN pip3 install --no-cache-dir --upgrade -r /code/requirements.txt

CMD ["fastapi", "run", "main.py", "--proxy-headers", "--port", "80"]