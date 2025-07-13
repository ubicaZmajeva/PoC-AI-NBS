using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiComplaintAssistant.Api.Extensions;
using Microsoft.SemanticKernel;

internal static class SemanticKernelExtensions
{
    internal static IServiceCollection RegisterSemanticKernel(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<Kernel>(sp =>
        {
            var apiKey = configuration["AI:ApiKey"];
            var endpoint = configuration["Ai:Endpoint"];
            var deploymentName = configuration["AI:DeploymentName"];

            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);
            return kernelBuilder.Build();
        });
        return services;
    }
}
