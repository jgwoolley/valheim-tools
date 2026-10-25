#!/usr/bin/env bash
# Installs steamcmd (Ubuntu) and downloads the Valheim Dedicated Server, which
# ships the same Unity assets as the game and can be fetched anonymously.
# Everything lands in extract/.work/, which is gitignored.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WORK="$HERE/.work"
STEAMCMD_DIR="$WORK/steamcmd"
SERVER_DIR="$WORK/valheim-server"
VALHEIM_SERVER_APP_ID=896660

mkdir -p "$WORK"

if ! dpkg -s lib32gcc-s1 xz-utils curl ca-certificates >/dev/null 2>&1; then
  echo "==> Installing system dependencies (needs sudo)"
  sudo dpkg --add-architecture i386
  sudo apt-get update
  sudo apt-get install -y curl ca-certificates xz-utils lib32gcc-s1
fi

if [ ! -x "$STEAMCMD_DIR/steamcmd.sh" ]; then
  echo "==> Installing steamcmd into $STEAMCMD_DIR"
  mkdir -p "$STEAMCMD_DIR"
  curl -fsSL https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz | tar -xz -C "$STEAMCMD_DIR"
fi

echo "==> Downloading Valheim Dedicated Server (app $VALHEIM_SERVER_APP_ID) into $SERVER_DIR"
# The first anonymous app_update sometimes fails with "Missing configuration"; retrying works.
for attempt in 1 2 3; do
  "$STEAMCMD_DIR/steamcmd.sh" \
    +@sSteamCmdForcePlatformType linux \
    +force_install_dir "$SERVER_DIR" \
    +login anonymous \
    +app_update "$VALHEIM_SERVER_APP_ID" validate \
    +quit && [ -d "$SERVER_DIR/valheim_server_Data" ] && break
  echo "steamcmd attempt $attempt failed, retrying..."
done

echo "==> Done: $SERVER_DIR/valheim_server_Data"
