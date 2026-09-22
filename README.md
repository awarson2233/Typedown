<p align="center">
  <img alt="Typedown Logo" src="./logo.png" width="100px" />
  <h1 align="center">Typedown</h1>
</p>

[![Typedown Download](https://get.microsoft.com/images/en-us%20light.svg)](https://apps.microsoft.com/detail/9p8tcw4h2hb4)

Typedown is a lightweight Markdown editor designed specifically for the Windows platform. With the WinUI framework, it provides users with a seamless interface and efficient editing experience that perfectly matches the operating system. Whether you're writing technical documents, academic papers, or blog posts, Typedown is your go-to assistant!

## Screenshots
<figure>
<img src="https://github.com/byxiaozhi/Typedown/assets/31278216/d0c9d76b-ecd2-4941-90ca-0f8c639c2ef0" width=200/>
<img src="https://github.com/byxiaozhi/Typedown/assets/31278216/d5320590-2d0b-4f9a-a3d2-4661eb021758" width=200/>
<img src="https://github.com/byxiaozhi/Typedown/assets/31278216/2ce7795c-1043-41ed-a420-c42f7aa5aa80" width=200/>
<img src="https://github.com/byxiaozhi/Typedown/assets/31278216/a2df17f3-3100-4129-b0ba-0e90e14a89bf" width=200/>
</figure>

## Building from source

### 1. Prerequisites
- [Visual Studio 2026](https://visualstudio.microsoft.com/) with the WinUI / Windows application development workload and the ARM64 build tools
- .NET 10 SDK
- Git for Windows
- [Node.js](https://nodejs.org/) with [Yarn Classic 1.x](https://classic.yarnpkg.com/) (`npm i -g yarn`)

### 2. Clone the repository
```ps
git clone https://github.com/byxiaozhi/Typedown
```

This will create a local copy of the repository.

### 3. Build the editor frontend
Install the frontend dependencies once, then build the WebView bundle:

```ps
cd Typedown\Dev\Typedown.Editor
yarn
yarn build
```
![20240319232236_rec_](https://github.com/byxiaozhi/Typedown/assets/31278216/3f038707-9311-4aad-846b-a22e8bad6857)

`yarn build` writes the bundle into `Typedown\Dev\Typedown.WinUI\Resources\Statics`. The WinUI build does not build the frontend, so run `yarn build` again whenever the editor frontend changes.

### 4. Build and run
Open `Typedown\Typedown.sln` in Visual Studio, right-click the `Typedown.WinUI` project, and select Set as Startup Project. For daily development use `Debug_Local` (unpackaged, unsigned) with `ARM64` or `x64`, then click Run!

The `Debug` and `Release` configurations produce a signed MSIX package and need a local code-signing certificate; see [docs/build.md](docs/build.md).

![20240319232529_rec_](https://github.com/byxiaozhi/Typedown/assets/31278216/50ef6e56-b177-49b0-b361-83659d25a40e)

### Contributors
Want to contribute to this project? Let us know with an [issue](https://github.com/byxiaozhi/Typedown/issues) that communicates your intent to create a [pull request](https://github.com/byxiaozhi/Typedown/pulls).
