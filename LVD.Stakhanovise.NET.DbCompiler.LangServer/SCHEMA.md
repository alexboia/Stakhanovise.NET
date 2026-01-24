# DbCompiler DSL Directive Schema (draft)

This document defines the intended shape of `.dbdef` and `.dbmap` directives for validation and tooling.

## Common conventions
- Identifiers: `^[A-Za-z_][A-Za-z0-9_$]*$`
- Property keys: `^[A-Za-z_][A-Za-z0-9_]*$`
- Property lists: semicolon-separated `key=value` pairs inside parentheses or after `PROPS:`
- Placeholders: `$IDENT$` allowed in identifiers and values
- Comments: `--` to end of line

## `.dbmap`
- Lines: `MAP: key=value`
  - `key`: identifier
  - `value`: non-empty string (may contain placeholders)

## `.dbdef` directives
- `TBL` / `SEQ` / `FUNC`
  - Marker lines; next `NAME:` should provide the identifier.
- `NAME: identifier`
  - Identifier required.
- `PROPS: key=value; ...`
  - At least one `key=value`; keys must be identifiers; values non-empty.
- `COL: name(key=value; ...)`
  - `name` required; properties required: `type`.
- `CONSTRAINT: name(col1, col2, ...); type=...` (columns only inside parens; properties after parens)
  - `name` required; column list required; properties required: `type` (e.g., pk, unq, fk, chk).
- `IDX: name(col1, col2=ASC, col3=DESC); type=...`
  - `name` required; column list required.
  - Each column may optionally specify direction `=ASC|DESC` (case-insensitive).
  - Properties after parens are optional; `type` is optional.
- `PARAM: name(key=value; ...)`
  - `name` required; properties required: `type`; optional `direction`.
- `RET: table(...)` or `RET: name(key=value; ...)`
  - If `table(...)`, just ensure balanced parentheses; otherwise require `type`.
- `BODY:` ... `BODY;`
  - Marks function body start/end; must be balanced.

## Notes / future refinement
- Enumerate allowable `type` values per directive (e.g., constraint types, index types, SQL data types) when the full spec is available.
- Add cross-file checks: ensure placeholders used in `.dbdef` resolve via `.dbmap` entries.
- Add uniqueness checks (e.g., duplicate column names, constraint names) within a table/function scope once structure is modeled.
