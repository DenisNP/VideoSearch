# Set the Git repository URL
$GIT_REPO_URL = "https://huggingface.co/ermu2001/pllava-7b.git"

# Create MODELS directory if it doesn't exist
New-Item -ItemType Directory -Path MODELS -ErrorAction SilentlyContinue

# Clone the Git repository using Git LFS
git lfs clone $GIT_REPO_URL MODELS

# Change directory to MODELS
Set-Location -Path MODELS

# Checkout the main branch
git checkout main

# Print success message
Write-Output "Git PLLaVA-7B downloaded successfully to MODELS directory."