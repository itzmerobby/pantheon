using System.Text;
using System.Xml;
using System.Xml.Linq;

var root = FindRepositoryRoot();
var versionPath = Path.Combine(root, "ecosystem.version");
if (Args.Count != 1 || string.IsNullOrWhiteSpace(Args[0]))
{
    throw new ArgumentException("Pass the package version as the only argument.");
}

var version = Args[0].Trim();
var currentVersion = File.ReadAllText(versionPath);
if (!string.Equals(currentVersion.Trim(), version, StringComparison.Ordinal))
{
    var lineEnding = currentVersion.EndsWith("\r\n", StringComparison.Ordinal)
        ? "\r\n"
        : currentVersion.EndsWith('\n')
            ? "\n"
            : currentVersion.EndsWith('\r')
                ? "\r"
                : string.Empty;
    File.WriteAllText(versionPath, version + lineEnding);
    Console.WriteLine($"Set ecosystem.version to {version}.");
}

var projectPaths = new[]
{
    Path.Combine(root, "Mekhane", "Mekhane", "Mekhane.csproj"),
    Path.Combine(root, "Mekhane", "Mekhane.AspNet", "Mekhane.AspNet.csproj")
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

static string FindRepositoryRoot()
{
    for (var directory = new DirectoryInfo(Environment.CurrentDirectory);
         directory is not null;
         directory = directory.Parent)
    {
        if (File.Exists(Path.Combine(directory.FullName, "ecosystem.version"))
            && File.Exists(Path.Combine(directory.FullName, "Mekhane", "Mekhane.slnx")))
        {
            return directory.FullName;
        }
    }

    throw new DirectoryNotFoundException(
        "Could not find the Pantheon repository root. Run this script from the repository or one of its subdirectories.");
}
