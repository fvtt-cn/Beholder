# Beholder
File System Watcher that notifies CDN that Caches should be refreshed/preloaded.

[English README](README.md) | [中文说明](README_CN.md)

## Usage (for Foundry VTT)
*Requires Docker and [foundryvtt-docker](https://github.com/felddy/foundryvtt-docker)*

1. Start up **foundryvtt-docker** container if not done yet
2. Configure Beholder with your own `appsettings.json`
    - If you don't know how to make your own config file, please check `appsettings.json` in the repo
    - Please check if `Spectator.Directory` and `Spectator.TrimStart` is valid (e.g. `/data/Data/` and `/data/Data/` if you plan to mount FVTT data volume as `/data` later)
3. Start up **beholder** container with volumes and configs
    ```console
    docker run -itd \
    --name=beholder_data \
    --restart=unless-stopped \
    -v <your_fvtt_data_dir>:/data \
    -v $PWD/appsettings.json:/app/appsettings.Production.json \
    hmqgg/beholder:latest
    ```