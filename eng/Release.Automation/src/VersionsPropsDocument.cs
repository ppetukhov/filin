using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

using Microsoft.Extensions.Logging;

using Nuke.Common.IO;

namespace Release.Automation;

/// <summary>
/// Represents a deserialized, strongly-typed view of the central Versions.props file.
/// </summary>
sealed class VersionsPropsDocument
{
    readonly XDocument _document;
    readonly XElement _propertyGroup;

    /// <summary>
    /// Initializes a new instance of <see cref="VersionsPropsDocument"/> wrapping the provided XML document.
    /// </summary>
    VersionsPropsDocument(XDocument document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));

        _propertyGroup = _document.Root?.Element("PropertyGroup")
            ?? throw new InvalidDataException("The XML structure is missing the required <PropertyGroup> element.");
    }

    /// <summary>
    /// Indexer to get or set the version string for a given project name.
    /// </summary>
    public string? this[string projectName]
    {
        get => GetVersion(projectName); set => _ = !string.IsNullOrWhiteSpace(value) ? TrySetVersion(projectName, value) : RemoveVersion(projectName);
    }

    /// <summary>
    /// Changed.
    /// </summary>
    public bool Changed { get; private set; }

    /// <summary>
    /// Converts a project name to its XML tag representation (e.g. 'My.Project' -> 'MyProjectVersion').
    /// </summary>
    public static string GetVersionXmlTagName(string projectName) =>
        string.Concat(projectName.AsSpan(), "Version".AsSpan())
            .Replace(".", string.Empty, StringComparison.Ordinal);

    /// <summary>
    /// Gets the current version for the specified project name, or <c>null</c> if not present.
    /// </summary>
    public string? GetVersion(string projectName)
    {
        var xmlTagName = GetVersionXmlTagName(projectName);

        return _propertyGroup.Element(xmlTagName)?.Value;
    }

    /// <summary>
    /// Tries to retrieve the version for the specified project.
    /// </summary>
    public bool TryGetVersion(string projectName, [NotNullWhen(true)] out string? version)
    {
        version = GetVersion(projectName);
        return version is not null;
    }

    /// <summary>
    /// Tries to sets or updates the version for the specified project name.
    /// </summary>
    public bool TrySetVersion(string projectName, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        var xmlTagName = GetVersionXmlTagName(projectName);

        var element = _propertyGroup.Element(xmlTagName);

        if (element is null)
        {
            _propertyGroup.Add(new XElement(xmlTagName, version));
            Changed = true;

            return true;
        }
        else if (!string.Equals(element.Value, version, StringComparison.Ordinal))
        {
            element.Value = version;
            Changed = true;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes the version entry for the specified project.
    /// </summary>
    public bool RemoveVersion(string projectName)
    {
        var xmlTagName = GetVersionXmlTagName(projectName);

        var element = _propertyGroup.Element(xmlTagName);

        if (element is not null)
        {
            element.Remove();
            Changed = true;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Deserializes a <see cref="VersionsPropsDocument"/> from an existing file or creates a virtual empty document.
    /// </summary>
    public static VersionsPropsDocument LoadOrCreate(AbsolutePath filePath, ILogger? logger = null)
    {
        if (!filePath.FileExists())
        {
            logger?.WorkingWithVirtualXmlStructure();
            var emptyDoc = new XDocument(new XElement("Project", new XElement("PropertyGroup")));
            return new VersionsPropsDocument(emptyDoc);
        }

        var doc = XDocument.Load(filePath);
        return new VersionsPropsDocument(doc);
    }

    /// <summary>
    /// Parses a <see cref="VersionsPropsDocument"/> directly from an XML string content.
    /// </summary>
    public static VersionsPropsDocument Parse(string xmlContent)
    {
        var doc = XDocument.Parse(xmlContent);
        return new VersionsPropsDocument(doc);
    }

    /// <summary>
    /// Serializes and saves the underlying XML document back to disk.
    /// </summary>
    public void Save(AbsolutePath filePath, ILogger? logger = null)
    {
        _document.Save(filePath);
        Changed = false;

        logger?.VersionsPropsSaved();
    }
}
