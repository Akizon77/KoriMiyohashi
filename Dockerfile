# 使用官方的 .NET 8 SDK 镜像作为构建阶段的基础镜像
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# 设置工作目录
WORKDIR /app

# 复制项目文件到工作目录
COPY . ./

# 恢复项目的依赖项
RUN dotnet restore

# 构建项目，输出到 /app/build
RUN dotnet publish -c Release -o /app/build

# 使用运行时镜像创建最终镜像，以减少镜像大小
FROM mcr.microsoft.com/dotnet/runtime:8.0

# 设置工作目录
WORKDIR /app

# 从构建阶段复制构建文件到镜像中
COPY --from=build /app/build .
RUN chmod +x ./KoriMiyohashi
# 设置容器启动时的命令
ENTRYPOINT ["./KoriMiyohashi"]
