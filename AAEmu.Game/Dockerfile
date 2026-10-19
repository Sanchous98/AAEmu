FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
ARG CONFIGURATION
ARG GAME_DB_URL

RUN apt-get update \
    && apt-get install -y --no-install-recommends xz-utils \
    && rm -rf /var/lib/apt/lists/*

WORKDIR app
COPY ./Directory.Build.props .
COPY ./Directory.Packages.props .
COPY ./AAEmu.Commons ./AAEmu.Commons
COPY ./AAEmu.Game ./AAEmu.Game
RUN dotnet publish ./AAEmu.Game/AAEmu.Game.csproj -c $CONFIGURATION -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0

ARG CONFIGURATION
ARG LOGIN_HOST
ARG LOGIN_PORT
ARG DB_HOST
ARG DB_PORT
ARG DB_USER
ARG DB_PASSWORD

# NLua/KeraLua ship a glibc (linux-x64) native liblua54.so only, so Game must run
# on a glibc base image. The musl/alpine image has no matching native asset and
# crashes (SIGSEGV) as soon as BigMonster Lua AI params are parsed.
RUN apt-get update \
    && apt-get install -y --no-install-recommends openssl default-mysql-client \
    && rm -rf /var/lib/apt/lists/*

WORKDIR app
COPY --from=builder /app/publish ./

EXPOSE 1239 1250
ENTRYPOINT ["dotnet", "AAEmu.Game.dll"]
