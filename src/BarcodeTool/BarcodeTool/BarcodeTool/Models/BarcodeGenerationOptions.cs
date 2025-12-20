namespace BarcodeTool.Models;

using ZXing;

/// <summary>
/// Options for barcode generation.
/// </summary>
public class BarcodeGenerationOptions
{
    public string Content { get; set; } = string.Empty;
    public BarcodeFormat Format { get; set; } = BarcodeFormat.QR_CODE;
    public int Width { get; set; } = 300;
    public int Height { get; set; } = 300;

    // Margins
    public int MarginTop { get; set; } = 10;
    public int MarginRight { get; set; } = 10;
    public int MarginBottom { get; set; } = 10;
    public int MarginLeft { get; set; } = 10;

    // 1D barcode options
    public bool ShowTextBelow { get; set; } = true;
    public int FontSize { get; set; } = 14;

    // GS1 support (for Code-128, DataMatrix, QR Code)
    public bool EnableGS1 { get; set; } = false;

    // QR Code options
    public string QrErrorCorrectionLevel { get; set; } = "M";

    // PDF417 options
    public int Pdf417ErrorLevel { get; set; } = 2;
    public bool Pdf417Compact { get; set; } = false;

    // Aztec options
    public int AztecErrorPercent { get; set; } = 33;
}
