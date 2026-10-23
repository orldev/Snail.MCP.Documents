using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snail.MCP.Documents.Adapters;
using Snail.MCP.Documents.Application;

namespace Snail.MCP.Documents.Extensions;

/// <summary>Driven adapters and use cases: where a document comes from, and the readers of Snail.Toolkit.Documents that read it.</summary>
public static class ReadingRegistrationExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Everything a session needs to reach and read the documents of its copy.</summary>
        public IServiceCollection AddReading(IConfiguration settings)
        {
            services.AddSingleton(settings);
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<IDisk, LocalDisk>();
            services.AddSingleton<DocumentMemory>();
            services.AddScoped<SessionRoots>();
            services.AddScoped<DocumentOpening>();
            services.AddScoped<DocumentReading>();
            services.AddScoped<DocumentOutline>();
            services.AddScoped<TableReading>();

            return services.AddDocumentReading();
        }
    }
}
