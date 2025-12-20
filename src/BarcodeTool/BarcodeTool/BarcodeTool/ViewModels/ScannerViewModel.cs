using BlazorMvvm;
using BarcodeTool.Models;
using BarcodeTool.Services;
using Microsoft.AspNetCore.Components.Forms;
using ZXing;

namespace BarcodeTool.ViewModels;

[BlazorMvvmViewModel(ViewModelLifetime.Transient)]
public partial class ScannerViewModel(IBarcodeReaderService readerService, IJsInteropService jsInterop) : BlazorViewModel
{
    private CancellationTokenSource _loadingCts;

    [BlazorObservableProperty]
    private List<ScannedFile> _scannedFiles = [];

    [BlazorObservableProperty]
    private BarcodeResultWrapper _selectedBarcode;

    [BlazorObservableProperty]
    private ScannedFile _selectedFile;

    [BlazorObservableProperty]
    private bool _isLoading;

    [BlazorObservableProperty]
    private string _loadingStatus = string.Empty;

    [BlazorObservableProperty]
    private int _loadingProgress;

    [BlazorObservableProperty]
    private int _loadingTotal;

    [BlazorCommand]
    private void CancelLoading()
    {
        _loadingCts?.Cancel();
    }

    [BlazorCommand]
    private async Task LoadFilesAsync(InputFileChangeEventArgs e)
    {
        _loadingCts?.Cancel();
        _loadingCts = new CancellationTokenSource();
        CancellationToken cancellationToken = _loadingCts.Token;

        IsLoading = true;
        List<IBrowserFile> files = e.GetMultipleFiles(maximumFileCount: 100).ToList();
        LoadingTotal = files.Count;
        LoadingProgress = 0;
        LoadingStatus = $"Loading 0/{files.Count} files...";
        OnPropertyChanged();
        await Task.Yield();

        List<ScannedFile> newlyAddedFiles = new();

        foreach (IBrowserFile file in files)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                LoadingStatus = "Cancelled";
                break;
            }

            LoadingProgress++;
            LoadingStatus = $"Loading {LoadingProgress}/{LoadingTotal}: {file.Name}";
            OnPropertyChanged(nameof(LoadingProgress));
            OnPropertyChanged(nameof(LoadingStatus));

            ScannedFile scannedFile = new()
            {
                Name = file.Name,
                ContentType = file.ContentType
            };
            ScannedFiles.Add(scannedFile);
            newlyAddedFiles.Add(scannedFile);
            OnPropertyChanged(nameof(ScannedFiles));

            try
            {
                byte[] imageBytes = await Task.Run(async () =>
                {
                    using Stream stream = file.OpenReadStream(10 * 1024 * 1024);
                    using MemoryStream ms = new();
                    await stream.CopyToAsync(ms, cancellationToken);
                    return ms.ToArray();
                }, cancellationToken);

                scannedFile.ImageBytes = imageBytes;

                await Task.Yield();

                if (cancellationToken.IsCancellationRequested) break;

                List<BarcodeResultWrapper> results = await Task.Run(
                    () => readerService.ReadBarcodesAsync(scannedFile.ImageBytes, file.ContentType),
                    cancellationToken);

                foreach (BarcodeResultWrapper wrapper in results)
                {
                    scannedFile.Barcodes.Add(wrapper);
                    if (SelectedBarcode == null)
                    {
                        SelectedBarcode = wrapper;
                        SelectedFile = scannedFile;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LoadingStatus = "Cancelled";
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning file {file.Name}: {ex.Message}");
            }

            await Task.Yield();
        }

        IsLoading = false;
        LoadingStatus = string.Empty;
        OnPropertyChanged();

        await Task.Yield();
        foreach (ScannedFile file in newlyAddedFiles.Where(f => f.ImageBytes != null))
        {
            if (file == SelectedFile && SelectedBarcode != null)
            {
                await jsInterop.CreateBlobUrlWithHighlightAsync(
                    file.PreviewElementId,
                    file.ImageBytes!,
                    file.ContentType ?? "image/png",
                    SelectedBarcode.Result.ResultPoints);
            }
            else
            {
                await jsInterop.CreateBlobUrlAsync(file.PreviewElementId, file.ImageBytes!, file.ContentType ?? "image/png");
            }
        }
    }


    [BlazorCommand]
    private async Task RemoveFileAsync(ScannedFile file)
    {
        await jsInterop.RevokeBlobUrlAsync(file.PreviewElementId);

        if (SelectedFile == file)
        {
            SelectedFile = null;
            SelectedBarcode = null;
        }
        else if (file.Barcodes.Contains(SelectedBarcode!))
        {
            SelectedBarcode = null;
        }

        ScannedFiles.Remove(file);
        OnPropertyChanged(nameof(ScannedFiles));

        if (SelectedBarcode == null && ScannedFiles.Count != 0)
        {
            ScannedFile firstFileWithBarcodes = ScannedFiles.FirstOrDefault(f => f.Barcodes.Count != 0);
            if (firstFileWithBarcodes != null)
            {
                SelectedFile = firstFileWithBarcodes;
                SelectedBarcode = firstFileWithBarcodes.Barcodes.First();

                OnPropertyChanged();

                await Task.Yield();

                if (SelectedFile?.ImageBytes != null)
                {
                    await jsInterop.CreateBlobUrlWithHighlightAsync(
                        SelectedFile.PreviewElementId,
                        SelectedFile.ImageBytes,
                        SelectedFile.ContentType ?? "image/png",
                        SelectedBarcode.Result.ResultPoints);
                }
            }
        }
    }
    [BlazorCommand]
    private async Task SelectBarcodeAsync(BarcodeResultWrapper barcode, ScannedFile file = null)
    {
        SelectedBarcode = barcode;
        if (file != null)
        {
            SelectedFile = file;
        }
        else
        {
            SelectedFile = ScannedFiles.FirstOrDefault(f => f.Barcodes.Contains(barcode));
        }

        OnPropertyChanged();

        await Task.Yield();

        if (SelectedFile?.ImageBytes != null)
        {
            await jsInterop.CreateBlobUrlWithHighlightAsync(
                SelectedFile.PreviewElementId,
                SelectedFile.ImageBytes,
                SelectedFile.ContentType ?? "image/png",
                barcode.Result.ResultPoints);
        }
    }


    public string GetDimension(BarcodeFormat format) => readerService.GetDimension(format);

    public bool IsGS1(Result result) => readerService.IsGS1(result);

    public static string FormatMetadataValue(object value)
    {
        if (value == null) return "null";

        // Handle byte array lists (BYTE_SEGMENTS)
        if (value is System.Collections.IList list)
        {
            List<string> segments = [];
            foreach (object item in list)
            {
                if (item is byte[] bytes)
                {
                    segments.Add($"[{bytes.Length} bytes: {BitConverter.ToString(bytes).Replace("-", " ")}]");
                }
                else
                {
                    segments.Add(item?.ToString() ?? "null");
                }
            }
            return string.Join(", ", segments);
        }

        if (value is byte[] byteArr)
        {
            return $"[{byteArr.Length} bytes: {BitConverter.ToString(byteArr).Replace("-", " ")}]";
        }

        return value.ToString() ?? "null";
    }

    public static string FormatBinaryBytes(byte[] bytes)
    {
        return string.Join(" ", bytes.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
    }

    public async Task DisposeAsync()
    {
        foreach (ScannedFile file in ScannedFiles)
        {
            await jsInterop.RevokeBlobUrlAsync(file.PreviewElementId);
        }
    }
}
