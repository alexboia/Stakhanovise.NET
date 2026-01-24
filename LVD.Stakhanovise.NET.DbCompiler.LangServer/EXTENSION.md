# VS Code DbCompiler Language Server - Implementation Guide

This guide describes how to build, run, and extend the VS Code extension and language server for `.dbdef` and `.dbmap` files.

## Prerequisites
- Node.js 18+
- VS Code 1.85+
- `npm` (or `pnpm` if you prefer; commands below use npm)

## Project layout
- `package.json` (root): npm workspaces wiring client and server
- `client/`: VS Code extension (language client, grammars, config)
- `server/`: Language Server Protocol (LSP) implementation
- `sample/`: Example `.dbdef` / `.dbmap` files used for validation and testing

## Install dependencies
From repo root:
```sh
npm install
npm install --workspaces
```

## Build
```sh
npm run compile
```
This builds both client and server to `client/out` and `server/out`.

## Run in VS Code (Extension Development Host)
1. Open the workspace in VS Code.
2. Choose **Run > Start Debugging** and select **Run DbCompiler Extension** (configured in `.vscode/launch.json`).
3. The preLaunch task runs `npm run compile`; then VS Code spawns the Extension Development Host.
4. Open a `.dbdef` or `.dbmap` file (e.g., from `sample/`) to trigger activation and highlighting.

## Debug
- The language client launches the server with `--inspect=6009` in debug mode. Use VS Code’s built-in debugger to attach (already wired by `F5`).

## Package
(Optional) create a VSIX:
```sh
cd client
npx vsce package
```

## Language definition (current)
- Languages: `dbdef` (`*.dbdef`), `dbmap` (`*.dbmap`)
- Comment style: line comment `--`
- TextMate grammars: `client/syntaxes/dbdef.tmLanguage.json`, `client/syntaxes/dbmap.tmLanguage.json`
- Language configuration: `client/language-configuration.json`

## Server capabilities (current)
- Incremental text sync
- Diagnostics
  - `dbmap`: each non-comment line must be `MAP: key=value`; keys must match `[A-Za-z_][A-Za-z0-9_]*`; value required.
  - `dbdef`: recognizes directives `TBL`, `SEQ`, `FUNC`, `NAME:`, `PROPS:`, `COL:`, `CONSTRAINT:`, `IDX:`, `PARAM:`, `RET:`, `BODY:`/`BODY;`. Checks for unknown directives, missing `key=value` in `PROPS:`, missing content after directive prefixes, unclosed `BODY;`.
- Completions
  - Directive keywords (`TBL`, `SEQ`, `FUNC`, `NAME:`, `PROPS:`, `COL:`, `CONSTRAINT:`, `IDX:`, `PARAM:`, `RET:`, `BODY:`, `BODY;`).
  - Property hints (`title=`, `description=`, `start=`, `increment=`, `min_value=`, `max_value=`, `cache=`, `language=`, `separator=`, `type=`, `not_null=`, `default=`, `direction=`).
  - `MAP:` keys (`queue_table_name=`, `results_queue_table_name=`, `execution_time_stats_table_name=`, `metrics_table_name=`, `new_task_notification_channel_name=`, `dequeue_function_name=`).

### Server code structure
- Entry point: `server/src/server.ts` (LSP wiring, completion, diagnostic publishing)
- Parsing/validation helpers: `server/src/parser.ts` (line-oriented checks for `.dbdef`/`.dbmap` returning structured diagnostics)

## Grammar file formats (quick reference)
- **TextMate grammar (`*.tmLanguage.json`)**: declarative regex-based tokenizer.
   - Root keys: `$schema`, `name`, `scopeName`, `patterns` (top-level includes), `repository` (named rules).
   - Each pattern can `include` another rule (e.g., `#comments`) or define a `match` regex with a `name` scope. Scopes drive syntax highlighting via themes.
   - Common rule shape: `{ "name": "scope.identifier", "match": "<regex>" }` or `{ "include": "#ruleName" }`.
   - Our grammars: [client/syntaxes/dbdef.tmLanguage.json](client/syntaxes/dbdef.tmLanguage.json) and [client/syntaxes/dbmap.tmLanguage.json](client/syntaxes/dbmap.tmLanguage.json).
- **Language configuration (`language-configuration.json`)**: editor behaviors, not colors.
   - Defines comments, brackets, auto-closing/surrounding pairs, word pattern for cursor/select operations.
   - Lives at [client/language-configuration.json](client/language-configuration.json); shared by both `dbdef` and `dbmap`.

## How to extend validation and completion
1. Define the schema for directives (this is about your `.dbdef`/`.dbmap` DSL fields, not the TextMate grammar schema)
   - `TBL`, `SEQ`, `FUNC`, `NAME`, `PROPS`, `COL`, `CONSTRAINT`, `IDX`, `PARAM`, `RET`, `BODY`.
   - Specify required/optional properties and allowed value types. See [schema.md](schema.md) for the current draft rules enforced by the parser (e.g., `CONSTRAINT: name(col1, col2); type=pk|unq`, `IDX: name(col1, col2=ASC); type=btree`).
2. Update parsing/validation
   - Edit `server/src/server.ts`:
     - Expand `validateDbDef` to parse each directive line into a structured object.
     - Validate required keys, value types, and duplicates; emit `Diagnostic` with ranges.
     - For `MAP`, add cross-reference checks (e.g., required mappings or missing substitutions).
   - Consider moving parsing into dedicated modules (e.g., `parser/`) if logic grows.
3. Strengthen completions
   - Use parsed context (current directive, inside PROPS list, etc.) to filter suggestions.
   - Suggest enums and known identifiers: table names, sequence names, function names extracted from open documents or indexed workspace files.
4. Add workspace indexing
   - Use `workspaceFolders` and `dbcompiler.schemaPaths` (declared in client `package.json`) to locate `.dbdef`/`.dbmap` files.
   - On `workspace/didChangeWatchedFiles`, rebuild an index of symbols (tables, sequences, functions, map keys) for cross-file validation and completions.
5. Improve diagnostics user experience
   - Use `DiagnosticSeverity.Error` for hard failures and `Warning` for softer issues.
   - Provide `code` values to classify errors (e.g., `missing-props`, `unknown-directive`).
   - Add related information for cross-file references when applicable.
6. Optional: Hover and Go-to Definition
   - Implement `textDocument/hover` to show directive/property docs.
   - Implement `textDocument/definition` to jump from placeholders to definitions (e.g., `$queue_table_name$` → table name location in `.dbmap`).

## TextMate grammar notes (quick inference from samples)
- `.dbmap` lines follow `MAP: key=value`, with `$placeholder$` allowed in values.
- `.dbdef` is directive-based, one directive per line:
  - Top-level markers: `TBL`, `SEQ`, `FUNC` (no trailing colon); subsequent `NAME:` line to set identifier.
  - `PROPS:` accepts semicolon-separated `key=value` pairs.
  - Detail directives (`COL:`, `CONSTRAINT:`, `IDX:`, `PARAM:`, `RET:`) carry their content after the colon; values may include parentheses and semicolons inside the directive payload.
  - `BODY:` starts function body; body ends at `BODY;`.
- Placeholders: `$IDENT$` can appear in names and values; grammar scopes them as placeholders for highlighting.

## Recommended next improvements
- Formalize the directive grammar and create a small parser (tokenizer + line classifier) to replace regex-based checks.
- Add tests (e.g., with `vitest` or `mocha`) for parser, diagnostics, and completion providers.
- Add CI to run `npm test` and `npm run compile`.
- Publish a VSIX when validation rules are stable.
