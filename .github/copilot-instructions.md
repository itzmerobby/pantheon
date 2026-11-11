# Common Instructions

## Skill References

### Code Style

`code-style-[lang]` - The user's code style for the specific language. Note: lang here is not abbreviated, for example, C# is written as csharp. lang here is always lowercase.

Available languages: csharp, javascript

### Scripting

`scripting-[runtime]` - The user's guide on scripting using a given scripting runtime.
`terminal-[terminal]` - The user's guide on running commands in a given terminal.

Available runtimes: nodejs
Available terminals: powershell (not pwsh / powershell core)

## Tools

Do not bypass the agent harness' file and command tools, if you cannot proceed without additional permission, quit early and state the problem to the user. Always use the agent harness' tools for writing, reading, and modifying a file instead of using the shell for file edits.