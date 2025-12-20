using BarcodeTool.Models;
using BarcodeTool.Services;
using ZXing;

namespace BarcodeTool.UnitTests;

/// <summary>
/// Tests for barcode generation with all supported formats and options.
/// </summary>
[TestClass]
public class BarcodeGenerationTests
{
    private readonly BarcodeGeneratorService _generatorService = new();

    #region Basic Generation Tests

    [TestMethod]
    [DataRow(BarcodeFormat.AZTEC, "TestData123")]
    [DataRow(BarcodeFormat.CODABAR, "A123456789A")]
    [DataRow(BarcodeFormat.CODE_39, "TEST123")]
    [DataRow(BarcodeFormat.CODE_93, "TEST123")]
    [DataRow(BarcodeFormat.CODE_128, "Test123")]
    [DataRow(BarcodeFormat.DATA_MATRIX, "TestData123")]
    [DataRow(BarcodeFormat.EAN_8, "12345670")]
    [DataRow(BarcodeFormat.EAN_13, "1234567890128")]
    [DataRow(BarcodeFormat.ITF, "12345678901234")]
    [DataRow(BarcodeFormat.MSI, "123456789012")]
    [DataRow(BarcodeFormat.PDF_417, "TestData123")]
    [DataRow(BarcodeFormat.PLESSEY, "123456789012")]
    [DataRow(BarcodeFormat.QR_CODE, "TestData123")]
    [DataRow(BarcodeFormat.UPC_A, "012345678905")]
    [DataRow(BarcodeFormat.UPC_E, "01234565")]
    public async Task Generate_AllFormats_ProducesNonNullImage(BarcodeFormat format, string content)
    {
        // Arrange
        BarcodeGenerationOptions options = new()
        {
            Format = format,
            Content = content,
            Width = 300,
            Height = 150
        };

        // Act
        BarcodeGenerationResult result = await _generatorService.GenerateAsync(options);

        // Assert
        Assert.IsNull(result.ErrorMessage, $"Error generating {format}: {result.ErrorMessage}");
        Assert.IsNotNull(result.ImageBytes, $"Image bytes should not be null for {format}");
        Assert.IsNotEmpty(result.ImageBytes, $"Image bytes should not be empty for {format}");
    }

    [TestMethod]
    [DataRow(BarcodeFormat.AZTEC)]
    [DataRow(BarcodeFormat.CODE_39)]
    [DataRow(BarcodeFormat.CODE_93)]
    [DataRow(BarcodeFormat.CODE_128)]
    [DataRow(BarcodeFormat.DATA_MATRIX)]
    [DataRow(BarcodeFormat.PDF_417)]
    [DataRow(BarcodeFormat.QR_CODE)]
    public async Task Generate_AllFormats_ProducesSvgContent(BarcodeFormat format)
    {
        // Arrange
        BarcodeGenerationOptions options = new()
        {
            Format = format,
            Content = "Test123",
            Width = 300,
            Height = 150
        };

        // Act
        BarcodeGenerationResult result = await _generatorService.GenerateAsync(options);

        // Assert
        Assert.IsNull(result.ErrorMessage, $"Error generating {format}: {result.ErrorMessage}");
        Assert.IsNotNull(result.SvgContent, $"SVG content should not be null for {format}");
        Assert.Contains("<svg", result.SvgContent, $"SVG content should be valid SVG for {format}");
    }

    [TestMethod]
    public async Task Generate_CODABAR_ProducesSvgContent()
    {
        // CODABAR requires start/stop guards
        BarcodeGenerationOptions options = new()
        {
            Format = BarcodeFormat.CODABAR,
            Content = "A123456A",
            Width = 300,
            Height = 150
        };

        BarcodeGenerationResult result = await _generatorService.GenerateAsync(options);

        Assert.IsNull(result.ErrorMessage, $"Error generating CODABAR: {result.ErrorMessage}");
        Assert.IsNotNull(result.SvgContent);
        Assert.Contains("<svg", result.SvgContent);
    }

    #endregion

    #region Format-Specific Option Tests

    [TestMethod]
    [DataRow("L")]
    [DataRow("M")]
    [DataRow("Q")]
    [DataRow("H")]
    public async Task Generate_QRCode_AllErrorCorrectionLevels(string errorLevel)
    {
        // Arrange
        BarcodeGenerationOptions options = new()
        {
            Format = BarcodeFormat.QR_CODE,
            Content = "TestData",
            Width = 300,
            Height = 300,
            QrErrorCorrectionLevel = errorLevel
        };

        // Act
        BarcodeGenerationResult result = await _generatorService.GenerateAsync(options);

        // Assert
        Assert.IsNull(result.ErrorMessage, $"Error with QR error level {errorLevel}: {result.ErrorMessage}");
        Assert.IsNotNull(result.ImageBytes);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(2)]
    [DataRow(4)]
    [DataRow(8)]
    public async Task Generate_PDF417_AllErrorLevels(int errorLevel)
    {
        // Arrange
        BarcodeGenerationOptions options = new()
        {
            Format = BarcodeFormat.PDF_417,
            Content = "TestData123",
            Width = 400,
            Height = 200,
            Pdf417ErrorLevel = errorLevel
        };

        // Act
        BarcodeGenerationResult result = await _generatorService.GenerateAsync(options);

        // Assert
        Assert.IsNull(result.ErrorMessage, $"Error with PDF417 error level {errorLevel}: {result.ErrorMessage}");
        Assert.IsNotNull(result.ImageBytes);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Generate_PDF417_CompactMode(bool compact)
    {
        // Arrange
        BarcodeGenerationOptions options = new()
        {
            Format = BarcodeFormat.PDF_417,
            Content = "TestData123",
            Width = 400,
            Height = 200,
            Pdf417Compact = compact
        };

        // Act
        BarcodeGenerationResult result = await _generatorService.GenerateAsync(options);

        // Assert
        Assert.IsNull(result.ErrorMessage, $"Error with PDF417 compact={compact}: {result.ErrorMessage}");
        Assert.IsNotNull(result.ImageBytes);
    }

    [TestMethod]
    [DataRow(10)]
    [DataRow(25)]
    [DataRow(50)]
    public async Task Generate_Aztec_ErrorCorrectionPercent(int errorPercent)
    {
        // Arrange
        BarcodeGenerationOptions options = new()
        {
            Format = BarcodeFormat.AZTEC,
            Content = "TestData123",
            Width = 300,
            Height = 300,
            AztecErrorPercent = errorPercent
        };

        // Act
        BarcodeGenerationResult result = await _generatorService.GenerateAsync(options);

        // Assert
        Assert.IsNull(result.ErrorMessage, $"Error with Aztec error percent {errorPercent}: {result.ErrorMessage}");
        Assert.IsNotNull(result.ImageBytes);
    }

    #endregion

    #region GS1 Format Tests

    [TestMethod]
    [DataRow(BarcodeFormat.CODE_128)]
    [DataRow(BarcodeFormat.DATA_MATRIX)]
    [DataRow(BarcodeFormat.QR_CODE)]
    public async Task Generate_GS1Formats_WithGS1Enabled(BarcodeFormat format)
    {
        // Arrange - Use valid GS1 content with Application Identifier
        BarcodeGenerationOptions options = new()
        {
            Format = format,
            Content = "0112345678901234", // AI 01 with GTIN-14
            Width = 300,
            Height = format == BarcodeFormat.CODE_128 ? 100 : 300,
            EnableGS1 = true
        };

        // Act
        BarcodeGenerationResult result = await _generatorService.GenerateAsync(options);

        // Assert
        Assert.IsNull(result.ErrorMessage, $"Error generating GS1 {format}: {result.ErrorMessage}");
        Assert.IsNotNull(result.ImageBytes);
    }

    [TestMethod]
    public void SupportsGS1_ReturnsCorrectValues()
    {
        Assert.IsTrue(_generatorService.SupportsGS1(BarcodeFormat.CODE_128));
        Assert.IsTrue(_generatorService.SupportsGS1(BarcodeFormat.DATA_MATRIX));
        Assert.IsTrue(_generatorService.SupportsGS1(BarcodeFormat.QR_CODE));
        Assert.IsFalse(_generatorService.SupportsGS1(BarcodeFormat.EAN_13));
        Assert.IsFalse(_generatorService.SupportsGS1(BarcodeFormat.PDF_417));
    }

    #endregion

    #region Dimension Tests

    [TestMethod]
    public void Is1DFormat_ReturnsCorrectValues()
    {
        // 2D formats should return false
        Assert.IsFalse(_generatorService.Is1DFormat(BarcodeFormat.QR_CODE));
        Assert.IsFalse(_generatorService.Is1DFormat(BarcodeFormat.DATA_MATRIX));
        Assert.IsFalse(_generatorService.Is1DFormat(BarcodeFormat.AZTEC));
        Assert.IsFalse(_generatorService.Is1DFormat(BarcodeFormat.PDF_417));

        // 1D formats should return true
        Assert.IsTrue(_generatorService.Is1DFormat(BarcodeFormat.CODE_128));
        Assert.IsTrue(_generatorService.Is1DFormat(BarcodeFormat.EAN_13));
        Assert.IsTrue(_generatorService.Is1DFormat(BarcodeFormat.UPC_A));
        Assert.IsTrue(_generatorService.Is1DFormat(BarcodeFormat.MSI));
        Assert.IsTrue(_generatorService.Is1DFormat(BarcodeFormat.PLESSEY));
    }

    #endregion

    #region Margin and Text Tests

    [TestMethod]
    public async Task Generate_1DBarcode_WithTextBelow()
    {
        // Arrange
        BarcodeGenerationOptions options = new()
        {
            Format = BarcodeFormat.CODE_128,
            Content = "TEST123",
            Width = 300,
            Height = 100,
            ShowTextBelow = true,
            FontSize = 14
        };

        // Act
        BarcodeGenerationResult result = await _generatorService.GenerateAsync(options);

        // Assert
        Assert.IsNull(result.ErrorMessage);
        Assert.IsNotNull(result.ImageBytes);
        Assert.IsNotNull(result.SvgContent);
        Assert.Contains("TEST123", result.SvgContent, "SVG should contain the barcode text");
    }

    [TestMethod]
    public async Task Generate_WithCustomMargins()
    {
        // Arrange
        BarcodeGenerationOptions options = new()
        {
            Format = BarcodeFormat.QR_CODE,
            Content = "Test",
            Width = 200,
            Height = 200,
            MarginTop = 20,
            MarginBottom = 20,
            MarginLeft = 20,
            MarginRight = 20
        };

        // Act
        BarcodeGenerationResult result = await _generatorService.GenerateAsync(options);

        // Assert
        Assert.IsNull(result.ErrorMessage);
        Assert.IsNotNull(result.ImageBytes);
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    public async Task Generate_EmptyContent_ReturnsError()
    {
        // Arrange
        BarcodeGenerationOptions options = new()
        {
            Format = BarcodeFormat.QR_CODE,
            Content = "",
            Width = 200,
            Height = 200
        };

        // Act
        BarcodeGenerationResult result = await _generatorService.GenerateAsync(options);

        // Assert
        Assert.IsNotNull(result.ErrorMessage);
        Assert.IsNull(result.ImageBytes);
    }

    [TestMethod]
    public async Task Generate_InvalidContentForFormat_ReturnsError()
    {
        // Arrange - EAN-13 requires exactly 13 numeric digits
        BarcodeGenerationOptions options = new()
        {
            Format = BarcodeFormat.EAN_13,
            Content = "INVALID",
            Width = 300,
            Height = 100
        };

        // Act
        BarcodeGenerationResult result = await _generatorService.GenerateAsync(options);

        // Assert
        Assert.IsNotNull(result.ErrorMessage);
    }

    #endregion
}
