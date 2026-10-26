# Changelog

What changed in these tools, newest first, grouped by day; only things a visitor or contributor would notice, not every commit.
There are no version numbers or tags: `main` is built and deployed to GitHub Pages, and the footer of every page shows the build time.
Older detail is in `git log`.

## 2026-10-26

- **Weapons** page (Game Info menu): every melee weapon, bow, crossbow, staff, fist weapon and arrow with its damage (total and breakdown), damage at max
  upgrade level, block and parry, stamina cost for the normal and secondary attack, knockback, backstab bonus, tier and durability.
  Totals leave out chop and pickaxe damage, which only matters against trees and rocks.

## 2026-10-25

**Game info** (new): Valheim's recipes, items, buildings and creatures, read straight out of the game's own files rather than typed in by hand.

- **Main menu** at `#/`, laid out like the Vintage Story wiki's home page: sections of cards with an icon, a description and an entry count.
  The save file parser moved to `#/saves`, so the old `#/` address now lands on the menu.
- **Pages** under a "Game Info" menu: Recipes, Items, Armor, Trinkets, Food & drink, Smelting & cooking, Buildings and Creatures. Every table has
  search, filters, click-to-sort columns and pagination, and all of that is in the page address (`#/items?q=iron&type=Shield&sort=weight&page=2&size=50`),
  so a view can be bookmarked or shared and Back steps through it.
- **Pages link to each other by game id**: ingredients, stations, drops, and an item's own "Recipe", "Made in", "Used in recipes", "Used in buildings",
  "Smelted or cooked" and "Dropped by" links (`#/recipes?id=SwordIron`, `#/recipes?uses=Wood`, `#/creatures?drop=TrophyBoar`). A link only shows when
  its page has something to show. A tag above the table says what the link filtered to, with a "Show all" button.
- **Upgrade items**: recipes list the idol needed to upgrade the gear ("To upgrade: 1 Black Metal Protection Idol") as a link.
- **Smelting & cooking**: what smelters, kilns, the spinning wheel, windmill, ovens, cooking stations and the fermenter turn each item into, with the
  time and fuel.
- **Pin recipes and buildings** (the star in the Recipes and Buildings tables). The **Materials list** page adds up everything you need to gather,
  with how many to make, the quality level to upgrade gear to, stacks to carry, and the crafting stations needed. Crafted ingredients (Iron Nails)
  and smelting steps (ore to iron, wood to coal, with the coal fuel) can be broken down into raw materials, each with its own checkbox.
- **Shareable lists**: the Materials list is stored in the address (`#/pinned?pin=Recipe_ShieldBlackmetal*5*3&smelt=0`) and has a Copy share link
  button. Opening someone else's list shows it as a shared list and leaves your own pins alone until you choose "Save as my pins".

**Save file parser**

- **Real names**: inventory items are matched against the full game item list, so items that were just "Item #12345" (including ones other players
  crafted) show their name, along with new Type and Weight columns. Recipes, materials, stations, trophies, active food and the kill, pickup, craft,
  food and placed-piece stats show English names, with the raw name on hover. Known Texts and Tutorials Seen show the readable titles and bodies
  instead of `$tutorial_stemple1_label`-style tokens.
- **Links back to the wiki**: those names link to their Items, Buildings or Creatures entry.

**For contributors**

- `extract/` holds the pipeline that produces `public/data/*.json`: `fetch-assets.sh` installs steamcmd and downloads the Valheim Dedicated Server,
  `rip-assets.sh` exports it with AssetRipper, and the `ValheimData` .NET project turns the export into JSON (enum names come from
  `assembly_valheim.dll`). `extract/extract-data.sh` runs all three. Downloaded and extracted game files stay in the gitignored `extract/.work/`.
- The service worker cache name is bumped whenever `public/` changes (`valheim-viewer-vN`).

## 2026-10-22

- nf3t.com link update in the header menu.

## 2026-10-12

- Link to the Vintage Story wiki.
- The "where do I find my character file?" table shows the paths for your operating system first.

## 2026-09-13

- Settings are remembered in the browser (local storage).
- Known Players moved to the top level and the player-matching logic was reworked.
- Playtime by World Name, and a more responsive location table.

## 2026-09-12

- First release: parse Valheim `.fch` character files in the browser, with a checksum check, a Steam link and Open Graph preview info.
