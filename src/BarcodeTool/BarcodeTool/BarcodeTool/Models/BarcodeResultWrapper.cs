namespace BarcodeTool.Models;

using ZXing;

/// <summary>
/// Wrapper for a ZXing barcode scan result.
/// </summary>
public class BarcodeResultWrapper
{
    public BarcodeResultWrapper(Result result) => Result = result;

    public Result Result { get; }
}
