using BarcodeTool.Models;
using BarcodeTool.Services;
using BlazorMvvm;
using System.Text;
using ZXing;

namespace BarcodeTool.ViewModels;

[BlazorMvvmViewModel(ViewModelLifetime.Transient)]
public partial class GeneratorViewModel(IBarcodeGeneratorService generatorService, IJsInteropService jsInterop) : BlazorViewModel
{
    private const string PreviewImageId = "barcode-preview-img";

    [BlazorObservableProperty]
    private string _content = "Hello World!";

    [BlazorObservableProperty]
    private BarcodeFormat _format = BarcodeFormat.QR_CODE;

    [BlazorObservableProperty]
    private int _width = 300;

    [BlazorObservableProperty]
    private int _height = 300;

    [BlazorObservableProperty]
    private int _marginLeft = 10;

    [BlazorObservableProperty]
    private int _marginRight = 10;

    [BlazorObservableProperty]
    private int _marginTop = 10;

    [BlazorObservableProperty]
    private int _marginBottom = 10;

    [BlazorObservableProperty]
    private bool _showTextBelow = true;

    [BlazorObservableProperty]
    private int _fontSize = 14;

    [BlazorObservableProperty]
    private string _qrErrorCorrectionLevel = "M";

    [BlazorObservableProperty]
    private bool _enableGS1 = false;

    [BlazorObservableProperty]
    private int _pdf417ErrorLevel = 2;

    [BlazorObservableProperty]
    private bool _pdf417Compact = false;

    [BlazorObservableProperty]
    private int _aztecErrorPercent = 33;

    [BlazorObservableProperty]
    private byte[] _generatedImageBytes;

    [BlazorObservableProperty]
    private string _generatedSvg;

    [BlazorObservableProperty]
    private string _errorMessage;

    public bool HasGeneratedImage => GeneratedImageBytes != null;

    public IReadOnlyList<BarcodeFormat> SupportedFormats => generatorService.SupportedFormats;

    public bool Is1DFormat(BarcodeFormat format) => generatorService.Is1DFormat(format);

    public bool SupportsGS1(BarcodeFormat format) => generatorService.SupportsGS1(format);

    [BlazorCommand]
    private async Task GenerateBarcodeAsync()
    {
        ErrorMessage = null;

        BarcodeGenerationOptions options = new()
        {
            Content = Content,
            Format = Format,
            Width = Width,
            Height = Height,
            MarginTop = MarginTop,
            MarginRight = MarginRight,
            MarginBottom = MarginBottom,
            MarginLeft = MarginLeft,
            ShowTextBelow = ShowTextBelow,
            FontSize = FontSize,
            EnableGS1 = EnableGS1,
            QrErrorCorrectionLevel = QrErrorCorrectionLevel,
            Pdf417ErrorLevel = Pdf417ErrorLevel,
            Pdf417Compact = Pdf417Compact,
            AztecErrorPercent = AztecErrorPercent
        };

        BarcodeGenerationResult result = await generatorService.GenerateAsync(options);

        if (result.Success)
        {
            GeneratedImageBytes = result.ImageBytes;
            GeneratedSvg = result.SvgContent;

            if (GeneratedImageBytes != null)
            {
                await jsInterop.CreateBlobUrlAsync(PreviewImageId, GeneratedImageBytes, "image/png");
            }
        }
        else
        {
            ErrorMessage = result.ErrorMessage;
            GeneratedImageBytes = null;
            GeneratedSvg = null;
        }

        OnPropertyChanged(nameof(HasGeneratedImage));
    }

    [BlazorCommand]
    private async Task DownloadImageAsync()
    {
        if (GeneratedImageBytes == null) return;

        string fileName = $"barcode_{Format}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        await jsInterop.DownloadFileAsync(fileName, GeneratedImageBytes, "image/png");
    }

    [BlazorCommand]
    private async Task DownloadSvgAsync()
    {
        if (string.IsNullOrEmpty(GeneratedSvg)) return;

        byte[] bytes = Encoding.UTF8.GetBytes(GeneratedSvg);
        string fileName = $"barcode_{Format}_{DateTime.Now:yyyyMMdd_HHmmss}.svg";
        await jsInterop.DownloadFileAsync(fileName, bytes, "image/svg+xml");
    }

    public async Task DisposeAsync()
    {
        await jsInterop.RevokeBlobUrlAsync(PreviewImageId);
    }
}
