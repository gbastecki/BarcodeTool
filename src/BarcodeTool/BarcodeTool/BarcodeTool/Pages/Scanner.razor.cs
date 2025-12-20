using BlazorMvvm;
using BarcodeTool.Services;
using BarcodeTool.ViewModels;
using Microsoft.AspNetCore.Components;

namespace BarcodeTool.Pages;

public partial class Scanner : BlazorMvvmComponentBase<ScannerViewModel>, IAsyncDisposable
{
    private const string DropzoneId = "scanner-dropzone";
    private const string FileInputId = "file-upload";
    private bool _dropzoneInitialized;

    [Inject]
    private IJsInteropService JsInterop { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (!_dropzoneInitialized && BaseViewModel.ScannedFiles.Count == 0 && !BaseViewModel.IsLoading)
        {
            try
            {
                _dropzoneInitialized = await JsInterop.InitializeDropzoneAsync(DropzoneId, FileInputId);
            }
            catch
            {
            }
        }
    }

    protected override async ValueTask OnDisposeAsync()
    {
        if (_dropzoneInitialized)
        {
            try
            {
                await JsInterop.DisposeDropzoneAsync(DropzoneId);
            }
            catch { }
        }

        if (BaseViewModel != null)
        {
            await BaseViewModel.DisposeAsync();
        }
    }
}
