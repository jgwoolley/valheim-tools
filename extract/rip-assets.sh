#!/usr/bin/env bash
# Runs AssetRipper headless against the downloaded Valheim server data and
# exports it as a Unity project (YAML) into extract/.work/ripped/.
# Takes a couple of minutes and ~9 GB of disk; all of it is gitignored.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WORK="$HERE/.work"
ASSETRIPPER_VERSION=2.0.0
ASSETRIPPER_DIR="$WORK/assetripper"
DATA_DIR="$WORK/valheim-server/valheim_server_Data"
OUT_DIR="$WORK/ripped"
PORT=5123
BASE="http://localhost:$PORT"

[ -d "$DATA_DIR" ] || { echo "Run fetch-assets.sh first ($DATA_DIR missing)" >&2; exit 1; }

if [ ! -x "$ASSETRIPPER_DIR/AssetRipper.GUI.Free" ]; then
  echo "==> Downloading AssetRipper $ASSETRIPPER_VERSION"
  mkdir -p "$ASSETRIPPER_DIR"
  curl -fsSL "https://github.com/AssetRipper/AssetRipper/releases/download/$ASSETRIPPER_VERSION/AssetRipper_linux_x64.tar.xz" \
    | tar -xJ -C "$ASSETRIPPER_DIR"
fi

echo "==> Starting AssetRipper on $BASE"
"$ASSETRIPPER_DIR/AssetRipper.GUI.Free" --headless --port "$PORT" --log=false >"$WORK/assetripper.log" 2>&1 &
AR_PID=$!
trap 'kill "$AR_PID" 2>/dev/null || true' EXIT

for _ in $(seq 60); do
  curl -fs -o /dev/null "$BASE/" && break
  sleep 1
done

echo "==> Loading $DATA_DIR"
curl -fs -o /dev/null -X POST "$BASE/LoadFolder" --data-urlencode "Path=$DATA_DIR"
# LoadFolder returns before processing finishes; wait for the log to say so.
for _ in $(seq 600); do
  grep -q "Finished processing assets" "$WORK/assetripper.log" && break
  sleep 1
done

echo "==> Exporting Unity project to $OUT_DIR"
rm -rf "$OUT_DIR"
curl -fs -o /dev/null -X POST "$BASE/Export/UnityProject" \
  --data-urlencode "Path=$OUT_DIR" --data-urlencode "CreateSubfolder=false"

echo "==> Done: $OUT_DIR/ExportedProject/Assets"
