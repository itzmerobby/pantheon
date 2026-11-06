using System.Text;
using System.Xml;
using System.Xml.Linq;

var scriptPath = Environment.GetCommandLineArgs().FirstOrDefault(argument =>
    string.Equals(Path.GetExtension(argument), ".csx", StringComparison.OrdinalIgnoreCase)
    && File.Exists(argument))
    ?? throw new InvalidOperationException("Could not determine the path of version-sync.csx.");
var root = Path.GetDirectoryName(Path.GetFullPath(scriptPath))!;
if (Args.Count != 1 || string.IsNullOrWhiteSpace(Args[0]))
{
    throw new ArgumentException("Pass the package version as the only argument.");
}

var version = Args[0].Trim();

var projectPaths = new[]
{
    Path.Combine(root, "Mekhane", "Mekhane", "Mekhane.csproj"),
    Path.Combine(root, "Mekhane", "Mekhane.AspNet", "Mekhane.AspNet.csproj"),
    Path.Combine(root, "Mekhane", "Mekhane.DependencyInjection", "Mekhane.DependencyInjection.csproj")
};

foreach (var projectPath in projectPaths)
{
    if (!File.Exists(projectPath))
    {
        throw new FileNotFoundException($"Package project was not found: {projectPath}", projectPath);
    }

    var document = XDocument.Load(projectPath, LoadOptions.PreserveWhitespace);
    var versions = document
        .Descendants()
        .Where(element => element.Name.LocalName == "Version")
        .ToArray();
    if (versions.Length == 0)
    {
        throw new InvalidOperationException($"Project {projectPath} has no <Version> element to update.");
    }

    var changed = false;
    foreach (var versionElement in versions)
    {
        if (!string.Equals(versionElement.Value, version, StringComparison.Ordinal))
        {
            versionElement.Value = version;
            changed = true;
        }
    }

    if (changed)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            Indent = false,
            NewLineHandling = NewLineHandling.None,
            OmitXmlDeclaration = document.Declaration is null
        };
        using var writer = XmlWriter.Create(projectPath, settings);
        document.Save(writer);
        Console.WriteLine($"Set {Path.GetFileName(projectPath)} version to {version}.");
    }
    else
    {
        Console.WriteLine($"{Path.GetFileName(projectPath)} is already at version {version}.");
    }
}
