# Set the Git repository URL
$GIT_REPO_URL = "https://huggingface.co/ermu2001/pllava-7b.git"

# Create MODELS directory if it doesn't exist
New-Item -ItemType Directory -Path MODELS -ErrorAction SilentlyContinue

# Clone the Git repository using Git LFS to MODELS\pllava-7b
git lfs clone $GIT_REPO_URL MODELS\pllava-7b

# Remove the .git folder after cloning
Remove-Item -Path MODELS\pllava-7b\.git -Force -Recurse

# Print success message
Write-Output "Git PLLaVA-7B downloaded successfully to MODELS\pllava-7b directory."