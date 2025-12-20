using BlazorMvvm;

namespace BarcodeTool.ViewModels;

[BlazorMvvmViewModel(ViewModelLifetime.Singleton)]
public partial class LibrariesViewModel : BlazorViewModel
{
    internal record LibraryInfo(string Name, string Version, string Description, string License, string NuGetUrl);

    internal readonly LibraryInfo[] LibraryList =
    [
        new("BlazorMvvm", "1.1.2",
            "MVVM framework for Blazor with source generators for automatic property change notification and ViewModel lifecycle management.",
            "MIT License",
            "https://www.nuget.org/packages/gbastecki.BlazorMvvm/1.1.2"),

        new("Microsoft.AspNetCore.Components.WebAssembly", "10.0.1",
            "Core framework for building Blazor WebAssembly applications, providing client-side hosting and component rendering.",
            "MIT License",
            "https://www.nuget.org/packages/Microsoft.AspNetCore.Components.WebAssembly/10.0.1"),

        new("SkiaSharp", "3.119.1",
            "Cross-platform 2D graphics API for .NET based on Google's Skia Graphics Library. Used for image processing, barcode rendering, and SVG conversion.",
            "MIT License",
            "https://www.nuget.org/packages/SkiaSharp/3.119.1"),

        new("SkiaSharp.NativeAssets.WebAssembly", "3.119.1",
            "Native WebAssembly bindings for SkiaSharp, enabling hardware-accelerated graphics in Blazor WebAssembly applications.",
            "MIT License",
            "https://www.nuget.org/packages/SkiaSharp.NativeAssets.WebAssembly/3.119.1"),

        new("SkiaSharp.Views.Blazor", "3.119.1",
            "Blazor-specific views and components for SkiaSharp, providing seamless integration with Blazor's component model.",
            "MIT License",
            "https://www.nuget.org/packages/SkiaSharp.Views.Blazor/3.119.1"),

        new("Svg.Skia", "3.2.1",
            "SVG rendering library that uses SkiaSharp as its rendering backend. Enables parsing and displaying SVG images.",
            "MIT License",
            "https://www.nuget.org/packages/Svg.Skia/3.2.1"),

        new("ZXing.Net", "0.16.11",
            "Port of the ZXing (Zebra Crossing) barcode scanning library to .NET. Supports reading and writing of various 1D and 2D barcode formats.",
            "Apache License 2.0",
            "https://www.nuget.org/packages/ZXing.Net/0.16.11"),

        new("ZXing.Net.Bindings.SkiaSharp", "0.16.22",
            "SkiaSharp bindings for ZXing.Net, enabling barcode generation and scanning using SkiaSharp bitmaps.",
            "Apache License 2.0",
            "https://www.nuget.org/packages/ZXing.Net.Bindings.SkiaSharp/0.16.22"),
    ];
}