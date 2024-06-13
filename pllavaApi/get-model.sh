#!/bin/bash

GIT_REPO_URL="https://huggingface.co/ermu2001/pllava-7b.git"

mkdir -p MODELS
git lfs clone $GIT_REPO_URL MODELS
cd MODELS
git checkout main
echo "Git PLLaVA-7B downloaded successfully to MODELS directory."