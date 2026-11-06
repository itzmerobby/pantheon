using System.Diagnostics;
using System.Xml.Linq;

var root = FindRepositoryRoot();
if (Args.Count != 1 || string.IsNullOrWhiteSpace(Args[0]))
{
    throw new ArgumentException("Pass the package version as the only argument.");
}

var expectedVersion = Args[0].Trim();
var projectPaths = new[]
{
    Path.Combine(root, "Mekhane", "Mekhane", "Mekhane.csproj"),
    Path.Combine(root, "Mekhane", "Mekhane.AspNet", "Mekhane.AspNet.csproj")
};

var projects = projectPaths.Select(ReadProject).ToArray();
var mismatches = projects
    .Where(project => !string.Equals(project.Version, expectedVersion, StringComparison.Ordinal))
    .Select(project => $"{project.PackageId}: {project.Version}")
    .ToArray();

if (mismatches.Length > 0)
{
    throw new InvalidOperationException(
        $"Requested version ({expectedVersion}) does not match package project version(s): {string.Join(", ", mismatches)}. Run version-sync.csx first.");
}

foreach (var project in projects)
{
    Console.WriteLine($"Validated {project.PackageId} {project.Version} ({string.Join(", ", project.TargetFrameworks)}).");
}

var apiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    throw new InvalidOperationException("NUGET_API_KEY must be set before publishing.");
}

foreach (var project in projects)
{
    RunDotnet(root, "pack", project.Path, "--configuration", "Release");

    var packagePath = Path.Combine(
        Path.GetDirectoryName(project.Path)!,
        "bin",
        "Release",
        $"{project.PackageId}.{project.Version}.nupkg");
    if (!File.Exists(packagePath))
    {
        throw new FileNotFoundException($"Expected package was not created: {packagePath}", packagePath);
    }

    RunDotnet(
        root,
        "nuget",
        "push",
        packagePath,
        "--api-key",
        apiKey,
        "--source",
        "https://api.nuget.org/v3/index.json");
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

static ProjectInfo ReadProject(string path)
{
    if (!File.Exists(path))
    {
        throw new FileNotFoundException($"Package project was not found: {path}", path);
    }

    var document = XDocument.Load(path);
    var properties = document.Root?
        .Elements()
        .Where(element => element.Name.LocalName == "PropertyGroup")
        .SelectMany(group => group.Elements())
        .ToArray() ?? throw new InvalidOperationException($"Project XML has no root element: {path}");

    string RequiredProperty(string name) =>
        properties.FirstOrDefault(element => element.Name.LocalName == name)?.Value.Trim()
        ?? throw new InvalidOperationException($"Project {path} is missing <{name}>.");

    var targetFrameworks = properties
        .Where(element => element.Name.LocalName is "TargetFramework" or "TargetFrameworks")
        .SelectMany(element => element.Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Distinct(StringComparer.Ordinal)
        .ToArray();
    if (targetFrameworks.Length == 0)
    {
        throw new InvalidOperationException($"Project {path} has no <TargetFramework> or <TargetFrameworks>.");
    }

    return new ProjectInfo(path, RequiredProperty("PackageId"), RequiredProperty("Version"), targetFrameworks);
}

static void RunDotnet(string workingDirectory, params string[] arguments)
{
    var startInfo = new ProcessStartInfo("dotnet")
    {
        WorkingDirectory = workingDirectory,
        UseShellExecute = false
    };
    foreach (var argument in arguments)
    {
        startInfo.ArgumentList.Add(argument);
    }

    using var process = Process.Start(startInfo)
        ?? throw new InvalidOperationException($"Could not start dotnet {string.Join(' ', arguments)}.");
    process.WaitForExit();
    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException(
            $"dotnet {string.Join(' ', arguments)} failed with exit code {process.ExitCode}.");
    }
}

record ProjectInfo(string Path, string PackageId, string Version, string[] TargetFrameworks);
