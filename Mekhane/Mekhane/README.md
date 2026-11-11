# Mekhane

A simple library for creating modular applications in C#.

## Installation

```shell
dotnet add package Mekhane
```

Package-detected application configuration and execution failures throw
`PantheonPackageException` from the `Pantheon.Common` package. Its `Code`
property provides a stable identifier; exceptions thrown by module callbacks,
application builders, or applications are available as `InnerException`.
