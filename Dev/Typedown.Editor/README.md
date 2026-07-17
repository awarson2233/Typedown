# Typedown.Editor

This React editor bundle is hosted by `Typedown.WinUI` through WebView2.

## Build the WebView bundle

Install dependencies once from this directory with `yarn`. `Typedown.WinUI` has a `ProjectReference` to this `.esproj`, so a normal Visual Studio/MSBuild build runs `yarn build` automatically.

`config-overrides.js` redirects the Create React App build output to:

```text
..\Typedown.WinUI\Resources\Statics
```

The output contains `index.html`, `asset-manifest.json`, `static/js/*`, `static/css/*`, `static/media/*`, and files copied from `public`.

## Let WinUI consume the bundle

The WinUI build waits for the editor project, then refreshes `Resources\Statics` before output-copy and MSIX packaging targets consume the files. This ensures newly generated hashed assets are copied instead of the item snapshot from project evaluation.

## Development server

`yarn start` runs the CRA development server, but the current WinUI host uses the compiled local bundle in `Resources\Statics`.
