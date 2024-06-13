#!/bin/bash

# Set the Git repository URL
GIT_REPO_URL="https://huggingface.co/ermu2001/pllava-7b.git"

# Create MODELS directory if it doesn't exist
mkdir -p MODELS

# Clone the Git repository using Git LFS to MODELS/pllava-7b
git lfs clone $GIT_REPO_URL MODELS/pllava-7b

# Remove the .git folder after cloning
rm -rf MODELS/pllava-7b/.git

# Print success message
echo "Git PLLaVA-7B downloaded successfully to MODELS/pllava-7b directory."