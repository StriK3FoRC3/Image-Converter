<div align="center">
    <img src="/app-screenshot1.png" width="400px"</img> 
</div>

# Image Converter

Image Converter is a compact Windows utility for batch-converting images, PDFs, and videos. It runs as a portable app, keeps its `Input` and `Output` folders beside the executable, and does not require FFmpeg, ImageMagick, or another converter to be installed.


⚠️Disclaimer⚠️: This app was created with the help of ChatGPT/Codex. If you don't like vibe coded programs, don't use it. Please report any bugs you find, and feel free to suggest new features.
I created this portable program so I can quickly convert any image or video to whatever file type I need. Yes, there are already many tools like this, but I wanted to make one my own way.



## Features

- Convert every supported file in the `Input` folder as one batch
- Drag files or folders onto the window without adding duplicate filenames
- Convert images between common, modern, and specialist formats
- Convert videos to GIF, animated WebP, animated MNG, or image sequences
- Convert PDF pages to individual images
- Resize by width, height, or a percentage from 5% to 100%
- Prevent output dimensions from exceeding the source dimensions
- Preserve the last selected output format
- Cancel a conversion in progress
- Write output names as `name-conv.ext`

## Download

Download the latest Windows x64 archive from this repository's Releases page, extract the complete folder, and run `ImageConverter.exe`. No installation is required.

Windows SmartScreen may show a warning for unsigned community-built executables. The source and automated build are included in this repository so releases can be independently inspected and rebuilt.

## Supported Files

Image input and output:

`PNG`, `JPEG`, `WebP`, `GIF`, `BMP`, `TIFF`, `ICO`, `SVG`, `AVIF`, `JPEG XL`, `JPEG 2000`, `SVGZ`, `CUR`, `DDS`, `EXR`, `HDR`, `TGA`, `PCX`, `PSD`, `QOI`, `PNM`, `PBM`, `PGM`, `PPM`, `PAM`, `WBMP`, `XBM`, `XPM`, `FITS`, `DPX`, `JNG`, and `MNG`.

Input only:

- `HEIF` / `HEIC`
- `PDF`
- `MP4`, `MOV`, `AVI`, `MKV`, and `WebM`

HEIF/HEIC output is unavailable because the bundled codec does not encode HEVC-based HEIC files. Video decoding uses Windows' built-in media support, so a video must be playable by Windows on the same computer.

## Usage

1. Put supported files in `Input` beside the executable, or drag files and folders onto the app.
2. Choose one output format.
3. Optionally enter a maximum width, maximum height, or scale percentage.
4. Select **Convert**.

Images and single-page PDFs are written directly to `Output`. Multi-page PDFs and video image sequences are placed in their own `name-conv` folder. **Clear** removes files from `Input`; it does not delete converted output.

## Local Data

The app keeps all working files beside the executable:

- `Input\`: source files waiting to be converted
- `Output\`: converted files
- `last_format.txt`: the most recently selected output format

The app does not collect telemetry or upload files.

## Build

Building requires Windows, the .NET Framework 4.7.1 developer pack, and a Windows SDK. Run:

```bat
build.bat
```

The first build automatically downloads the pinned Magick.NET 14.14.0 packages from NuGet. The runnable x64 application and all required files are written to:

```text
build\
```

Keep the executable, the three Magick.NET DLL files, `LICENSE.txt`, and `THIRD-PARTY-NOTICES.txt` together when distributing a build.

Pushing a version tag such as `v1.0.0` runs the release workflow and publishes a ready-to-download Windows x64 archive.

## Contributing

Bug reports, feature ideas, and noncommercial contributions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) before submitting a pull request.

## License

Image Converter is available under the [PolyForm Noncommercial License 1.0.0](LICENSE) You may use, modify, and distribute it for permitted noncommercial purposes. Commercial use is not granted by this license.

Third-party components retain their own licenses. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md); complete dependency notices are included with every build.
