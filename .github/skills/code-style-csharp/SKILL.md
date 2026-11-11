---
name: code-style-csharp
description: User's C# Coding Style. Use before coding in C#.
---

# C# Coding Style

## Naming Conventions

- Variable names should describe what the variable actually represents, even if the name will need to be of extreme length.
- Variable names are always camelCase.
- Private fields are camelCase too, with no underscore prefix. Use `this.` only when a parameter would otherwise shadow the field.
- Classes, records, structs, enums, DTOs, and delegates are PascalCase.
- Interfaces are PascalCase with the standard `I` prefix (`IOrderRepository`).
- Methods, properties, and events are PascalCase. Async methods end in `Async`.
- Name files after the type they contain, one primary type per file (`OrderService.cs`).
- Booleans read as a statement of fact (`isExpired`, `hasPendingOrders`).

## Structure

- Prefer an existing abstraction (framework type, shared helper, base class) over writing a new one, unless it is too restrictive for the case.
- Think in modular units: each class has one job, and larger behavior comes from stitching units together.
- Write code that is easy to visualize. Keep methods short, flow top to bottom, and use early returns instead of deep nesting.

## Dependency Injection

- Depend on interfaces and receive them through the constructor.
- Store injected dependencies in `private readonly` fields.
- Never construct a collaborator with `new` inside business logic. Register it in the DI container instead.
- Keep constructors free of logic. They only assign dependencies.

## Comments

- Comment only where the code cannot explain itself, such as a non-obvious reason, a workaround, or a constraint.
- Do not comment self-explanatory code. Do not restate what the code does.
- Skip XML doc comments on internal types. Add them only on public API surfaces.

## Formatting

- Use file-scoped namespaces.
- Use `var` when the type is obvious from the right-hand side. Otherwise state the type.
- Enable nullable reference types and handle nulls explicitly.
- Use `async`/`await` end to end. Avoid `.Result` and `.Wait()`.