# Typedown.Editor

This React editor bundle is hosted by `Typedown.WinUI` through WebView2.

## Build the WebView bundle

Install dependencies once from this directory with `yarn`, then run `yarn build` whenever the frontend source or dependencies change. Visual Studio/MSBuild builds do not build this frontend automatically.

`config-overrides.js` redirects the Create React App build output to:

```text
..\Typedown.WinUI\Resources\Statics
```

The output contains `index.html`, `asset-manifest.json`, `static/js/*`, `static/css/*`, `static/media/*`, and files copied from `public`.

## Let WinUI consume the bundle

The WinUI build refreshes the existing files under `Resources\Statics` before output-copy and MSIX packaging targets consume them. Run `yarn build` manually before building WinUI whenever the frontend bundle needs to be updated.

## Development server

`yarn start` runs the CRA development server, but the current WinUI host uses the compiled local bundle in `Resources\Statics`.
