---
name: terminal-powershell
description: The user's guide for running terminal commands in Windows PowerShell 5.1 (not pwsh / PowerShell Core): navigation, files, processes, environment, native programs, quoting, elevation. Use when giving or running commands for a PowerShell terminal.
---

# Terminal: PowerShell

## Assumptions

- The shell is Windows PowerShell 5.1 (`powershell.exe`) on Windows. It is not `pwsh` / PowerShell Core / PowerShell 7. Write only syntax and parameters that exist in 5.1.
- State it when a command would differ in cmd.exe or bash.
- The terminal is usually elevated (the user runs as local administrator). Treat every command as having full permission to change the system.
- If a command fails with a parameter or syntax error that looks like a version problem, confirm with `$PSVersionTable.PSVersion` and rewrite it for 5.1.

## Not available in 5.1

Do not use any of these. Use the 5.1 alternative.

| Avoid (PowerShell 7+) | Use in 5.1 |
|---|---|
| `cmd1 && cmd2`, `cmd1 \|\| cmd2` | `cmd1; if ($?) { cmd2 }`, or check `$LASTEXITCODE` |
| `$a ? $b : $c` | `if ($a) { $b } else { $c }` |
| `$a ?? $b`, `$a ??= $b`, `$a?.Prop` | `if ($null -ne $a) { $a } else { $b }` |
| `ForEach-Object -Parallel` | A plain `foreach`, or `Start-Job` when parallel work is truly needed |
| `ConvertFrom-Json -AsHashtable` | Convert the object, or use `ConvertFrom-Json` and read its properties |
| `Get-Content -AsByteStream` | `Get-Content -Encoding Byte` |
| `-Encoding utf8NoBOM` | The .NET call shown under "Navigation and files" |
| `$PSStyle`, `Join-String`, `Get-Error` | Plain strings, `-join`, `$Error[0]` |

## Safety (elevated terminal)

- Preview destructive commands first with `-WhatIf` (`Remove-Item`, `Move-Item`, `Stop-Process`, `Set-ItemProperty` and most state-changing cmdlets support it).
- Never run `Remove-Item -Recurse -Force` on a path built from a variable that might be empty. Check it first: `if (-not $targetPath) { throw 'targetPath is empty' }`.
- Use `-LiteralPath` instead of `-Path` when a path may contain `[`, `]`, `*`, or `?`.
- Do not change system-wide settings (registry under `HKLM`, machine environment variables, services, firewall, execution policy) without saying so in the reply first.
- Prefer `-Scope CurrentUser` for settings that offer a scope (`Set-ExecutionPolicy`, `Install-Module`).
- Do not disable security features (Defender, UAC, SmartScreen) as a shortcut to fix an error.

## Writing commands

- Give one command or one short pipeline per code block, ready to paste.
- Use full cmdlet names and full parameter names (`Get-ChildItem -Recurse`, not `gci -r`). Aliases are fine only for the shortest interactive commands like `cd` and `ls`.
- Chain steps with `;`. Make a step depend on the previous one with `if ($?) { ... }`, since `&&` and `||` do not exist in 5.1.
- Work with objects: filter with `Where-Object`, shape with `Select-Object`, sort with `Sort-Object`. Do not parse text with regex when a property exists.
- Prefer `-ErrorAction Stop` with `try`/`catch` when a failure should halt the sequence.
- Use `Format-Table`/`Format-List` only as the last pipeline step, since it breaks further piping.
- For machine-readable output use `ConvertTo-Json -Depth 5` and `ConvertTo-Csv`.

## Navigation and files

- Show where you are with `Get-Location`. Change directory with `Set-Location -LiteralPath`.
- Include hidden and system items with `Get-ChildItem -Force`.
- Test before acting: `Test-Path -LiteralPath $path`.
- Build paths with `Join-Path`, never string concatenation.
- Read files with `Get-Content`, and use `-Raw` when the whole text is needed as one string and `-Tail`/`-TotalCount` for large files.
- Write text files with an explicit encoding. `Set-Content -Encoding utf8` writes a BOM in 5.1, which breaks some tools (JSON parsers, shell scripts, config files). For UTF-8 without a BOM use:
  `[System.IO.File]::WriteAllText($path, $text, (New-Object System.Text.UTF8Encoding $false))`
- Copy, move, and delete with `Copy-Item`, `Move-Item`, `Remove-Item`. Use `-Recurse` only when needed.
- Find files by content with `Select-String -Path ... -Pattern ...`.

## Processes, services, and ports

- List processes with `Get-Process`. Stop with `Stop-Process -Id <id>` and confirm with `-WhatIf` first.
- Find what holds a port: `Get-NetTCPConnection -LocalPort <port> | Select-Object LocalPort, State, OwningProcess`.
- Manage services with `Get-Service`, `Start-Service`, `Stop-Service`, `Restart-Service`. Say so when a service change affects the system.
- Start a program detached with `Start-Process`. Use `-Wait` to block, `-PassThru` to get the process object, and `-Verb RunAs` to elevate a non-elevated terminal.
- Check whether the current terminal is elevated:
  `([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)`

## Environment variables

- Read with `$env:NAME`. Set for the current session with `$env:NAME = 'value'`.
- Persist for the user with `[Environment]::SetEnvironmentVariable('NAME', 'value', 'User')`. Use `'Machine'` only when the user asks, and say that it affects every account.
- A persisted variable does not appear in terminals that are already open. Say that a new terminal is needed.
- Append to `PATH` without overwriting it: `$env:Path += ';C:\tools'` for the session.

## Running native programs

- Call a program whose path contains spaces or that starts with a quote with the call operator: `& 'C:\Program Files\Tool\tool.exe' --flag`.
- Check the result with `$LASTEXITCODE` after native commands. `$?` only tells you whether PowerShell itself succeeded.
- Use `curl.exe` explicitly. In 5.1 the bare `curl` and `wget` are aliases for `Invoke-WebRequest`, with different arguments.
- Add `-UseBasicParsing` to `Invoke-WebRequest` so it does not depend on Internet Explorer being set up.
- Avoid `Invoke-Expression`. Pass arguments as separate tokens or a splatted array instead of building a command string.
- Redirect with `>` and `2>&1` carefully. In 5.1, `>` writes UTF-16 and `2>&1` on a native command wraps stderr lines as error records. Use `| Out-File -Encoding utf8` or the .NET call above when the encoding matters.

## Quoting

- Single quotes `'...'` are literal. Double quotes `"..."` expand `$variables` and `$(subexpressions)`.
- Escape a literal `$` or `"` inside double quotes with the backtick: `` `$ `` and `` `" ``.
- To put a single quote inside a single-quoted string, double it: `'it''s'`.
- Double quotes inside a native-command argument are stripped or split by 5.1's argument passing. Prefer single quotes or template literals inside the program's own script, as in `node -p 'process.versions.node'`.
- When an argument must contain double quotes, escape each one as `\"` inside a single-quoted string, or move the text into a file.
- For long or awkward text, use a here-string (`@' ... '@` for literal, `@" ... "@` for expanding) and pass it through a variable or a file.

## Execution policy and scripts

- Run a script file with `.\script.ps1`. The leading `.\` is required for the current directory.
- If scripts are blocked, set the policy for the current user: `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`. For a single run, `powershell -ExecutionPolicy Bypass -File .\script.ps1` avoids a persistent change. Always launch with `powershell`, not `pwsh`.
- Start scripts with `Set-StrictMode -Version Latest` and `$ErrorActionPreference = 'Stop'`.
- For anything beyond a short pipeline, or anything that will be reused, write a `.ps1` file instead of a pasted block.

## Output to the user

- Say which directory a command expects to run from when it matters.
- When a command changes state, say what it changes before the code block.
- When a command may need a new terminal, a reboot, or a re-login, say so.
- When the user pastes an error, quote the exact message and fix that message. Do not guess at a different problem.