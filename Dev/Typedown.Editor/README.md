# Typedown.Editor

This React editor bundle is hosted by `Typedown.WinUI` through WebView2.

## Build the WebView bundle

Run the frontend build from this directory:

```powershell
yarn
yarn build
```

`config-overrides.js` redirects the Create React App build output to:

```text
..\Typedown.WinUI\Resources\Statics
```

The output contains `index.html`, `asset-manifest.json`, `static/js/*`, `static/css/*`, `static/media/*`, and files copied from `public`.

## Let WinUI consume the bundle

After `yarn build`, build or start `Typedown.WinUI`. The WinUI project copies the existing `Resources\Statics` files into its output directory and MSIX package payload.

If you only run `yarn build` but do not rebuild/restart `Typedown.WinUI`, the app can still load an older bundle from `bin\...\Resources\Statics` because the runtime checks the output directory first.

## Development server

`yarn start` runs the CRA development server, but the current WinUI host uses the compiled local bundle in `Resources\Statics`.
