---
name: code-style-javascript
description: The user's code style for JavaScript source (naming, comments, declarations, async). Use whenever writing or editing JavaScript, including Node.js scripts and one-liners.
---

# JavaScript code style

## Naming

- Variable names are camelCase and describe what the value represents, even when the name gets long.
- Type-like identifiers (classes, DTOs, interfaces) are PascalCase.

## Comments

- Comment only where the code cannot explain itself.
- Do not comment self-explanatory code.

## Declarations

- Use `const` by default, `let` only when reassigning, never `var`.

## Async

- Use `async`/`await`.
- Run independent work with `Promise.all`.