#!/usr/bin/env node
import { readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { parseArgs } from "node:util";

const root = path.dirname(fileURLToPath(import.meta.url));

const { values: options, positionals } = parseArgs({
  allowPositionals: true,
  options: {
    "dry-run": { type: "boolean", default: false },
    help: { type: "boolean", short: "h", default: false },
  },
});

if (options.help) {
  console.log("Usage: node version-sync.mjs <version> [--dry-run]");
} else if (positionals.length !== 1 || positionals[0].trim() === "") {
  console.error("Pass the package version as the only argument.");
  console.error("Usage: node version-sync.mjs <version> [--dry-run]");
  process.exitCode = 1;
} else {
  const version = positionals[0].trim();
  const escapedVersion = escapeXml(version);

  try {
    const config = JSON.parse(
      await readFile(path.join(root, "pantheon.config.json"), "utf8"),
    );
    const projectPaths = config.packages.flatMap(({ group, packages }) =>
      packages.map((packageName) =>
        path.join(root, group, packageName, `${packageName}.csproj`),
      ),
    );

    for (const projectPath of projectPaths) {
      const originalContents = await readFile(projectPath, "utf8");
      let versionCount = 0;
      let changed = false;
      const updatedContents = originalContents.replace(
        /<((?:[\w.-]+:)?Version)\b([^>]*)>([\s\S]*?)<\/\1\s*>/g,
        (element, name, attributes, currentVersion) => {
          versionCount += 1;
          if (currentVersion === escapedVersion) {
            return element;
          }

          changed = true;
          return `<${name}${attributes}>${escapedVersion}</${name}>`;
        },
      );

      if (versionCount === 0) {
        throw new Error(`Project ${projectPath} has no <Version> element to update.`);
      }

      if (changed) {
        if (options["dry-run"]) {
          console.log(`[dry-run] Would set ${path.basename(projectPath)} version to ${version}.`);
        } else {
          await writeFile(projectPath, updatedContents, "utf8");
          console.log(`Set ${path.basename(projectPath)} version to ${version}.`);
        }
      } else {
        console.log(`${path.basename(projectPath)} is already at version ${version}.`);
      }
    }
  } catch (error) {
    console.error(error instanceof Error ? error.message : error);
    process.exitCode = 1;
  }
}

function escapeXml(value) {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;");
}
