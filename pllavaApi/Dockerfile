FROM nvidia/cuda:12.1.1-cudnn8-runtime-ubuntu22.04

ARG DEBIAN_FRONTEND=noninteractive

ENV PYTHONUNBUFFERED=1

# Install required packages
RUN apt-get update && apt-get install --no-install-recommends -y \
    build-essential \
    python3.10 \
    python3-pip \
    git \
    wget \
    ffmpeg \
    nano \
    gcc \
    python3-dev \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Set up a new user named "user" with user ID 1000
RUN useradd -m -u 1000 user

# Set environment variables
ENV HOME=/home/user \
    PATH=/home/user/.local/bin:$PATH \
    PYTHONPATH=$HOME/app

# Switch to the "user" user
USER user

# Set the working directory
WORKDIR $HOME/app

# Copy necessary files to the container
COPY --chown=user ./MODELS $HOME/app/MODELS
COPY --chown=user ./utils $HOME/app/utils
COPY --chown=user ./__init__.py $HOME/app/__init__.py
COPY --chown=user ./main.py $HOME/app/main.py

# Copy requirements and install dependencies
COPY ./requirements.txt /code/requirements.txt
RUN pip3 install --no-cache-dir --upgrade -r /code/requirements.txt

# Define the command to run the application
CMD ["fastapi", "run", "main.py", "--proxy-headers", "--port", "80"]