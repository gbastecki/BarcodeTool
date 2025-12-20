using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BarcodeTool;
using BlazorMvvm;
using BarcodeTool.Services;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IBarcodeGeneratorService, BarcodeGeneratorService>();
builder.Services.AddScoped<IBarcodeReaderService, BarcodeReaderService>();
builder.Services.AddScoped<IJsInteropService, JsInteropService>();
builder.Services.UseBlazorMvvmViewModelFactory();

await builder.Build().RunAsync();

