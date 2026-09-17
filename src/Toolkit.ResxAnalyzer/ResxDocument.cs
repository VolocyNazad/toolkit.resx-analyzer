using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;

namespace Toolkit.ResxAnalyzer;

/// <summary>
/// One &lt;data name="..."&gt;&lt;value&gt;...&lt;/value&gt;&lt;/data&gt; entry read from a .resx
/// document, in document order and including duplicates (name uniqueness is a separate check,
/// not something this reader enforces).
/// </summary>
/// <param name="name">The entry's <c>name</c> attribute.</param>
/// <param name="value">The entry's <c>&lt;value&gt;</c> element text.</param>
internal readonly struct ResxEntry(string name, string value)
{
    public string Name { get; } = name;

    public string Value { get; } = value;
}

/// <summary>
/// Reads the &lt;data&gt; entries of a .resx document. &lt;metadata&gt; elements (design-time-only
/// entries some designers emit) are not &lt;data&gt; elements and are therefore never included.
/// </summary>
internal static class ResxDocument
{
    public static ImmutableArray<ResxEntry> ReadEntries(string content)
    {
        try
        {
            XDocument document = XDocument.Parse(content, LoadOptions.None);
            XElement? root = document.Root;
            if (root is null || root.Name.LocalName != "root")
                return [];

            ImmutableArray<ResxEntry>.Builder builder = ImmutableArray.CreateBuilder<ResxEntry>();

            foreach (XElement data in root.Elements("data"))
            {
                string? name = (string?)data.Attribute("name");
                if (string.IsNullOrEmpty(name))
                    continue;

                string value = data.Element("value")?.Value ?? string.Empty;
                builder.Add(new ResxEntry(name!, value));
            }

            return builder.ToImmutable();
        }
        catch (XmlException)
        {
            return [];
        }
    }
}
