# Multi-stage Dockerfile for HomeSphere ASP.NET Core Web API (.NET 8)
# Stage 1: Build and Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project definition files for layer caching during restore
COPY ["src/HomeSphere.Domain/HomeSphere.Domain.csproj", "src/HomeSphere.Domain/"]
COPY ["src/HomeSphere.Application/HomeSphere.Application.csproj", "src/HomeSphere.Application/"]
COPY ["src/HomeSphere.Infrastructure/HomeSphere.Infrastructure.csproj", "src/HomeSphere.Infrastructure/"]
COPY ["src/HomeSphere.WebApi/HomeSphere.WebApi.csproj", "src/HomeSphere.WebApi/"]
COPY ["HomeSphere.sln", "./"]

RUN dotnet restore "src/HomeSphere.WebApi/HomeSphere.WebApi.csproj"

# Copy source code and build release package
COPY src/ src/
RUN dotnet publish "src/HomeSphere.WebApi/HomeSphere.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Minimal, Hardened Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Non-root user execution for container hardening (built-in ASP.NET 8 app user)
USER app

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENV DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "HomeSphere.WebApi.dll"]
