using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snail.MCP.Documents.Adapters;
using Snail.MCP.Documents.Application;

namespace Snail.MCP.Documents.Extensions;

/// <summary>Driven adapters and use cases: where a document comes from and how each format is read.</summary>
public static class ReadingRegistrationExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Everything a session needs to reach and read the documents of its copy.</summary>
        public IServiceCollection AddReading(IConfiguration settings)
        {
            services.AddSingleton(settings);
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<DocumentMemory>();
            services.AddScoped<SessionRoots>();
            services.AddScoped<DocumentOpening>();
            services.AddScoped<DocumentReading>();
            services.AddScoped<DocumentOutline>();
            services.AddScoped<TableReading>();

            return services.AddFormats();
        }

        /// <summary>One reader for each format this server reads.</summary>
        internal IServiceCollection AddFormats()
        {
            services.AddSingleton<IFormatReader, WordReader>();
            services.AddSingleton<IFormatReader, ExcelReader>();
            services.AddSingleton<IFormatReader, PowerPointReader>();
            services.AddSingleton<IFormatReader, PdfReader>();
            services.AddSingleton<IFormatReader, HtmlReader>();
            services.AddSingleton<IFormatReader, DelimitedReader>();
            services.AddSingleton<IFormatReader, JsonReader>();
            services.AddSingleton<IFormatReader, MarkdownReader>();
            services.AddSingleton<IFormatReader, PlainTextReader>();

            return services;
        }
    }
}
