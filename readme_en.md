# Kori Miyohashi: Telegram Channel Submission Bot

> [!NOTE]  
> Kori Miyohashi (聖代橋氷織) is a character from the game [Sugar Sweet Temptation](https://store.steampowered.com/app/2374590/Sugar_Sweet_Temptation) by Recette and its derivative works.

## Quick Start

Running with Docker

```bash
docker run -d --name kori-miyohashi \
    -e TG_TOKEN=TOKEN \
    -e WORK_GROUP=YOUR_GROUP_ID \
    -e CHANNEL_ID=YOUR_CHANNEL_ID \
    -e CHANNEL_LINK=HANNEL_LINK \
    -e GROUP_LINK=GROUP_LINK \
    akizon77/kori_miyohashi:latest
```

View logs

```bash
docker logs -f kori-miyohashi
```

## Parameter Explanation

- **DEBUG**: Determines whether to enable debug mode. `true` enables debug mode, allowing detailed logging for troubleshooting.

- **DB_TYPE**: Sets the database type. Supports `sqlite` and `mysql`.

- **DB_CONNECTION_STRING**: The database connection string. Typically not needed when using `sqlite`.

- **DB_FILE**: Specifies the path to the SQLite database file. The default path is `./KoriMiyohashi.db`.

- **USE_PROXY**: Determines whether to use a proxy server. Default `false` means no proxy server is used.

- **PROXY**: Specifies the address of the proxy server. Example: `socks5://127.0.0.1:12612`.

- **TG_TOKEN**: The API Token for the Telegram bot, used for authenticating with the Telegram Bot API.

- **WORK_GROUP**: The ID of the review group. It is a unique identifier for a Telegram group.

- **CHANNEL_ID**: The ID of the Telegram channel, serving as a unique identifier for the channel.

- **CHANNEL_LINK**: The link to the Telegram channel. This link is used to send submissions back to the submitter after approval.

- **GROUP_LINK**: The link to the Telegram group, used during review to locate submissions.

- **OWNER**: The user ID of the top administrator, who holds all permissions.

## Manually Build the Image
### Modify Variables in the Script

Ensure you adjust the following variables according to your needs:

- **IMAGE_NAME**: Set the name of the image. For example, `kori_miyohashi`.
- **REPO_NAME**: Set the name of the repository. It can be a Docker Hub username or a private repository address. An empty string means local builds.
- **TAG**: Set the version tag. For instance, `latest` indicates the newest version.

### Run the Script

In the terminal, execute the following command to build the image:

```bash
bash build-docker.sh
```

## Manual Compilation
> [!NOTE]  
> Requires .NET 8 SDK environment

Restore project dependencies

```bash
dotnet restore
```

Build the project and output to `./builds`

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