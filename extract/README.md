# Game data extraction

Pulls Valheim's recipes, items, buildings and creatures out of the game's own data. Ubuntu only.

```
extract/extract-data.sh               # writes public/data/{recipes,items,pieces,creatures}.json
```

Steps (each is also runnable on its own, and is safe to re-run):

1. `fetch-assets.sh` — installs steamcmd (needs sudo for apt) and downloads the Valheim Dedicated Server (app 896660, anonymous login). It contains the same Unity assets as the game.
2. `rip-assets.sh` — runs [AssetRipper](https://github.com/AssetRipper/AssetRipper) headless and exports the assets as a Unity project (about 9 GB, a couple of minutes).
3. `ValheimData/` — .NET project that reads the exported recipe assets and item/piece/creature prefabs, resolves references via prefab GUIDs, takes English names from `localization.txt`, and reads enum names (item types, factions, ...) from `assembly_valheim.dll`.

All downloaded and extracted data lives in `extract/.work/`, which is gitignored. Only the generated JSON files are meant to be committed; don't commit the raw game files.
