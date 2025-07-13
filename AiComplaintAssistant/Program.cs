using System;
using System.Net.Http;
using System.Runtime.InteropServices.JavaScript;
using AiComplaintAssistant.Options;
using AiComplaintAssistant.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

builder.Services.Configure<AppSettings>(
builder.Configuration.GetSection(nameof(AppSettings)));

builder.Services.AddHttpClient<AiComplaintAssistantService>((sp, client) => {
    var appSettings = sp.GetRequiredService<IOptions<AppSettings>>();
    if (string.IsNullOrEmpty(appSettings.Value.BackendUri))
        return;
    client.BaseAddress = new Uri(appSettings.Value.BackendUri);
});

await JSHost.ImportAsync(
    nameof(JavaScriptModule),
    $"../js/iframe.js?{Guid.NewGuid()}" /* cache bust */);

await builder.Build().RunAsync();
