using Microsoft.JSInterop;
using SkiaSharp;
using ZXing;

namespace BarcodeTool.Services;

public interface IJsInteropService : IAsyncDisposable
{
    ValueTask CreateBlobUrlAsync(string elementId, byte[] bytes, string mimeType);
    ValueTask CreateBlobUrlWithHighlightAsync(string elementId, byte[] imageBytes, string mimeType, ResultPoint[] highlightPoints);
    ValueTask RevokeBlobUrlAsync(string elementId);
    ValueTask RevokeAllBlobUrlsAsync();
    ValueTask DownloadFileAsync(string fileName, byte[] bytes, string mimeType);
    ValueTask<bool> InitializeDropzoneAsync(string dropzoneId, string inputId);
    ValueTask DisposeDropzoneAsync(string dropzoneId);
}

public class JsInteropService(IJSRuntime jsRuntime) : IJsInteropService
{
    private IJSObjectReference _module;

    private async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/interop.js");
        return _module;
    }

    public async ValueTask CreateBlobUrlAsync(string elementId, byte[] bytes, string mimeType)
    {
        IJSObjectReference module = await GetModuleAsync();
        await module.InvokeVoidAsync("createBlobUrl", elementId, bytes, mimeType);
    }

    public async ValueTask CreateBlobUrlWithHighlightAsync(string elementId, byte[] imageBytes, string mimeType, ResultPoint[] highlightPoints)
    {
        byte[] bytesToDisplay = imageBytes;

        if (highlightPoints != null && highlightPoints.Length >= 2)
        {
            bytesToDisplay = DrawHighlightOnImage(imageBytes, highlightPoints);
        }

        IJSObjectReference module = await GetModuleAsync();
        await module.InvokeVoidAsync("createBlobUrl", elementId, bytesToDisplay, "image/png");
    }

    private static byte[] DrawHighlightOnImage(byte[] imageBytes, ResultPoint[] points)
    {
        SKBitmap originalBitmap = null;

        try
        {
            // Try to detect and handle SVG
            if (IsSvgContent(imageBytes))
            {
                originalBitmap = LoadSvgAsBitmap(imageBytes);
            }
            else
            {
                using MemoryStream ms = new(imageBytes);
                originalBitmap = SKBitmap.Decode(ms);
            }

            if (originalBitmap == null) return imageBytes;

            // Create a mutable copy
            using SKBitmap bitmap = originalBitmap.Copy();
            originalBitmap.Dispose();

            using SKCanvas canvas = new(bitmap);

            // Calculate bounding rectangle from all points
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (ResultPoint point in points)
            {
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
            }

            // Add small padding around the barcode
            float padding = Math.Max(3, Math.Min(bitmap.Width, bitmap.Height) / 50f);
            minX = Math.Max(0, minX - padding);
            minY = Math.Max(0, minY - padding);
            maxX = Math.Min(bitmap.Width, maxX + padding);
            maxY = Math.Min(bitmap.Height, maxY + padding);

            // Create red stroke paint
            using SKPaint paint = new()
            {
                Color = SKColors.Red,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = Math.Max(3, Math.Min(bitmap.Width, bitmap.Height) / 100f),
                IsAntialias = true
            };

            // Draw bounding rectangle
            SKRect rect = new(minX, minY, maxX, maxY);
            canvas.DrawRect(rect, paint);

            // Encode result
            using MemoryStream output = new();
            bitmap.Encode(output, SKEncodedImageFormat.Png, 100);
            return output.ToArray();
        }
        finally
        {
            originalBitmap?.Dispose();
        }
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
            using Svg.Skia.SKSvg svg = new();
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


    public async ValueTask RevokeBlobUrlAsync(string elementId)
    {
        IJSObjectReference module = await GetModuleAsync();
        await module.InvokeVoidAsync("revokeBlobUrl", elementId);
    }

    public async ValueTask RevokeAllBlobUrlsAsync()
    {
        IJSObjectReference module = await GetModuleAsync();
        await module.InvokeVoidAsync("revokeAllBlobUrls");
    }

    public async ValueTask DownloadFileAsync(string fileName, byte[] bytes, string mimeType)
    {
        IJSObjectReference module = await GetModuleAsync();
        await module.InvokeVoidAsync("downloadFileFromBytes", fileName, bytes, mimeType);
    }

    public async ValueTask<bool> InitializeDropzoneAsync(string dropzoneId, string inputId)
    {
        IJSObjectReference module = await GetModuleAsync();
        return await module.InvokeAsync<bool>("initializeDropzone", dropzoneId, inputId);
    }

    public async ValueTask DisposeDropzoneAsync(string dropzoneId)
    {
        IJSObjectReference module = await GetModuleAsync();
        await module.InvokeVoidAsync("disposeDropzone", dropzoneId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}
