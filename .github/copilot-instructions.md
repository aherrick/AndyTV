# Coding Instructions

Favor simple, readable, maintainable code over clever or elaborate solutions. Explicit user requests override these guidelines.

## Principles

- **KISS:** Choose the simplest solution that works. Avoid overengineering.
- **DRY:** Reuse existing logic and remove meaningful duplication, but don't abstract one-off code.
- **YAGNI:** Build only what's needed now. No speculative features, configuration, or extensibility.

## Making Changes

- Check existing code first and follow its conventions and patterns.
- Make the smallest focused change. Don't refactor unrelated code or change behavior unless asked.
- Keep one-off logic inline. Add a helper, service, or file only when it removes real duplication or an existing pattern calls for it.
- Don't add interfaces, wrappers, or layers without a concrete need, such as a second implementation.
- Don't add NuGet packages or frameworks unless asked.
- Don't move hardcoded values into configuration or add feature flags unless asked. Keep existing fallback defaults.
- Handle realistic errors only. No null checks, try/catch blocks, or fallbacks for impossible cases.
- Reuse existing styles and theme variables. Don't add new colors, pages, or major layout changes unless asked.
- Comments explain why, not what.
- Follow existing test conventions when behavior changes.

## C# and .NET

- Target .NET 11 with its default language version, C# 15.
- Nullable reference types are disabled. Don't use `string?`, `#nullable enable`, or `!`. Nullable value types like `int?` are fine.
- `dotnet new` templates add `<Nullable>enable</Nullable>`; remove it from new projects.
- Don't initialize string properties to `string.Empty` just to avoid null.
- Use file-scoped namespaces.
- Use primary constructors for services and simple classes. Use a regular constructor only when it needs a body, such as form setup or `InitializeComponent()`.
- Always use braces for `if`, `else`, and loops, even for one line.
- Prefer `var` when the type is obvious, pattern matching over `as` plus a null check, `?.` for guarded access and assignment, the `field` keyword over explicit backing fields, and collection expressions (`[]`, `[1, 2]`, `[.. items]`).
- Use `[GeneratedRegex]` instead of `new Regex(...)` or static `Regex` calls.
- Don't add an `Async` suffix to your own methods. Keep it only where a framework, interface, or override requires it.
- Mark members `static` when they don't use instance state, except `[RelayCommand]` methods.
- Build after changing code, and fix analyzer warnings in the code you changed.

## Responses

- Implement the requested change directly instead of offering options or asking unnecessary questions.
- Keep explanations brief, and don't suggest unrelated enhancements or refactors.
- If extra complexity is genuinely needed, briefly say why.
