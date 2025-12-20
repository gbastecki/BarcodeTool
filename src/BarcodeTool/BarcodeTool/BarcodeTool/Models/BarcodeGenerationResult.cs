namespace BarcodeTool.Models;

/// <summary>
/// Result of barcode generation containing PNG bytes and SVG string.
/// </summary>
public class BarcodeGenerationResult
{
    public byte[] ImageBytes { get; set; }
    public string SvgContent { get; set; }
    public string ErrorMessage { get; set; }
    public bool Success => ImageBytes != null && ErrorMessage == null;
}
