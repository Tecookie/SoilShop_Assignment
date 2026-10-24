FROM mcr.microsoft.com/dotnet/sdk:10.0.402 AS build
ARG PROJECT=SoilShop.Api
WORKDIR /source
COPY global.json Directory.Build.props ./
COPY src/ src/
RUN --mount=type=cache,id=soilshop-nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet publish src/${PROJECT}/${PROJECT}.csproj -c Release -o /publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG PROJECT=SoilShop.Api
ENV APP_DLL=${PROJECT}.dll
WORKDIR /app
COPY --from=build /publish ./
USER $APP_UID
ENTRYPOINT ["/bin/sh", "-c", "exec dotnet \"$APP_DLL\""]
