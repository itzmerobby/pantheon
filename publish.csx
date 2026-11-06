using System.Diagnostics;
using System.Xml.Linq;

var scriptPath = Environment.GetCommandLineArgs().FirstOrDefault(argument =>
    string.Equals(Path.GetExtension(argument), ".csx", StringComparison.OrdinalIgnoreCase)
    && File.Exists(argument))
    ?? throw new InvalidOperationException("Could not determine the path of publish.csx.");
var root = Path.GetDirectoryName(Path.GetFullPath(scriptPath))!;
if (Args.Count != 1 || string.IsNullOrWhiteSpace(Args[0]))
{
    throw new ArgumentException("Pass the package version as the only argument.");
}

var expectedVersion = Args[0].Trim();
var projectPaths = new[]
{
    Path.Combine(root, "Mekhane", "Mekhane", "Mekhane.csproj"),
    Path.Combine(root, "Mekhane", "Mekhane.AspNet", "Mekhane.AspNet.csproj"),
    Path.Combine(root, "Mekhane", "Mekhane.DependencyInjection", "Mekhane.DependencyInjection.csproj")
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
    Console.WriteLine($"Validated {project.PackageId} {project.Version} ({project.TargetFramework}).");
}

var apiKey = Environment.GetEnvironmentVariable("NUGET_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    throw new InvalidOperationException("NUGET_API_KEY must be set before publishing.");
}

foreach (var project in projects)
{
    var outputDirectory = Path.Combine(
        Path.GetDirectoryName(project.Path)!,
        "bin",
        "Release",
        project.TargetFramework);
    RunDotnet(root, "pack", project.Path, "--configuration", "Release", "--output", outputDirectory);

    var packagePath = Path.Combine(
        outputDirectory,
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

    return new ProjectInfo(path, RequiredProperty("PackageId"), RequiredProperty("Version"), RequiredProperty("TargetFramework"));
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

record ProjectInfo(string Path, string PackageId, string Version, string TargetFramework);
