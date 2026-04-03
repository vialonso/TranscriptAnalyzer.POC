using Azure;
using Azure.AI.ContentUnderstanding;
using TranscriptAnalyzer.POC.Application.Interfaces.Repositories;
using TranscriptAnalyzer.POC.Application.Interfaces.Services;
using TranscriptAnalyzer.POC.Application.Services;
using TranscriptAnalyzer.POC.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
builder.Services.AddSingleton<IStorageAccountRepository, StorageAccountRepository>();
builder.Services.AddSingleton<IContentUnderstandingRepository, ContentUnderstandingRepository>();

var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(Environment.GetEnvironmentVariable("AzureBlobStorage"));
builder.Services.AddSingleton(blobServiceClient);

string contentUnderstandingEndpoint = Environment.GetEnvironmentVariable("CONTENTUNDERSTANDING_ENDPOINT") ?? throw new Exception("CONTENTUNDERSTANDING_ENDPOINT NOT FOUND");
string contentUnderstandingKey = Environment.GetEnvironmentVariable("CONTENTUNDERSTANDING_KEY") ?? throw new Exception("CONTENTUNDERSTANDING_KEY NOT FOUND");
var contentUnderstandingClient = new ContentUnderstandingClient(new Uri(contentUnderstandingEndpoint), new AzureKeyCredential(contentUnderstandingKey));

builder.Services.AddSingleton(contentUnderstandingClient);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
