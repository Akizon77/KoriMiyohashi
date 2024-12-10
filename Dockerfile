FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY ./ ./

RUN dotnet restore src

RUN dotnet publish src -c Release -o /app/build

# 使用运行时镜像创建最终镜像，以减少镜像大小
FROM mcr.microsoft.com/dotnet/runtime:8.0

WORKDIR /app

COPY --from=build /app/build .
RUN chmod +x ./KoriMiyohashi

ENTRYPOINT ["./KoriMiyohashi"]
