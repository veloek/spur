# syntax=docker/dockerfile:1
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY Spur/Spur.csproj ./
RUN dotnet restore -a $TARGETARCH

# Copy everything else and build
COPY Spur ./
RUN dotnet publish --no-restore -a $TARGETARCH -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

VOLUME /app/storage

RUN umask 0077

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["./Spur"]
