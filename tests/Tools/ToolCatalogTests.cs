using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using Snail.MCP.Documents.Application.Ports;
using Snail.MCP.Documents.Application.Prompts;
using Snail.MCP.Documents.Domain;
using Snail.MCP.Documents.Tools.Prompts;

namespace Snail.MCP.Documents.Tests.Tools;

/// <summary>Guard tests over what the model reads: a hardcoded description drifts from the code it describes unless something fails when it does.</summary>
public sealed partial class ToolCatalogTests
{
    [Fact]
    public void EveryTool_InTheAssembly_IsNamedAndDescribed()
    {
        Assert.Equal(["documents_outline", "documents_read", "documents_table"], Catalog.Names);
        Assert.All(Catalog.Tools, tool => Assert.False(string.IsNullOrWhiteSpace(tool.GetCustomAttribute<DescriptionAttribute>()?.Description)));
    }

    /// <summary>A parameter the model cannot see the meaning of is one it fills in by guessing.</summary>
    [Fact]
    public void EveryParameter_TheModelFills_IsDescribed()
    {
        var parameters = Catalog.Tools
            .SelectMany(tool => tool.GetParameters())
            .Where(parameter => parameter.ParameterType != typeof(CancellationToken));

        Assert.All(parameters, parameter =>
            Assert.False(string.IsNullOrWhiteSpace(parameter.GetCustomAttribute<DescriptionAttribute>()?.Description), parameter.Name));
    }

    /// <summary>A tool without the hint is treated by the platform as one that writes, and is shown to a human before it runs.</summary>
    [Fact]
    public void EveryTool_OfThisServer_IsDeclaredReadOnlyAndClosedWorld()
    {
        Assert.Equal(Catalog.Names, [.. Catalog.Named(tool => tool.ReadOnly)]);
        Assert.Equal(Catalog.Names, [.. Catalog.Named(tool => !tool.OpenWorld)]);
    }

    /// <summary>A tool a text names and the server does not have is a call the model will make and lose.</summary>
    [Theory]
    [InlineData(typeof(ServerGuidance))]
    [InlineData(typeof(ToolDescriptions))]
    [InlineData(typeof(Messages))]
    public void EveryTool_ATextNames_Exists(Type texts)
    {
        var named = texts
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .SelectMany(field => ToolName().Matches((string)field.GetRawConstantValue()!))
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal);

        Assert.All(named, name => Assert.Contains(name, Catalog.Names));
    }

    /// <summary>A format added to the code and left out of the description is one the model will never think to ask for.</summary>
    [Fact]
    public void ReadDescription_NamesEveryKindOfDocument() =>
        Assert.All(Enum.GetNames<DocumentKind>(), kind => Assert.Contains(kind, ToolDescriptions.Read, StringComparison.OrdinalIgnoreCase));

    /// <summary>The refusal of an unknown format lists what is read; a reader added without it is a format nobody is told about.</summary>
    [Fact]
    public void UnknownHint_ListsEveryExtensionAReaderTakes()
    {
        var extensions = typeof(IFormatReader).Assembly.GetTypes()
            .Where(type => type.IsClass && typeof(IFormatReader).IsAssignableFrom(type))
            .Select(type => (IFormatReader)Activator.CreateInstance(type)!)
            .SelectMany(reader => reader.Extensions)
            .Where(extension => extension is not (".docm" or ".xlsm" or ".pptm" or ".text" or ".log" or ".markdown"));

        Assert.All(extensions, extension => Assert.Contains(extension, Messages.UnknownHint, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(ToolDescriptions.ReadCharacters, Limits.FewestCharacters, Limits.MostCharacters, Limits.DefaultCharacters)]
    [InlineData(ToolDescriptions.TableRows, 1, Limits.MostRows, Limits.DefaultRows)]
    public void Description_OfANumber_StatesTheRangeTheCodeEnforces(string description, int least, int most, int byDefault)
    {
        Assert.Contains($"{least} to {most}", description, StringComparison.Ordinal);
        Assert.Contains($"{byDefault} by default", description, StringComparison.Ordinal);
    }

    /// <summary>The outline says where its lists stop; a cap changed in code and not in prose is a list the model thinks is whole.</summary>
    [Fact]
    public void OutlineDescription_StatesWhereEveryListStops()
    {
        Assert.Equal([Limits.MostParts, Limits.MostParts], [Limits.MostHeadings, Limits.MostTables]);
        Assert.Contains($"stops at {Limits.MostParts} entries", ToolDescriptions.Outline, StringComparison.Ordinal);
    }

    [GeneratedRegex(@"\bdocuments_[a-z_]+\b", RegexOptions.CultureInvariant)]
    private static partial Regex ToolName();
}
