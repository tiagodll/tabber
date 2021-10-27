
FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
ARG buildconfig
WORKDIR /app
COPY ./tabber.Client /app/tabber.Client
COPY ./tabber.Server /app/tabber.Server
RUN env
RUN dotnet publish ./tabber.Server -o /app/publish -c Release

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS runtime-env
ARG buildconfig
WORKDIR /publish
COPY --from=build-env /app/publish .
ENTRYPOINT [ "dotnet","tabber.Server.dll" ]
