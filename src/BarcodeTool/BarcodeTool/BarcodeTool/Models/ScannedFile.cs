namespace BarcodeTool.Models;

public class ScannedFile
{
    public string Id { get; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public byte[] ImageBytes { get; set; }

    public string ContentType { get; set; }

    public List<BarcodeResultWrapper> Barcodes { get; set; } = [];

    public string PreviewElementId => $"preview-{Id}";
}
