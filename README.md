# BarcodeTool

![https://github.com/github/docs/actions/workflows/main.yml](https://github.com/gbastecki/BarcodeTool/actions/workflows/build.yml/badge.svg)
![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![Blazor WebAssembly](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor)
[![GitHub](https://img.shields.io/github/license/gbastecki/BarcodeTool?color=0000a4&style=plastic)](https://github.com/gbastecki/BarcodeTool/blob/main/LICENSE)

A modern, client-side barcode scanner and generator built with Blazor WebAssembly. 

All processing happens locally in your browser - no data is ever sent to a server.

## Features

### Barcode Scanner
- Upload images to scan for barcodes (supports drag & drop)
- Detects multiple barcodes in a single image
- Displays detailed metadata: content, format, dimension (1D/2D), raw bytes
- GS1 standard detection
- Supports SVG and raster image formats

### Barcode Generator
- Generate 20+ barcode formats including QR Code, DataMatrix, EAN-13, Code 128, and more
- Customizable options: size, margins, error correction levels
- GS1 standard support for compatible formats
- Download as PNG or SVG
- Real-time preview

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

### Build & Run

```bash
# Clone the repository
git clone https://github.com/gbastecki/BarcodeTool.git
cd BarcodeTool

# Run the application
dotnet run --project BarcodeTool

# Or build for production
dotnet publish -c Release
```

## Technology Stack

- **[Blazor WebAssembly](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)** - Client-side web framework
- **[SkiaSharp](https://github.com/mono/SkiaSharp)** - Cross-platform 2D graphics library
- **[ZXing.Net](https://github.com/micjahn/ZXing.Net)** - Barcode reading and writing library
- **[Svg.Skia](https://github.com/wieslawsoltes/Svg.Skia)** - SVG rendering with SkiaSharp
- **[BlazorMvvm](https://github.com/gbastecki/BlazorMvvm)** - MVVM framework for Blazor

## Supported Barcode Formats

### 1D Barcodes
CODE_39, CODE_93, CODE_128, EAN_8, EAN_13, UPC_A, UPC_E, ITF, CODABAR, MSI, PLESSEY

### 2D Barcodes
QR_CODE, DATA_MATRIX, AZTEC, PDF_417

## License

This project is licensed under the **MIT** license - see the [LICENSE](LICENSE) file for details.

### Third-Party Libraries

This project uses several open-source libraries. See the [NOTICE](NOTICE) file for attribution information.

| Library | License |
|---------|---------|
| BlazorMvvm | MIT |
| Microsoft.AspNetCore.Components.WebAssembly | MIT |
| SkiaSharp | MIT |
| Svg.Skia | MIT |
| ZXing.Net | Apache 2.0 |
| ZXing.Net.Bindings.SkiaSharp | Apache 2.0 |

## Privacy

BarcodeTool runs entirely in your browser.

No barcode data, uploaded images, or generated barcodes are ever transmitted to any server.

Your data stays on your device.
