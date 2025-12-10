# syntax=docker/dockerfile:1.7-labs
# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
ENV DEBIAN_FRONTEND=noninteractive
RUN apt update && apt install -y mc tshark curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Directory.Packages.props", "."]
COPY ["src/Directory.Build.props", "."]
COPY ["src/Red55.Uma.Authz.Proxy/Red55.Uma.Authz.Proxy.csproj", "Red55.Uma.Authz.Proxy/"]
RUN dotnet restore "./Red55.Uma.Authz.Proxy/Red55.Uma.Authz.Proxy.csproj"
WORKDIR "/src/Red55.Uma.Authz.Proxy"
RUN --mount=type=bind,source=./.git,target=/src/Red55.Uma.Authz.Proxy/.git/ dotnet build "./Red55.Uma.Authz.Proxy.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN --mount=type=bind,source=./.git,target=/src/Red55.Uma.Authz.Proxy/.git/ dotnet publish "./Red55.Uma.Authz.Proxy.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Red55.Uma.Authz.Proxy.dll"]
