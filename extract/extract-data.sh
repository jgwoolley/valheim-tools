#!/usr/bin/env bash
# Full pipeline: download Valheim server -> AssetRipper export -> JSON data files.
# Usage: extract/extract-data.sh [output dir]   (default: public/data)
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
OUTPUT="${1:-$HERE/../public/data}"
SERVER="$HERE/.work/valheim-server"

"$HERE/fetch-assets.sh"
BUILD_ID="$(grep -oP '"buildid"\s+"\K\d+' "$SERVER/steamapps/appmanifest_896660.acf")"
"$HERE/rip-assets.sh"
dotnet run --project "$HERE/ValheimData" -- \
  "$HERE/.work/ripped/ExportedProject/Assets" "$OUTPUT" "$BUILD_ID" \
  "$SERVER/valheim_server_Data/Managed/assembly_valheim.dll"
