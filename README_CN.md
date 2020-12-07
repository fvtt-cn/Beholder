# Beholder
提醒 CDN 需要刷新/预热缓存的文件系统监视器。

[English README](README.md) | [中文说明](README_CN.md)

## 使用 (配合 Foundry VTT)
*需要 Docker 和 [foundryvtt-docker](https://github.com/felddy/foundryvtt-docker)*

1. 如果还未启动，先启动 **foundryvtt-docker** 容器（可以使用 [FoundryDeploy 部署脚本](https://github.com/fvtt-cn/FoundryDeploy)）
2. 配置自己的 `appsettings.json`
    - 如果你不知道该如何写配置文件，可以查看仓库中的 `appsettings.json`
    - 请检查 `Spectator.Directory` 和 `Spectator.TrimStart` 是否有效 （比如准备挂在 FVTT 数据卷到 `/data` 时，配置为 `/data/Data/` 和 `/data/Data/`）
3. 使用挂载卷和配置文件，后台启动 **beholder** 容器
    ```console
    docker run -itd \
    --name=beholder_data \
    --restart=unless-stopped \
    -v <your_fvtt_data_dir>:/data \
    -v $PWD/appsettings.json:/app/appsettings.Production.json \
    hmqgg/beholder:latest
    ```