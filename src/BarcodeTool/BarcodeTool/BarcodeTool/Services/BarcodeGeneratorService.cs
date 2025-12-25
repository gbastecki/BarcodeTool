using BarcodeTool.Models;
using SkiaSharp;
using System.Text;
using ZXing;
using ZXing.Common;
using ZXing.PDF417.Internal;
using ZXing.QrCode.Internal;
using ZXing.SkiaSharp;

namespace BarcodeTool.Services;

public interface IBarcodeGeneratorService
{
    /// <summary>
    /// Generates a barcode as PNG bytes and SVG string.
    /// </summary>
    Task<BarcodeGenerationResult> GenerateAsync(BarcodeGenerationOptions options);

    /// <summary>
    /// Gets the list of supported barcode formats for generation.
    /// </summary>
    IReadOnlyList<BarcodeFormat> SupportedFormats { get; }

    /// <summary>
    /// Checks if a format supports GS1 encoding.
    /// </summary>
    bool SupportsGS1(BarcodeFormat format);

    /// <summary>
    /// Checks if a format is a 1D barcode.
    /// </summary>
    bool Is1DFormat(BarcodeFormat format);
}

public class BarcodeGeneratorService : IBarcodeGeneratorService
{
    private static readonly BarcodeFormat[] _supportedFormats =
    [
        BarcodeFormat.AZTEC,
        BarcodeFormat.CODABAR,
        BarcodeFormat.CODE_39,
        BarcodeFormat.CODE_93,
        BarcodeFormat.CODE_128,
        BarcodeFormat.DATA_MATRIX,
        BarcodeFormat.EAN_8,
        BarcodeFormat.EAN_13,
        BarcodeFormat.ITF,
        BarcodeFormat.MSI,
        BarcodeFormat.PDF_417,
        BarcodeFormat.PLESSEY,
        BarcodeFormat.QR_CODE,
        BarcodeFormat.UPC_A,
        BarcodeFormat.UPC_E,
    ];

    public IReadOnlyList<BarcodeFormat> SupportedFormats => _supportedFormats;

    public bool SupportsGS1(BarcodeFormat format)
    {
        return format switch
        {
            BarcodeFormat.CODE_128 => true,
            BarcodeFormat.DATA_MATRIX => true,
            BarcodeFormat.QR_CODE => true,
            _ => false
        };
    }

    public bool Is1DFormat(BarcodeFormat format)
    {
        return format switch
        {
            BarcodeFormat.QR_CODE or BarcodeFormat.DATA_MATRIX or BarcodeFormat.AZTEC or BarcodeFormat.PDF_417 or BarcodeFormat.MAXICODE => false,
            _ => true
        };
    }

    /// <summary>
    /// Gets the display text for a barcode, including auto-calculated check digits for EAN/UPC formats.
    /// </summary>
    private static string GetDisplayText(string content, BarcodeFormat format)
    {
        return format switch
        {
            BarcodeFormat.EAN_8 when content.Length == 7 => content + CalculateEanCheckDigit(content),
            BarcodeFormat.EAN_13 when content.Length == 12 => content + CalculateEanCheckDigit(content),
            BarcodeFormat.UPC_A when content.Length == 11 => content + CalculateEanCheckDigit(content),
            BarcodeFormat.UPC_E when content.Length == 7 => content + CalculateUpcECheckDigit(content),
            _ => content
        };
    }

    /// <summary>
    /// Calculates the check digit for EAN-8, EAN-13, and UPC-A barcodes.
    /// </summary>
    private static char CalculateEanCheckDigit(string digits)
    {
        int sum = 0;
        bool isOdd = true;

        for (int i = digits.Length - 1; i >= 0; i--)
        {
            int digit = digits[i] - '0';
            sum += isOdd ? digit * 3 : digit;
            isOdd = !isOdd;
        }

        int checkDigit = (10 - (sum % 10)) % 10;
        return (char)('0' + checkDigit);
    }

    /// <summary>
    /// Calculates the check digit for UPC-E barcodes.
    /// </summary>
    private static char CalculateUpcECheckDigit(string digits)
    {
        string expanded = ExpandUpcE(digits);
        return CalculateEanCheckDigit(expanded);
    }

    /// <summary>
    /// Expands a 7-digit UPC-E code to its 11-digit UPC-A equivalent (without check digit).
    /// </summary>
    private static string ExpandUpcE(string upcE)
    {
        if (upcE.Length != 7)
        {
            return upcE;
        }

        char lastDigit = upcE[6];
        string manufacturer;
        string product;

        switch (lastDigit)
        {
            case '0':
            case '1':
            case '2':
                manufacturer = upcE.Substring(1, 2) + lastDigit + "00";
                product = string.Concat("00", upcE.AsSpan(3, 3));
                break;
            case '3':
                manufacturer = upcE.Substring(1, 3) + "00";
                product = string.Concat("000", upcE.AsSpan(4, 2));
                break;
            case '4':
                manufacturer = upcE.Substring(1, 4) + "0";
                product = "0000" + upcE[5];
                break;
            default: // 5-9
                manufacturer = upcE.Substring(1, 5);
                product = "0000" + lastDigit;
                break;
        }

        return upcE[0] + manufacturer + product;
    }

    public Task<BarcodeGenerationResult> GenerateAsync(BarcodeGenerationOptions options)
    {
        return Task.Run(() => GenerateInternal(options));
    }

    private BarcodeGenerationResult GenerateInternal(BarcodeGenerationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Content))
        {
            return new BarcodeGenerationResult { ErrorMessage = "Content cannot be empty." };
        }

        try
        {
            EncodingOptions encodingOptions = CreateEncodingOptions(options);
            BarcodeWriter writer = new()
            {
                Format = options.Format,
                Options = encodingOptions
            };

            using SKBitmap barcodeBitmap = writer.Write(options.Content);

            int textHeight = Is1DFormat(options.Format) && options.ShowTextBelow ? options.FontSize + 8 : 0;

            // Calculate final dimensions with custom margins
            int finalWidth = barcodeBitmap.Width + options.MarginLeft + options.MarginRight;
            int finalHeight = barcodeBitmap.Height + options.MarginTop + options.MarginBottom + textHeight;

            byte[] imageBytes;
            using (SKBitmap finalBitmap = new(finalWidth, finalHeight))
            {
                using SKCanvas canvas = new(finalBitmap);
                canvas.Clear(SKColors.White);

                // Draw barcode with margins
                canvas.DrawBitmap(barcodeBitmap, options.MarginLeft, options.MarginTop);

                // Draw text if enabled for 1D barcodes
                if (Is1DFormat(options.Format) && options.ShowTextBelow)
                {
                    using SKTypeface typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Normal);
                    using SKFont font = new(typeface, options.FontSize);
                    using SKPaint paint = new()
                    {
                        Color = SKColors.Black,
                        IsAntialias = true
                    };

                    float textX = options.MarginLeft + barcodeBitmap.Width / 2f;
                    float textY = options.MarginTop + barcodeBitmap.Height + options.FontSize;
                    string displayText = GetDisplayText(options.Content, options.Format);
                    canvas.DrawText(displayText, textX, textY, SKTextAlign.Center, font, paint);
                }

                using SKImage image = SKImage.FromBitmap(finalBitmap);
                using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
                imageBytes = data.ToArray();
            }

            // Generate SVG
            string svgContent = GenerateSvg(options, encodingOptions, textHeight);

            return new BarcodeGenerationResult
            {
                ImageBytes = imageBytes,
                SvgContent = svgContent
            };
        }
        catch (Exception ex)
        {
            return new BarcodeGenerationResult
            {
                ErrorMessage = $"Error: {ex.Message}. Check if content is valid for the selected format."
            };
        }
    }

    private string GenerateSvg(BarcodeGenerationOptions options, EncodingOptions encodingOptions, int textHeight)
    {
        try
        {
            BarcodeWriterGeneric writer = new()
            {
                Format = options.Format,
                Options = encodingOptions
            };

            BitMatrix bitMatrix = writer.Encode(options.Content);
            return RenderBitMatrixToSvg(bitMatrix, options, textHeight);
        }
        catch
        {
            return null;
        }
    }

    private string RenderBitMatrixToSvg(BitMatrix matrix, BarcodeGenerationOptions options, int textHeight)
    {
        StringBuilder sb = new();
        int matrixWidth = matrix.Width;
        int matrixHeight = matrix.Height;

        // Final dimensions with margins
        int totalWidth = matrixWidth + options.MarginLeft + options.MarginRight;
        int totalHeight = matrixHeight + options.MarginTop + options.MarginBottom + textHeight;

        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {totalWidth} {totalHeight}\" width=\"{totalWidth}\" height=\"{totalHeight}\" shape-rendering=\"crispEdges\">");
        sb.AppendLine($"<rect width=\"{totalWidth}\" height=\"{totalHeight}\" fill=\"white\"/>");

        // Draw barcode with offset for margins
        for (int y = 0; y < matrixHeight; y++)
        {
            int x = 0;
            while (x < matrixWidth)
            {
                if (matrix[x, y])
                {
                    int startX = x;
                    int startY = y;
                    while (x < matrixWidth && matrix[x, y]) x++;
                    int rectWidth = x - startX;

                    int rectHeight = 1;
                    bool canExtend = true;
                    while (canExtend && startY + rectHeight < matrixHeight)
                    {
                        for (int checkX = startX; checkX < startX + rectWidth; checkX++)
                        {
                            if (!matrix[checkX, startY + rectHeight])
                            {
                                canExtend = false;
                                break;
                            }
                        }
                        if (canExtend)
                        {
                            for (int clearX = startX; clearX < startX + rectWidth; clearX++)
                            {
                                matrix[clearX, startY + rectHeight] = false;
                            }
                            rectHeight++;
                        }
                    }

                    // Add margin offset
                    int drawX = startX + options.MarginLeft;
                    int drawY = startY + options.MarginTop;
                    sb.AppendLine($"<rect x=\"{drawX}\" y=\"{drawY}\" width=\"{rectWidth}\" height=\"{rectHeight}\" fill=\"black\"/>");
                }
                else
                {
                    x++;
                }
            }
        }

        // Draw text with margin offset
        if (Is1DFormat(options.Format) && options.ShowTextBelow && textHeight > 0)
        {
            int textX = options.MarginLeft + matrixWidth / 2;
            int textY = options.MarginTop + matrixHeight + options.FontSize;
            string displayText = GetDisplayText(options.Content, options.Format);
            sb.AppendLine($"<text x=\"{textX}\" y=\"{textY}\" text-anchor=\"middle\" font-family=\"Consolas, monospace\" font-size=\"{options.FontSize}\" fill=\"black\">{System.Security.SecurityElement.Escape(displayText)}</text>");
        }

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static EncodingOptions CreateEncodingOptions(BarcodeGenerationOptions options)
    {
        EncodingOptions encodingOptions;

        switch (options.Format)
        {
            case BarcodeFormat.QR_CODE:
                ErrorCorrectionLevel qrLevel = options.QrErrorCorrectionLevel switch
                {
                    "L" => ErrorCorrectionLevel.L,
                    "M" => ErrorCorrectionLevel.M,
                    "Q" => ErrorCorrectionLevel.Q,
                    "H" => ErrorCorrectionLevel.H,
                    _ => ErrorCorrectionLevel.M
                };
                ZXing.QrCode.QrCodeEncodingOptions qrOptions = new()
                {
                    ErrorCorrection = qrLevel,
                    Width = options.Width,
                    Height = options.Height,
                    Margin = 0
                };
                if (options.EnableGS1)
                {
                    qrOptions.Hints[EncodeHintType.GS1_FORMAT] = true;
                }
                encodingOptions = qrOptions;
                break;

            case BarcodeFormat.DATA_MATRIX:
                ZXing.Datamatrix.DatamatrixEncodingOptions dmOptions = new()
                {
                    Width = options.Width,
                    Height = options.Height,
                    Margin = 0,
                    GS1Format = options.EnableGS1
                };
                encodingOptions = dmOptions;
                break;

            case BarcodeFormat.CODE_128:
                encodingOptions = new EncodingOptions
                {
                    Width = options.Width,
                    Height = options.Height,
                    Margin = 0,
                    PureBarcode = true
                };
                if (options.EnableGS1)
                {
                    encodingOptions.Hints[EncodeHintType.GS1_FORMAT] = true;
                }
                break;

            case BarcodeFormat.PDF_417:
                encodingOptions = new ZXing.PDF417.PDF417EncodingOptions
                {
                    Width = options.Width,
                    Height = options.Height,
                    Margin = 1,
                    ErrorCorrection = (PDF417ErrorCorrectionLevel)options.Pdf417ErrorLevel,
                    Compact = options.Pdf417Compact
                };
                break;

            case BarcodeFormat.AZTEC:
                encodingOptions = new ZXing.Aztec.AztecEncodingOptions
                {
                    Width = options.Width,
                    Height = options.Height,
                    Margin = 0,
                    ErrorCorrection = options.AztecErrorPercent
                };
                break;

            case BarcodeFormat.MSI:
                encodingOptions = new EncodingOptions
                {
                    Width = options.Width,
                    Height = options.Height,
                    Margin = 1
                };
                break;

            case BarcodeFormat.PLESSEY:
                encodingOptions = new EncodingOptions
                {
                    Width = options.Width,
                    Height = options.Height,
                    Margin = 1
                };
                break;

            default:
                encodingOptions = new EncodingOptions
                {
                    Width = options.Width,
                    Height = options.Height,
                    Margin = 0,
                    PureBarcode = true
                };
                break;
        }

        return encodingOptions;
    }
}
