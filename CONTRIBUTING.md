# Contributing to Image Converter

Thanks for helping improve Image Converter.

## Before You Start

- Search existing issues before opening a new one.
- Keep changes focused and consistent with the compact Windows desktop design.
- Do not commit restored packages, generated builds, converted files, or local settings.
- Contributions must be compatible with the PolyForm Noncommercial License 1.0.0.

## Building

Use Windows 10 or Windows 11 with the .NET Framework 4.7.1 developer pack, then run:

```bat
build.bat
```

The first build downloads the pinned Magick.NET packages if they are not already present. The runnable application is written to `build\`.

## Pull Requests

1. Run `build.bat` and confirm it completes successfully.
2. Test the affected conversion workflow on Windows 10 or Windows 11.
3. Describe the behavior change and the checks you performed.
4. Include screenshots for visible interface changes.

By submitting a contribution, you agree that it may be distributed under the repository's license.
