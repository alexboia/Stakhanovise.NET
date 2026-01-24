"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.parseDocument = parseDocument;
exports.parseDbMapText = parseDbMapText;
exports.parseDbDefText = parseDbDefText;
const node_1 = require("vscode-languageserver/node");
function parseDocument(kind, text) {
    return kind === "dbmap" ? parseDbMapText(text) : parseDbDefText(text);
}
function parseDbMapText(text) {
    const diagnostics = [];
    const lines = text.split(/\r?\n/);
    lines.forEach((line, index) => {
        const trimmed = line.trim();
        if (!trimmed || trimmed.startsWith("--")) {
            return;
        }
        if (!trimmed.startsWith("MAP:")) {
            diagnostics.push(diag(index, 0, line.length, "Line must start with MAP: directive", "map-prefix"));
            return;
        }
        const content = trimmed.substring(4).trim();
        if (!content.includes("=")) {
            diagnostics.push(diag(index, 4, line.length, "MAP directive must contain key=value", "map-key-value"));
            return;
        }
        const [rawKey, rawValue] = content.split(/=(.+)/).slice(0, 2);
        const key = rawKey?.trim() ?? "";
        const value = rawValue?.trim() ?? "";
        if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(key)) {
            diagnostics.push(diag(index, 4, 4 + key.length, "Invalid map key; use letters, digits, underscore", "map-key"));
        }
        if (!value) {
            diagnostics.push(diag(index, 4 + (rawKey?.length ?? 0), line.length, "Missing map value", "map-value"));
        }
    });
    return diagnostics;
}
function parseDbDefText(text) {
    const diagnostics = [];
    const lines = text.split(/\r?\n/);
    let inBody = false;
    lines.forEach((line, index) => {
        const trimmed = line.trim();
        if (!trimmed || trimmed.startsWith("--")) {
            return;
        }
        if (inBody) {
            if (trimmed === "BODY;") {
                inBody = false;
            }
            return;
        }
        if (trimmed.startsWith("BODY:")) {
            inBody = true;
            return;
        }
        const directive = getDirective(trimmed);
        if (!directive) {
            diagnostics.push(diag(index, 0, line.length, "Unknown directive; expected TBL, SEQ, FUNC, NAME, PROPS, COL, CONSTRAINT, IDX, PARAM, RET or BODY", "unknown-directive"));
            return;
        }
        switch (directive) {
            case "PROPS": {
                validateProps(line, index, diagnostics);
                return;
            }
            case "COL": {
                validateDetailWithRequiredProps(line, index, "COL", ["type"], diagnostics);
                return;
            }
            case "CONSTRAINT": {
                validateConstraint(line, index, diagnostics);
                return;
            }
            case "IDX": {
                validateIndex(line, index, diagnostics);
                return;
            }
            case "PARAM": {
                validateDetailWithRequiredProps(line, index, "PARAM", ["type"], diagnostics);
                return;
            }
            case "RET": {
                validateReturn(line, index, diagnostics);
                return;
            }
            case "TBL":
            case "SEQ":
            case "FUNC": {
                return;
            }
            case "NAME": {
                if (!trimmed.includes(":") || trimmed.endsWith(":")) {
                    diagnostics.push(diag(index, 0, line.length, "NAME directive must provide an identifier", "name-missing"));
                }
                return;
            }
            default:
                return;
        }
    });
    if (inBody) {
        const lastLine = Math.max(0, lines.length - 1);
        diagnostics.push(diag(lastLine, 0, lines[lastLine]?.length ?? 1, "Missing closing BODY; directive", "body-unclosed"));
    }
    return diagnostics;
}
function getDirective(trimmed) {
    if (/^(TBL|SEQ|FUNC)\b/.test(trimmed)) {
        return trimmed.split(/\s+/)[0];
    }
    if (/^NAME:/.test(trimmed)) {
        return "NAME";
    }
    if (/^PROPS:/.test(trimmed)) {
        return "PROPS";
    }
    if (/^COL:/.test(trimmed)) {
        return "COL";
    }
    if (/^CONSTRAINT:/.test(trimmed)) {
        return "CONSTRAINT";
    }
    if (/^IDX:/.test(trimmed)) {
        return "IDX";
    }
    if (/^PARAM:/.test(trimmed)) {
        return "PARAM";
    }
    if (/^RET:/.test(trimmed)) {
        return "RET";
    }
    if (/^BODY[:;]/.test(trimmed)) {
        return "BODY";
    }
    return undefined;
}
function validateProps(line, lineIndex, diagnostics) {
    const rest = line.trim().substring("PROPS:".length).trim();
    if (!rest) {
        diagnostics.push(diag(lineIndex, 0, line.length, "PROPS directive requires key=value entries", "props-empty"));
        return;
    }
    const entries = rest.split(";").map((entry) => entry.trim()).filter(Boolean);
    entries.forEach((entry) => {
        const offset = line.indexOf(entry);
        if (!entry.includes("=")) {
            diagnostics.push(diag(lineIndex, offset, offset + entry.length, "Property must use key=value", "props-key-value"));
            return;
        }
        const [key, value] = entry.split(/=(.+)/).slice(0, 2);
        if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(key.trim())) {
            diagnostics.push(diag(lineIndex, offset, offset + key.length, "Invalid property key", "props-key"));
        }
        if ((value?.trim() ?? "").length === 0) {
            diagnostics.push(diag(lineIndex, offset, offset + entry.length, "Property value is missing", "props-value"));
        }
    });
}
function validateDetailWithRequiredProps(line, lineIndex, directive, requiredKeys, diagnostics) {
    const match = new RegExp(`^${directive}:\\s*([^\\(\\s]+)\\s*\\((.*)\\)\\s*$`).exec(line.trim());
    if (!match) {
        diagnostics.push(diag(lineIndex, 0, line.length, `${directive} directive must look like ${directive}: name(key=value; ...)`, `${directive.toLowerCase()}-shape`));
        return;
    }
    const name = match[1];
    const body = match[2];
    if (!/^[A-Za-z_][A-Za-z0-9_$]*$/.test(name)) {
        diagnostics.push(diag(lineIndex, 0, name.length + directive.length + 1, `${directive} name is invalid`, `${directive.toLowerCase()}-name`));
    }
    const props = splitProps(body);
    const propKeys = new Set();
    props.forEach(prop => {
        if (!prop.includes("=")) {
            diagnostics.push(diag(lineIndex, 0, line.length, `${directive} properties must use key=value`, `${directive.toLowerCase()}-prop-format`));
            return;
        }
        const [key, value] = prop.split(/=(.+)/).slice(0, 2).map(p => p.trim());
        if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(key)) {
            diagnostics.push(diag(lineIndex, 0, line.length, `${directive} property key is invalid`, `${directive.toLowerCase()}-prop-key`));
        }
        if (!value) {
            diagnostics.push(diag(lineIndex, 0, line.length, `${directive} property value is missing`, `${directive.toLowerCase()}-prop-value`));
        }
        if (propKeys.has(key)) {
            diagnostics.push(diag(lineIndex, 0, line.length, `${directive} property '${key}' is duplicated`, `${directive.toLowerCase()}-prop-dup`));
        }
        propKeys.add(key);
    });
    requiredKeys.forEach(req => {
        if (!propKeys.has(req)) {
            diagnostics.push(diag(lineIndex, 0, line.length, `${directive} requires property '${req}'`, `${directive.toLowerCase()}-prop-required`));
        }
    });
}
function validateConstraint(line, lineIndex, diagnostics) {
    const trimmed = line.trim();
    const match = /^CONSTRAINT:\s*([A-Za-z_][A-Za-z0-9_$]*)\s*\(([^)]*)\)\s*(;.*)?$/.exec(trimmed);
    if (!match) {
        diagnostics.push(diag(lineIndex, 0, line.length, "CONSTRAINT must look like CONSTRAINT: name(col1, col2); type=pk", "constraint-shape"));
        return;
    }
    const name = match[1];
    const columnsRaw = match[2].trim();
    const trailingPropsRaw = (match[3] ?? "").replace(/^;\s*/, "").trim();
    if (!/^[A-Za-z_][A-Za-z0-9_$]*$/.test(name)) {
        diagnostics.push(diag(lineIndex, 0, name.length + "CONSTRAINT:".length, "CONSTRAINT name is invalid", "constraint-name"));
    }
    if (!columnsRaw) {
        diagnostics.push(diag(lineIndex, 0, line.length, "CONSTRAINT must list at least one column inside ()", "constraint-columns"));
    }
    else {
        const columns = columnsRaw.split(",").map(col => col.trim()).filter(Boolean);
        columns.forEach(col => {
            if (!/^[A-Za-z_][A-Za-z0-9_$]*$/.test(col)) {
                diagnostics.push(diag(lineIndex, 0, line.length, `Invalid column name '${col}' in CONSTRAINT`, "constraint-column-name"));
            }
        });
    }
    if (!trailingPropsRaw) {
        // type is optional
    }
    else {
        const props = splitProps(trailingPropsRaw);
        const propKeys = new Set();
        props.forEach(prop => {
            if (!prop.includes("=")) {
                diagnostics.push(diag(lineIndex, 0, line.length, "CONSTRAINT properties must use key=value", "constraint-prop-format"));
                return;
            }
            const [key, value] = prop.split(/=(.+)/).slice(0, 2).map(p => p.trim());
            if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(key)) {
                diagnostics.push(diag(lineIndex, 0, line.length, "CONSTRAINT property key is invalid", "constraint-prop-key"));
            }
            if (!value) {
                diagnostics.push(diag(lineIndex, 0, line.length, "CONSTRAINT property value is missing", "constraint-prop-value"));
            }
            if (propKeys.has(key)) {
                diagnostics.push(diag(lineIndex, 0, line.length, `CONSTRAINT property '${key}' is duplicated`, "constraint-prop-dup"));
            }
            if (key === "type" && !/^pk$|^unq$/i.test(value)) {
                diagnostics.push(diag(lineIndex, 0, line.length, "CONSTRAINT type must be pk or unq", "constraint-type-value"));
            }
            propKeys.add(key);
        });
    }
}
function validateIndex(line, lineIndex, diagnostics) {
    const trimmed = line.trim();
    const match = /^IDX:\s*([A-Za-z_][A-Za-z0-9_$]*)\s*\(([^)]*)\)\s*(;.*)?$/.exec(trimmed);
    if (!match) {
        diagnostics.push(diag(lineIndex, 0, line.length, "IDX must look like IDX: name(col1, col2=ASC); type=btree", "idx-shape"));
        return;
    }
    const name = match[1];
    const columnsRaw = match[2].trim();
    const trailingPropsRaw = (match[3] ?? "").replace(/^;\s*/, "").trim();
    if (!/^[A-Za-z_][A-Za-z0-9_$]*$/.test(name)) {
        diagnostics.push(diag(lineIndex, 0, name.length + "IDX:".length, "IDX name is invalid", "idx-name"));
    }
    if (!columnsRaw) {
        diagnostics.push(diag(lineIndex, 0, line.length, "IDX must list at least one column", "idx-columns"));
    }
    else {
        const columns = columnsRaw.split(",").map(col => col.trim()).filter(Boolean);
        columns.forEach(colSpec => {
            const [col, dir] = colSpec.split("=").map(part => part.trim());
            if (!/^[A-Za-z_][A-Za-z0-9_$]*$/.test(col)) {
                diagnostics.push(diag(lineIndex, 0, line.length, `Invalid column name '${col}' in IDX`, "idx-column-name"));
            }
            if (dir && !/^ASC$|^DESC$/i.test(dir)) {
                diagnostics.push(diag(lineIndex, 0, line.length, "IDX direction must be ASC or DESC", "idx-direction"));
            }
        });
    }
    if (trailingPropsRaw) {
        const props = splitProps(trailingPropsRaw);
        const propKeys = new Set();
        props.forEach(prop => {
            if (!prop.includes("=")) {
                diagnostics.push(diag(lineIndex, 0, line.length, "IDX properties must use key=value", "idx-prop-format"));
                return;
            }
            const [key, value] = prop.split(/=(.+)/).slice(0, 2).map(p => p.trim());
            if (!/^[A-Za-z_][A-Za-z0-9_]*$/.test(key)) {
                diagnostics.push(diag(lineIndex, 0, line.length, "IDX property key is invalid", "idx-prop-key"));
            }
            if (!value) {
                diagnostics.push(diag(lineIndex, 0, line.length, "IDX property value is missing", "idx-prop-value"));
            }
            if (propKeys.has(key)) {
                diagnostics.push(diag(lineIndex, 0, line.length, `IDX property '${key}' is duplicated`, "idx-prop-dup"));
            }
            propKeys.add(key);
        });
    }
}
function validateReturn(line, lineIndex, diagnostics) {
    const trimmed = line.trim();
    if (/^RET:\s*table\(/i.test(trimmed)) {
        // RET: table(...) form; ensure parentheses closed
        if (!trimmed.endsWith(")")) {
            diagnostics.push(diag(lineIndex, 0, line.length, "RET table(...) must close parentheses", "ret-parens"));
        }
        return;
    }
    // Fallback: treat like detail with required type
    validateDetailWithRequiredProps(line, lineIndex, "RET", ["type"], diagnostics);
}
function splitProps(body) {
    return body
        .split(";")
        .map(p => p.trim())
        .filter(Boolean);
}
function diag(line, startChar, endChar, message, code, severity = node_1.DiagnosticSeverity.Warning) {
    return { line, startChar, endChar, message, severity, code };
}
//# sourceMappingURL=parser.js.map