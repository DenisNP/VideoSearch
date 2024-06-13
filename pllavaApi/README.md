1. Run `[text](get-model.sh)` to download PLLaVA-7B model from https://huggingface.co/ermu2001/pllava-7b using `git lfs`.
2. Run `docker build -t pllava-api .`
3. Always use container with `--gpus=all`