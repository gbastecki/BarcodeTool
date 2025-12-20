using BlazorMvvm;
using BarcodeTool.ViewModels;

namespace BarcodeTool.Pages;

public partial class Generator : BlazorMvvmComponentBase<GeneratorViewModel>, IAsyncDisposable
{
    protected override async ValueTask OnDisposeAsync()
    {
        if (BaseViewModel != null)
        {
            await BaseViewModel.DisposeAsync();
        }
    }
}
