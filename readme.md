# Kori Miyohashi 是 Telegram 频道投稿机器人。

> [!NOTE]  
> Kori Miyohashi(聖代橋氷織) 是 Recette 的游戏 [しゅがてん！-sugarfull tempering-](https://store.steampowered.com/app/2374590/Sugar_Sweet_Temptation/?l=schinese)  及其衍生作品的登场角色。

## 快速开始

使用Docker运行

```bash
docker run -d --name kori-miyohashi \
    -e TG_TOKEN=TOKEN \
    -e WORK_GROUP=YOUR_GROUP_ID \
    -e CHANNEL_ID=YOUR_CHANNEL_ID \
    -e CHANNEL_LINK=HANNEL_LINK \
    -e GROUP_LINK=GROUP_LINK \
    akizon77/kori_miyohashi:latest
```

查看运行日志

```bash
docker logs -f kori-miyohashi
```

## 参数说明

- **DEBUG**: 用于控制是否启用调试模式。`true` 表示开启调试模式，可以输出详细的日志信息，有助于问题排查。

- **DB_TYPE**: 数据库类型设置。支持 `sqlite` 和 `mysql`

- **DB_CONNECTION_STRING**: 数据库连接字符串。在使用 `sqlite` 的情况下，通常不需要设置此参数。

- **DB_FILE**: 指定 SQLite 数据库文件的路径。默认数据库文件路径为 `./KoriMiyohashi.db`。

- **USE_PROXY**: 用于控制是否使用代理服务器。默认 `false` 表示不使用代理服务器。

- **PROXY**: 指定代理服务器的地址。示例 `socks5://127.0.0.1:12612`。

- **TG_TOKEN**: Telegram 机器人的 API Token。用于 Telegram Bot API 的认证。

- **WORK_GROUP**: 审核群的 ID。为一个 Telegram 群组的唯一标识符。

- **CHANNEL_ID**: Telegram 频道的 ID。为一个 Telegram 频道的唯一标识符。

- **CHANNEL_LINK**: Telegram 频道的链接。审核通过后将使用此链接将投稿发送到投稿人。

- **GROUP_LINK**: Telegram 群组的链接。审核时候使用此链接定位稿件。

- **OWNER**: 最高管理员的用户 ID。拥有所有权限。

## 手动构建镜像
### 修改脚本内的变量

确保你根据自己的需求调整以下变量：

- **IMAGE_NAME**: 设定镜像的名称。例如，`kori_miyohashi`。
- **REPO_NAME**: 设定仓库名称，可以是 Docker Hub 用户名或私有仓库地址。为空意味着进行本地构建。
- **TAG**: 设定版本标签。例如，`latest` 表示最新版本。

### 运行脚本

在终端中运行以下命令来执行脚本，从而构建镜像：

```bash
bash build-docker.sh
```

## 手动编译
> [!NOTE]  
> 需要.NET 8 SDK 环境

恢复项目的依赖项
```bash
dotnet restore
```

构建项目，输出到 `./builds`

```bash
dotnet publish -c Release -o ./builds
```

# License

```
Copyright (c) 2024 akizon77

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
```



