# Changelog

What changed in these tools, newest first, grouped by day; only things a visitor or contributor would notice, not every commit.
There are no version numbers or tags: `main` is built and deployed to GitHub Pages, and the footer of every page shows the build time.
Older detail is in `git log`.

## 2026-10-26

- **Achievement progress** in the save file parser: a new Achievements tab shows how close the selected character is to every achievement, closest first,
  with a progress bar and what is still missing (kills, crafts, pieces placed and stat counters, with how many you have of each). Needs the data in
  `public/data/achievements.json`. Hard-difficulty achievements are the least certain: they are judged against the per-difficulty stats copy at the
  achievement's difficulty index, which is not verified.
- **Achievements** page: all 53 achievements with what each asks for (kills, crafting, building, stats, other achievements), secret ones marked,
  and the long "do everything" lists tucked into a collapsed list whose entries link to their item, building or creature.
- **Lore** now says where to find it: which runestone, rune tablet or vegvisir holds each text and where it generates (location, biome, how many per
  world), and vegvisirs are listed with the boss location each one reveals (230 entries). Runestone texts follow the location's own override where the
  game has one (the Ashlands runestone's base prefab still lists Mistlands texts).
- **Wiki look**: the whole site now uses the same design as the Vintage Story wiki (green title bar with a search box, left navigation panels, a parchment
  page with serif headings and breadcrumbs, light and dark themes following your system setting), by sharing its stylesheet (`public/wiki.css`, copied
  from that project). The old header menu moved into the sidebar.
- **Lore** page (Game Info): the game's runestone and cave-painting lore, the Hugin and Munin tutorial pop-ups and the dreams, in its own words, with
  search and a kind filter (219 entries). The save file parser's Tutorials Seen and Known Texts link to the matching entry.
- **Search box in the header** finds items, buildings and creatures by name and jumps to their entry (press `/` to focus it).
- **Tables use the wiki's table controls**: search, filter dropdowns, sortable headers, "Showing 26-50 of 183" and Prev/Next buttons, and a removable tag for
  filters that come from a link. All of it is still in the page address.
- On narrow screens the sidebar becomes a menu behind the header's menu button.

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
