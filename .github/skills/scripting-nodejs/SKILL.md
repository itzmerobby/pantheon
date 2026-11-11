---
name: scripting-nodejs
description: The user's guide for writing standalone Node.js scripts (automation, file work, CLI helpers, one-off tooling). Use when asked to write, fix, or extend a script that runs on Node.js.
---

# Scripting with Node.js

## Choosing a mode

Decide first whether the task is a one-off or a persistent script, and say which one you picked when it isn't obvious.

| | One-off (`node -e`) | Persistent (`.js` / `.mjs` file) |
|---|---|---|
| Purpose | Answer a question or do a quick action once | Something that will be run again, shared, or maintained |
| Lifetime | Gone after it runs | Lives in the repo or a scripts folder |
| Delivered as | A command the user pastes into a terminal | A file |
| Rules that apply | "One-off scripts" below | Every section below |

Use a persistent script instead when any of these is true:

- It needs more than about 10 lines, or more than one statement per line to stay readable.
- It will be run again, by the user or by anyone else.
- It changes or deletes files or systems, where `--dry-run` and logging matter.
- It needs quotes or special characters that make the shell quoting fragile.
- It needs arguments, configuration, or dependencies injected.

When a one-off turns out to be reusable, offer to promote it to a persistent script.

## One-off scripts (`node -e`)

- Deliver a single command the user can paste, with no file and no setup.
- Use `node -p "..."` when the result is just an expression to print. Use `node -e "..."` when you need statements.
- Use `--input-type=module` when the snippet needs `import` or top-level `await`. Otherwise `require` is available as-is.
- Keep it read-only by default. Inspect, count, convert, and print. If it must change something, make that explicit in your reply before the command.
- Print the result directly to stdout. No argument parsing, no template, and no exit-code handling beyond letting errors surface.
- Keep it short and flat. Skip the modular structure, dependency injection, and helper functions.
- Do not create files, install packages, or write anywhere as a side effect.

### Shell quoting (Windows)

State which shell the command is for when quoting matters. The user usually works in PowerShell or cmd on Windows.

- **PowerShell:** wrap the script in single quotes and use single quotes or backtick template literals inside it. Double quotes inside a native command argument get mangled. `node -p 'process.versions.node'`
- **cmd.exe:** wrap the script in double quotes and use single quotes or template literals inside it.
- Avoid `$`, `%`, `!`, `^`, and `&` inside the script, because shells interpret them.
- If the snippet cannot avoid fragile quoting, use a persistent script instead of fighting the shell.

## Persistent scripts

Code style (naming, comments, declarations, async) comes from the `code-style-javascript` skill and applies to both modes. Everything below applies to persistent scripts only.

## Defaults

- Write ES modules (`.mjs`, or `.js` with `"type": "module"`). Use `import`, not `require`.
- Import built-ins with the `node:` prefix (`node:fs/promises`, `node:path`, `node:child_process`).
- Use top-level `await`. Do not wrap the script in an `async main()` unless it is reused as a module.
- Prefer Node built-ins over dependencies. Add a package only when the built-in alternative is clearly worse.
- Reuse what already exists in the project (helpers, config, existing scripts) before writing new code.

## Structure

- Think in small modular units: one function does one job, and the script stitches them together.
- Keep the entry file thin: parse arguments, call the units, set the exit code.
- Move anything reusable into its own module and import it.
- Pass dependencies in as parameters (file system, logger, clock, process runner) instead of reaching for globals, so units stay easy to test and fix.
- Keep the flow top to bottom so the script is easy to visualize. Use early returns instead of deep nesting.

## Arguments and configuration

- Parse arguments with `parseArgs` from `node:util`. Do not hand-roll `process.argv` parsing.
- Support `--help`, and a `--dry-run` flag for anything that changes files or systems.
- Read secrets and settings from environment variables, or from a file loaded with `--env-file`. Never hardcode them.

## Files and paths

- Always build paths with `node:path` (`path.join`, `path.resolve`). Never concatenate with `/` or `\`.
- Resolve relative paths against the script's location (`import.meta.dirname`), not the current working directory, unless the user is meant to pass a path.
- Use `fs/promises` for file work. Use `fs.cp` and `fs.rm({ recursive: true, force: true })` instead of shelling out.
- Convert paths to file URLs with `pathToFileURL` when dynamically importing on Windows.
- Make scripts idempotent: running twice must not break or duplicate anything.
- Destructive operations (delete, overwrite, bulk rename) respect `--dry-run` and log what they did.

## Running other programs

- Use `execFile` or `spawn` with an argument array. Avoid `exec` with interpolated strings.
- Check the exit code of every child process and fail the script when it is non-zero.
- On Windows, call `.cmd` shims (`npm.cmd`) explicitly or use `shell: true` only when required.
- Scripts may run from an elevated terminal, so avoid writing outside the intended directories and never assume the working directory is safe.

## Output and exit codes

- Normal output goes to stdout. Errors and diagnostics go to stderr.
- Exit with `0` on success and a non-zero code on failure. Set `process.exitCode` instead of calling `process.exit()` so pending work can finish.
- Catch errors once at the top level, print a short message, and set the exit code. Do not swallow errors silently.
- If the output is meant for other tools, print machine-readable JSON.

## Template

```js
#!/usr/bin/env node
import { parseArgs } from "node:util";
import { readFile, writeFile } from "node:fs/promises";
import path from "node:path";

const { values: options } = parseArgs({
  options: {
    input: { type: "string" },
    dryRun: { type: "boolean", default: false },
    help: { type: "boolean", default: false },
  },
});

if (options.help || !options.input) {
  console.error("Usage: script --input <file> [--dry-run]");
  process.exitCode = options.help ? 0 : 1;
} else {
  try {
    const inputPath = path.resolve(options.input);
    const fileContents = await readFile(inputPath, "utf8");
    const transformedContents = fileContents.trim();

    if (options.dryRun) {
      console.log(`[dry-run] would write ${inputPath}`);
    } else {
      await writeFile(inputPath, transformedContents);
      console.log(`wrote ${inputPath}`);
    }
  } catch (error) {
    console.error(error instanceof Error ? error.message : error);
    process.exitCode = 1;
  }
}
```

## Style

Refer to the skill `code-style-javascript`.