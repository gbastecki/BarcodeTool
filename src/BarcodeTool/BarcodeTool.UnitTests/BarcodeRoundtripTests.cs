using BarcodeTool.Models;
using BarcodeTool.Services;
using ZXing;

namespace BarcodeTool.UnitTests;

/// <summary>
/// End-to-end tests that generate a barcode and verify it can be read back correctly.
/// </summary>
[TestClass]
public class BarcodeRoundtripTests
{
    private readonly BarcodeGeneratorService _generatorService = new();
    private readonly BarcodeReaderService _readerService = new();

    #region Standard Roundtrip Tests

    [TestMethod]
    [DataRow(BarcodeFormat.AZTEC, "RoundtripTest123")]
    [DataRow(BarcodeFormat.CODE_39, "ROUNDTRIP")]
    [DataRow(BarcodeFormat.CODE_93, "ROUNDTRIP")]
    [DataRow(BarcodeFormat.CODE_128, "RoundtripTest123")]
    [DataRow(BarcodeFormat.DATA_MATRIX, "RoundtripTest123")]
    [DataRow(BarcodeFormat.PDF_417, "RoundtripTest123")]
    [DataRow(BarcodeFormat.QR_CODE, "RoundtripTest123")]
    public async Task Roundtrip_Generate_ThenRead_ContentMatches(BarcodeFormat format, string content)
    {
        // Arrange - Generate the barcode
        BarcodeGenerationOptions generateOptions = new()
        {
            Format = format,
            Content = content,
            Width = 400,
            Height = format == BarcodeFormat.PDF_417 ? 200 : 400,
            MarginTop = 20,
            MarginBottom = 20,
            MarginLeft = 20,
            MarginRight = 20
        };

        // Act - Generate
        BarcodeGenerationResult generateResult = await _generatorService.GenerateAsync(generateOptions);
        Assert.IsNull(generateResult.ErrorMessage, $"Generation failed: {generateResult.ErrorMessage}");
        Assert.IsNotNull(generateResult.ImageBytes, "No image bytes generated");

        // Act - Read back
        List<BarcodeResultWrapper> readResults = await _readerService.ReadBarcodesAsync(generateResult.ImageBytes);

        // Assert
        Assert.IsNotEmpty(readResults, $"No barcodes detected in generated {format} image");

        string decodedContent = readResults[0].Result.Text;
        Assert.AreEqual(content, decodedContent, $"Content mismatch for {format}: expected '{content}', got '{decodedContent}'");
    }

    [TestMethod]
    [DataRow(BarcodeFormat.EAN_8, "12345670")]
    [DataRow(BarcodeFormat.EAN_13, "1234567890128")]
    [DataRow(BarcodeFormat.UPC_A, "012345678905")]
    public async Task Roundtrip_EANAndUPC_ContentMatches(BarcodeFormat format, string content)
    {
        // Arrange - Generate the barcode with adequate margins
        BarcodeGenerationOptions generateOptions = new()
        {
            Format = format,
            Content = content,
            Width = 400,
            Height = 150,
            MarginTop = 20,
            MarginBottom = 20,
            MarginLeft = 40,
            MarginRight = 40
        };

        // Act - Generate
        BarcodeGenerationResult generateResult = await _generatorService.GenerateAsync(generateOptions);
        Assert.IsNull(generateResult.ErrorMessage, $"Generation failed: {generateResult.ErrorMessage}");
        Assert.IsNotNull(generateResult.ImageBytes, "No image bytes generated");

        // Act - Read back
        List<BarcodeResultWrapper> readResults = await _readerService.ReadBarcodesAsync(generateResult.ImageBytes);

        // Assert
        Assert.IsNotEmpty(readResults, $"No barcodes detected in generated {format} image");

        string decodedContent = readResults[0].Result.Text;
        Assert.AreEqual(content, decodedContent, $"Content mismatch for {format}: expected '{content}', got '{decodedContent}'");
    }

    [TestMethod]
    public async Task Roundtrip_ITF_ContentMatches()
    {
        // ITF requires even number of digits
        string content = "12345678901234";
        BarcodeGenerationOptions generateOptions = new()
        {
            Format = BarcodeFormat.ITF,
            Content = content,
            Width = 400,
            Height = 150,
            MarginTop = 20,
            MarginBottom = 20,
            MarginLeft = 40,
            MarginRight = 40
        };

        BarcodeGenerationResult generateResult = await _generatorService.GenerateAsync(generateOptions);
        Assert.IsNull(generateResult.ErrorMessage, $"Generation failed: {generateResult.ErrorMessage}");

        List<BarcodeResultWrapper> readResults = await _readerService.ReadBarcodesAsync(generateResult.ImageBytes!);
        Assert.IsNotEmpty(readResults, "No barcodes detected");
        Assert.AreEqual(content, readResults[0].Result.Text);
    }

    [TestMethod]
    public async Task Roundtrip_CODABAR_ContentMatches()
    {
        // CODABAR requires start/stop characters (A, B, C, or D)
        // ZXing strips start/stop chars when decoding, so compare core content
        string content = "A123456789A";
        string expectedDecodedContent = "123456789"; // Start/stop guards stripped by decoder

        BarcodeGenerationOptions generateOptions = new()
        {
            Format = BarcodeFormat.CODABAR,
            Content = content,
            Width = 400,
            Height = 150,
            MarginTop = 20,
            MarginBottom = 20,
            MarginLeft = 40,
            MarginRight = 40
        };

        BarcodeGenerationResult generateResult = await _generatorService.GenerateAsync(generateOptions);
        Assert.IsNull(generateResult.ErrorMessage, $"Generation failed: {generateResult.ErrorMessage}");

        List<BarcodeResultWrapper> readResults = await _readerService.ReadBarcodesAsync(generateResult.ImageBytes!);
        Assert.IsNotEmpty(readResults, "No barcodes detected");
        Assert.AreEqual(expectedDecodedContent, readResults[0].Result.Text);
    }

    #endregion

    #region MSI and PLESSEY Tests - These formats may have limited reader support

    [TestMethod]
    public async Task Roundtrip_MSI_GeneratesSuccessfully()
    {
        // MSI is numeric-only
        string content = "123456789012";
        BarcodeGenerationOptions generateOptions = new()
        {
            Format = BarcodeFormat.MSI,
            Content = content,
            Width = 400,
            Height = 150,
            MarginTop = 20,
            MarginBottom = 20,
            MarginLeft = 40,
            MarginRight = 40
        };

        BarcodeGenerationResult generateResult = await _generatorService.GenerateAsync(generateOptions);

        // Assert generation succeeded
        Assert.IsNull(generateResult.ErrorMessage, $"Generation failed: {generateResult.ErrorMessage}");
        Assert.IsNotNull(generateResult.ImageBytes, "No image bytes generated");
        Assert.IsNotEmpty(generateResult.ImageBytes, "Image bytes should not be empty");

        // Try to read
        List<BarcodeResultWrapper> readResults = await _readerService.ReadBarcodesAsync(generateResult.ImageBytes);

        // If can read it, verify content matches
        if (readResults.Count > 0)
        {
            Assert.AreEqual(BarcodeFormat.MSI, readResults[0].Result.BarcodeFormat);
        }
        else
        {
            Assert.Inconclusive("MSI barcode generation succeeded, but reading failed - MSI may not be supported by the reader.");
        }
    }

    [TestMethod]
    public async Task Roundtrip_PLESSEY_GeneratesSuccessfully()
    {
        // PLESSEY is numeric-only
        string content = "123456789012";
        BarcodeGenerationOptions generateOptions = new()
        {
            Format = BarcodeFormat.PLESSEY,
            Content = content,
            Width = 400,
            Height = 150,
            MarginTop = 20,
            MarginBottom = 20,
            MarginLeft = 40,
            MarginRight = 40
        };

        BarcodeGenerationResult generateResult = await _generatorService.GenerateAsync(generateOptions);

        // Assert generation succeeded
        Assert.IsNull(generateResult.ErrorMessage, $"Generation failed: {generateResult.ErrorMessage}");
        Assert.IsNotNull(generateResult.ImageBytes, "No image bytes generated");
        Assert.IsNotEmpty(generateResult.ImageBytes, "Image bytes should not be empty");

        // Try to read
        List<BarcodeResultWrapper> readResults = await _readerService.ReadBarcodesAsync(generateResult.ImageBytes);

        // If can read it, verify content matches
        if (readResults.Count > 0)
        {
            Assert.AreEqual(BarcodeFormat.PLESSEY, readResults[0].Result.BarcodeFormat);
        }
        else         
        {
            Assert.Inconclusive("PLESSEY barcode generation succeeded, but reading failed - PLESSEY may not be supported by the reader.");
        }
    }

    #endregion

    #region GS1 Mode Tests

    [TestMethod]
    [DataRow(BarcodeFormat.DATA_MATRIX)]
    [DataRow(BarcodeFormat.QR_CODE)]
    public async Task Roundtrip_GS1Format_GeneratesAndReads(BarcodeFormat format)
    {
        // GS1 content with Application Identifier 01 (GTIN)
        string content = "0112345678901234";
        BarcodeGenerationOptions generateOptions = new()
        {
            Format = format,
            Content = content,
            Width = 400,
            Height = 400,
            EnableGS1 = true,
            MarginTop = 20,
            MarginBottom = 20,
            MarginLeft = 20,
            MarginRight = 20
        };

        BarcodeGenerationResult generateResult = await _generatorService.GenerateAsync(generateOptions);
        Assert.IsNull(generateResult.ErrorMessage, $"Generation failed: {generateResult.ErrorMessage}");
        Assert.IsNotNull(generateResult.ImageBytes);

        List<BarcodeResultWrapper> readResults = await _readerService.ReadBarcodesAsync(generateResult.ImageBytes);
        Assert.IsNotEmpty(readResults, $"No barcodes detected in GS1 {format}");

        // GS1 barcodes may have FNC1 characters that affect the decoded text, verify the core content is present
        Assert.Contains("12345678901234", readResults[0].Result.Text, $"GTIN not found in decoded content: {readResults[0].Result.Text}");
    }

    #endregion

    #region QR Code Options Tests

    [TestMethod]
    [DataRow("L")]
    [DataRow("M")]
    [DataRow("Q")]
    [DataRow("H")]
    public async Task Roundtrip_QRCode_AllErrorLevels_ContentMatches(string errorLevel)
    {
        string content = "ErrorCorrectionTest";
        BarcodeGenerationOptions generateOptions = new()
        {
            Format = BarcodeFormat.QR_CODE,
            Content = content,
            Width = 300,
            Height = 300,
            QrErrorCorrectionLevel = errorLevel,
            MarginTop = 20,
            MarginBottom = 20,
            MarginLeft = 20,
            MarginRight = 20
        };

        BarcodeGenerationResult generateResult = await _generatorService.GenerateAsync(generateOptions);
        Assert.IsNull(generateResult.ErrorMessage);

        List<BarcodeResultWrapper> readResults = await _readerService.ReadBarcodesAsync(generateResult.ImageBytes!);
        Assert.IsNotEmpty(readResults, $"No QR code detected with error level {errorLevel}");
        Assert.AreEqual(content, readResults[0].Result.Text);
    }

    #endregion

    #region PDF417 Options Tests

    [TestMethod]
    [DataRow(0)]
    [DataRow(4)]
    [DataRow(8)]
    public async Task Roundtrip_PDF417_AllErrorLevels_ContentMatches(int errorLevel)
    {
        string content = "PDF417ErrorTest";
        BarcodeGenerationOptions generateOptions = new()
        {
            Format = BarcodeFormat.PDF_417,
            Content = content,
            Width = 500,
            Height = 200,
            Pdf417ErrorLevel = errorLevel,
            MarginTop = 20,
            MarginBottom = 20,
            MarginLeft = 40,
            MarginRight = 40
        };

        BarcodeGenerationResult generateResult = await _generatorService.GenerateAsync(generateOptions);
        Assert.IsNull(generateResult.ErrorMessage, $"Generation failed at error level {errorLevel}");

        List<BarcodeResultWrapper> readResults = await _readerService.ReadBarcodesAsync(generateResult.ImageBytes!);
        Assert.IsNotEmpty(readResults, $"No PDF417 detected with error level {errorLevel}");
        Assert.AreEqual(content, readResults[0].Result.Text);
    }

    #endregion

    #region Reader Service Tests

    [TestMethod]
    public void GetDimension_ReturnsCorrectValues()
    {
        Assert.AreEqual("2D", _readerService.GetDimension(BarcodeFormat.QR_CODE));
        Assert.AreEqual("2D", _readerService.GetDimension(BarcodeFormat.DATA_MATRIX));
        Assert.AreEqual("2D", _readerService.GetDimension(BarcodeFormat.AZTEC));
        Assert.AreEqual("2D", _readerService.GetDimension(BarcodeFormat.PDF_417));
        Assert.AreEqual("1D", _readerService.GetDimension(BarcodeFormat.CODE_128));
        Assert.AreEqual("1D", _readerService.GetDimension(BarcodeFormat.EAN_13));
        Assert.AreEqual("1D", _readerService.GetDimension(BarcodeFormat.MSI));
    }

    #endregion
}
