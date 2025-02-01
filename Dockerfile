FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

RUN git clone https://github.com/Akizon77/KoriMiyohashi ./

RUN dotnet restore src

RUN dotnet publish src -c Release -o /app/build

# 使用运行时镜像创建最终镜像，以减少镜像大小
FROM mcr.microsoft.com/dotnet/runtime:8.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends ffmpeg \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /app/build .
RUN chmod +x ./KoriMiyohashi

ENTRYPOINT ["./KoriMiyohashi"]
