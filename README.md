# Nf3t's Valheim Tools

Parses and displays Valheim `.fch` character save files entirely in your browser. Nothing is ever uploaded anywhere.

## Using it

```
npm install
```

This also vendors Cytoscape.js (used for the per-world pin map) into `public/vendor/`, so the page never depends on any external site.

Then just open `public/index.html` in a browser, or serve the `public/` folder locally (e.g. `npx http-server public`) if you want the installable PWA bits (service worker, offline caching) to work too — those require http(s), not a plain `file://` path.

## Build / deploy

`npm run build` generates `public/build.json` (build date + commit link), shown in the footer. It's meant to run in CI, not locally — without it the footer just stays empty.

Pushing to `main` runs `.github/workflows/deploy.yml`, which builds and deploys `public/` to GitHub Pages automatically.

## Project layout

- `public/` — the actual site (everything that gets deployed)
- `scripts/` — small Node scripts used by `npm install` / `npm run build`
- `.github/workflows/` — the GitHub Pages deploy pipeline
