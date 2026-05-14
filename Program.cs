using Brease.Core;
using Brease.Core.Models;
using Brease.Core.Readers;
using CBreaseWebApp1;
using CBreaseWebApp1.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<IBreaseProjectLoader, CbzBreaseProjectLoader>();
builder.Services.AddScoped<ProjectState>();
builder.Services.AddScoped<CbzFileService>();
builder.Services.AddScoped<DraftStorageService>();
await builder.Build().RunAsync();