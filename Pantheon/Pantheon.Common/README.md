# Pantheon.Common

Shared types used by Pantheon packages. This package is not an application runtime.

## PantheonPackageException

`PantheonPackageException` describes the error and provides a stable `Code`
identifier for programmatic error handling. Codes consist of a 3-12 letter
prefix followed by a number from 1 to 999999. An optional inner exception is
available for preserving exceptions thrown by user-provided code.
