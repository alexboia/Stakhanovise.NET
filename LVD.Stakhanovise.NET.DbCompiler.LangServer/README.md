# DbCompiler Language Server (VS Code)

VS Code language server extension for `.dbdef` and `.dbmap` files used by Stakhanovise DbCompiler.

## Quick start

1. Install dependencies (from repo root):
   - `npm install`
   - `npm install --workspaces`
2. Build both client and server:
   - `npm run compile`
3. Launch the Extension Development Host:
   - Press `F5` in VS Code or run `Debug: Start Debugging` targeting this workspace.
4. Open any `.dbdef` or `.dbmap` file (see `sample/`) to trigger the language server.

## Features (initial)
- Syntax highlighting via TextMate grammars for `.dbdef` and `.dbmap`.
- Diagnostics for unknown directives and malformed `MAP:` / `PROPS:` lines.
- Basic completions for directives and common property keys.

## Workspace setting
- `dbcompiler.schemaPaths` (array): folders to scan for definitions (reserved for future cross-file indexing; defaults to `sample`).

## Notes
- The server is a lightweight, line-oriented parser; it keeps going after errors to surface multiple diagnostics.
- Extension activation is automatic when a `.dbdef` or `.dbmap` file is opened.
