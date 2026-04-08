using Azure;
using Azure.AI.ContentUnderstanding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TranscriptAnalyzer.POC.Application.Interfaces.Repositories;
using TranscriptAnalyzer.POC.Application.Interfaces.Services;
using TranscriptAnalyzer.POC.Application.Services;
using TranscriptAnalyzer.POC.Infrastructure.Repositories;

namespace TranscriptAnalyzer.POC.Infrastructure.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {

            services
                .AddServices()
                .AddRepositories()
                .AddAzureBlobStorageClient()
                .AddContentUnderstandingClient();

            return services;

        }


        private static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddSingleton<IBlobStorageService, BlobStorageService>();
            services.AddSingleton<ITranscriptAnalyzerService, TranscriptAnalyzerService>();
            return services;
        }

        private static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddSingleton<IStorageAccountRepository, StorageAccountRepository>();
            services.AddSingleton<IContentUnderstandingRepository, ContentUnderstandingRepository>();
            return services;
        }

        private static IServiceCollection AddAzureBlobStorageClient(this IServiceCollection services)
        {
            var blobServiceClient = new Azure.Storage.Blobs.BlobServiceClient(Environment.GetEnvironmentVariable("AzureBlobStorage"));
            services.AddSingleton(blobServiceClient);

            return services;
        }

        private static IServiceCollection AddContentUnderstandingClient(this IServiceCollection services)
        {
            string contentUnderstandingEndpoint = Environment.GetEnvironmentVariable("CONTENTUNDERSTANDING_ENDPOINT") ?? throw new Exception("CONTENTUNDERSTANDING_ENDPOINT NOT FOUND");
            string contentUnderstandingKey = Environment.GetEnvironmentVariable("CONTENTUNDERSTANDING_KEY") ?? throw new Exception("CONTENTUNDERSTANDING_KEY NOT FOUND");
            var contentUnderstandingClient = new ContentUnderstandingClient(new Uri(contentUnderstandingEndpoint), new AzureKeyCredential(contentUnderstandingKey));

            services.AddSingleton(contentUnderstandingClient);
            return services;
        }


    }
}
