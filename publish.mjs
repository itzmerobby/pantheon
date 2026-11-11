#!/usr/bin/env node
import { spawn, execFile } from "node:child_process";
import { access, readFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { parseArgs, promisify } from "node:util";

const execFileAsync = promisify(execFile);
const root = path.dirname(fileURLToPath(import.meta.url));

const { values: options, positionals } = parseArgs({
  allowPositionals: true,
  options: {
    "dry-run": { type: "boolean", default: false },
    help: { type: "boolean", short: "h", default: false },
  },
});

if (options.help) {
  console.log("Usage: node publish.mjs <version> [--dry-run]");
} else if (positionals.length !== 1 || positionals[0].trim() === "") {
  console.error("Pass the package version as the only argument.");
  console.error("Usage: node publish.mjs <version> [--dry-run]");
  process.exitCode = 1;
} else {
  const expectedVersion = positionals[0].trim();

  try {
    const config = JSON.parse(
      await readFile(path.join(root, "pantheon.config.json"), "utf8"),
    );
    const projectPaths = config.packages.flatMap(({ group, packages }) =>
      packages.map((packageName) =>
        path.join(root, group, packageName, `${packageName}.csproj`),
      ),
    );
    const projects = await Promise.all(projectPaths.map(readProject));
    const mismatches = projects
      .filter((project) => project.version !== expectedVersion)
      .map((project) => `${project.packageId}: ${project.version}`);

    if (mismatches.length > 0) {
      throw new Error(
        `Requested version (${expectedVersion}) does not match package project version(s): ${mismatches.join(", ")}. Run version-sync.mjs first.`,
      );
    }

    for (const project of projects) {
      console.log(
        `Validated ${project.packageId} ${project.version} (${project.targetFramework}).`,
      );
    }

    const apiKey = process.env.NUGET_API_KEY;
    if (!options["dry-run"] && (!apiKey || apiKey.trim() === "")) {
      throw new Error("NUGET_API_KEY must be set before publishing.");
    }

    for (const project of projects) {
      const outputDirectory = path.join(
        path.dirname(project.path),
        "bin",
        "Release",
        project.targetFramework,
      );
      const packagePath = path.join(
        outputDirectory,
        `${project.packageId}.${project.version}.nupkg`,
      );

      if (options["dry-run"]) {
        console.log(`[dry-run] Would pack ${project.packageId} and push ${packagePath}.`);
        continue;
      }

      await runDotnet(
        root,
        "pack",
        project.path,
        "--configuration",
        "Release",
        "--output",
        outputDirectory,
      );

      await ensurePackageExists(packagePath);
      await runDotnet(
        root,
        "nuget",
        "push",
        packagePath,
        "--api-key",
        apiKey,
        "--source",
        "https://api.nuget.org/v3/index.json",
      );
    }
  } catch (error) {
    console.error(error instanceof Error ? error.message : error);
    process.exitCode = 1;
  }
}

async function readProject(projectPath) {
  let output;
  try {
    ({ stdout: output } = await execFileAsync(
      "dotnet",
      [
        "msbuild",
        projectPath,
        "-getProperty:PackageId,Version,TargetFramework",
      ],
      { cwd: root, windowsHide: true },
    ));
  } catch (error) {
    const detail = error instanceof Error ? error.message : String(error);
    throw new Error(`Could not read MSBuild properties from ${projectPath}: ${detail}`);
  }

  let properties;
  try {
    properties = JSON.parse(output).Properties;
  } catch {
    throw new Error(`Could not parse MSBuild properties from ${projectPath}.`);
  }

  const { PackageId: packageId, Version: version, TargetFramework: targetFramework } =
    properties ?? {};
  if (!packageId || !version || !targetFramework) {
    throw new Error(`Project ${projectPath} is missing required MSBuild properties.`);
  }

  return { path: projectPath, packageId, version, targetFramework };
}

async function ensurePackageExists(packagePath) {
  try {
    await access(packagePath);
  } catch {
    throw new Error(`Expected package was not created: ${packagePath}`);
  }
}

function runDotnet(workingDirectory, ...arguments_) {
  return new Promise((resolve, reject) => {
    const childProcess = spawn("dotnet", arguments_, {
      cwd: workingDirectory,
      stdio: "inherit",
      windowsHide: true,
    });

    childProcess.once("error", (error) => {
      reject(new Error(`Could not start dotnet ${arguments_.join(" ")}: ${error.message}`));
    });
    childProcess.once("close", (code) => {
      if (code === 0) {
        resolve();
      } else {
        reject(
          new Error(
            `dotnet ${arguments_.join(" ")} failed with exit code ${code ?? "unknown"}.`,
          ),
        );
      }
    });
  });
}
