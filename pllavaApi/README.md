1. Install `git lfs` on your OS
2. Activate it in `pllavaApi` folder via `git lfs install`
3. Run `[text](get-model.sh)` to download PLLaVA-7B model from https://huggingface.co/ermu2001/pllava-7b using `git lfs`.
4. Run `docker build -t pllava-api .`
5. Always use container with `--gpus=all`