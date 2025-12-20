using BarcodeTool.Models;
using SkiaSharp;
using Svg.Skia;
using ZXing;
using ZXing.SkiaSharp;

namespace BarcodeTool.Services;

public interface IBarcodeReaderService
{
    /// <summary>
    /// Reads barcodes from image bytes.
    /// </summary>
    Task<List<BarcodeResultWrapper>> ReadBarcodesAsync(byte[] imageBytes, string contentType = null);

    /// <summary>
    /// Checks if a barcode result is GS1 encoded.
    /// </summary>
    bool IsGS1(Result result);

    /// <summary>
    /// Gets the dimension (1D or 2D) of a barcode format.
    /// </summary>
    string GetDimension(BarcodeFormat format);
}

public class BarcodeReaderService : IBarcodeReaderService
{
    public Task<List<BarcodeResultWrapper>> ReadBarcodesAsync(byte[] imageBytes, string contentType = null)
    {
        return Task.Run(() => ReadBarcodesInternal(imageBytes, contentType));
    }

    private static List<BarcodeResultWrapper> ReadBarcodesInternal(byte[] imageBytes, string contentType)
    {
        List<BarcodeResultWrapper> results = new();

        try
        {
            SKBitmap skBitmap = null;

            // Check if it's an SVG file
            if (contentType == "image/svg+xml" || IsSvgContent(imageBytes))
            {
                skBitmap = LoadSvgAsBitmap(imageBytes);
            }
            else
            {
                using MemoryStream ms = new(imageBytes);
                skBitmap = SKBitmap.Decode(ms);
            }

            if (skBitmap != null)
            {
                using (skBitmap)
                {
                    // Add padding around the image for better barcode detection
                    using SKBitmap paddedBitmap = AddPaddingToBitmap(skBitmap);

                    BarcodeReader reader = new();
                    reader.Options.TryHarder = true;

                    Result[] barcodeResults = reader.DecodeMultiple(paddedBitmap);

                    if (barcodeResults != null)
                    {
                        // Adjust result points to account for padding offset
                        int padding = CalculatePadding(skBitmap.Width, skBitmap.Height);

                        foreach (Result r in barcodeResults)
                        {
                            // Offset the result points back to original image coordinates
                            if (r.ResultPoints != null)
                            {
                                for (int i = 0; i < r.ResultPoints.Length; i++)
                                {
                                    ResultPoint pt = r.ResultPoints[i];
                                    r.ResultPoints[i] = new ResultPoint(pt.X - padding, pt.Y - padding);
                                }
                            }
                            results.Add(new BarcodeResultWrapper(r));
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error scanning barcode: {ex.Message}");
        }

        return results;
    }

    private static int CalculatePadding(int width, int height)
    {
        // Add 10% padding on each side, minimum 20 pixels
        return Math.Max(20, (int)(Math.Min(width, height) * 0.1));
    }

    private static SKBitmap AddPaddingToBitmap(SKBitmap original)
    {
        int padding = CalculatePadding(original.Width, original.Height);
        int newWidth = original.Width + (padding * 2);
        int newHeight = original.Height + (padding * 2);

        SKBitmap paddedBitmap = new(newWidth, newHeight);
        using SKCanvas canvas = new(paddedBitmap);

        // Fill with white background (quiet zone)
        canvas.Clear(SKColors.White);

        // Draw original image centered
        canvas.DrawBitmap(original, padding, padding);

        return paddedBitmap;
    }

    private static bool IsSvgContent(byte[] bytes)
    {
        if (bytes.Length < 5) return false;

        string start = System.Text.Encoding.UTF8.GetString(bytes, 0, Math.Min(100, bytes.Length)).TrimStart();
        return start.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) ||
               start.StartsWith("<svg", StringComparison.OrdinalIgnoreCase);
    }

    private static SKBitmap LoadSvgAsBitmap(byte[] svgBytes)
    {
        try
        {
            string svgContent = System.Text.Encoding.UTF8.GetString(svgBytes);
            using SKSvg svg = new();
            svg.FromSvg(svgContent);

            if (svg.Picture == null) return null;

            SKRect bounds = svg.Picture.CullRect;
            int width = (int)Math.Ceiling(bounds.Width);
            int height = (int)Math.Ceiling(bounds.Height);

            if (width <= 0 || height <= 0)
            {
                width = 500;
                height = 500;
            }

            SKBitmap bitmap = new(width, height);
            using SKCanvas canvas = new(bitmap);
            canvas.Clear(SKColors.White);
            canvas.DrawPicture(svg.Picture);

            return bitmap;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading SVG: {ex.Message}");
            return null;
        }
    }

    public bool IsGS1(Result result)
    {
        // Check symbology identifier
        if (result.ResultMetadata != null && result.ResultMetadata.TryGetValue(ResultMetadataType.SYMBOLOGY_IDENTIFIER, out object value))
        {
            string id = value?.ToString();
            if (id != null)
            {
                // GS1 Symbology identifiers:
                // ]C1 = GS1-128
                // ]e0 = GS1 DataBar
                // ]d2 = GS1 DataMatrix
                // ]Q3 = GS1 QR Code
                if (id == "]C1" || id == "]e0" || id == "]d2" || id == "]Q3")
                    return true;
            }
        }

        // Check if content starts with FNC1 or contains AI patterns
        string text = result.Text;
        if (!string.IsNullOrEmpty(text))
        {
            // Check for common GS1 Application Identifiers at start
            if (text.StartsWith("01") || text.StartsWith("(01)") ||
                text.StartsWith("02") || text.StartsWith("(02)") ||
                text.StartsWith("10") || text.StartsWith("(10)") ||
                text.StartsWith("21") || text.StartsWith("(21)"))
            {
                // Additional check for valid GTIN format
                if (text.Length >= 14)
                    return true;
            }
        }

        return false;
    }

    public string GetDimension(BarcodeFormat format)
    {
        return format switch
        {
            BarcodeFormat.QR_CODE or BarcodeFormat.DATA_MATRIX or BarcodeFormat.AZTEC or BarcodeFormat.PDF_417 or BarcodeFormat.MAXICODE => "2D",
            _ => "1D"
        };
    }
}
