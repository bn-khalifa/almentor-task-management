# Multi-stage build: the SDK image compiles/publishes the app, then a slim
# ASP.NET runtime image runs the published output — keeps the final image
# small and free of build tools/source.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first and restore — layer-cached, so `docker build` only
# re-restores when a .csproj actually changes, not on every source edit.
COPY src/Almentor.TaskApi.Domain/*.csproj src/Almentor.TaskApi.Domain/
COPY src/Almentor.TaskApi.Application/*.csproj src/Almentor.TaskApi.Application/
COPY src/Almentor.TaskApi.Infrastructure/*.csproj src/Almentor.TaskApi.Infrastructure/
COPY src/Almentor.TaskApi.Api/*.csproj src/Almentor.TaskApi.Api/
RUN dotnet restore src/Almentor.TaskApi.Api/Almentor.TaskApi.Api.csproj

# Now copy everything else and publish.
COPY src/ src/
RUN dotnet publish src/Almentor.TaskApi.Api/Almentor.TaskApi.Api.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Almentor.TaskApi.Api.dll"]
