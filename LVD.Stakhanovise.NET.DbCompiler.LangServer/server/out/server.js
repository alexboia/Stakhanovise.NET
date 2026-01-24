"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
const node_1 = require("vscode-languageserver/node");
const vscode_languageserver_textdocument_1 = require("vscode-languageserver-textdocument");
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
const parser_1 = require("./parser");
const completions_1 = require("./completions");
const connection = (0, node_1.createConnection)(node_1.ProposedFeatures.all);
const documents = new node_1.TextDocuments(vscode_languageserver_textdocument_1.TextDocument);
const placeholderIndex = new Set();
connection.onInitialize((_params) => {
    return {
        capabilities: {
            textDocumentSync: node_1.TextDocumentSyncKind.Incremental,
            completionProvider: {
                resolveProvider: false,
                triggerCharacters: ["$"]
            }
        }
    };
});
connection.onInitialized(() => {
    documents.all().forEach(validateDocument);
    rebuildPlaceholders();
});
documents.onDidChangeContent((change) => {
    validateDocument(change.document);
    updatePlaceholders(change.document);
});
connection.onDidChangeWatchedFiles(() => {
    documents.all().forEach(validateDocument);
    rebuildPlaceholders();
});
connection.onCompletion((params) => {
    const document = documents.get(params.textDocument.uri);
    if (!document) {
        return [];
    }
    const lines = document.getText().split(/\r?\n/);
    const line = lines[params.position.line] ?? "";
    const languageId = inferLanguageId(document.uri);
    if (isPlaceholderContext(line, params.position.character)) {
        return (0, completions_1.providePlaceholderCompletions)(placeholderIndex);
    }
    if (languageId === "dbmap") {
        return (0, completions_1.provideDbMapCompletions)(line, params.position.character);
    }
    return (0, completions_1.provideDbDefCompletions)(line, params.position.character);
});
documents.listen(connection);
connection.listen();
function validateDocument(document) {
    const languageId = inferLanguageId(document.uri);
    const parsed = (0, parser_1.parseDocument)(languageId, document.getText());
    const diagnostics = parsed.map(toLspDiagnostic);
    connection.sendDiagnostics({ uri: document.uri, diagnostics: diagnostics });
}
function inferLanguageId(uri) {
    return uri.toLowerCase().endsWith(".dbmap") ? "dbmap" : "dbdef";
}
function toLspDiagnostic(parsed) {
    return {
        severity: parsed.severity,
        code: parsed.code,
        range: {
            start: { line: parsed.line, character: parsed.startChar },
            end: { line: parsed.line, character: parsed.endChar }
        },
        message: parsed.message,
        source: "dbcompiler"
    };
}
function updatePlaceholders(document) {
    const kind = inferLanguageId(document.uri);
    if (kind !== "dbmap") {
        return;
    }
    collectPlaceholders(document.getText()).forEach(ph => placeholderIndex.add(ph));
}
function rebuildPlaceholders() {
    placeholderIndex.clear();
    documents.all().forEach(doc => updatePlaceholders(doc));
    indexDbMapFilesFromDisk();
}
function isPlaceholderContext(line, character) {
    const before = line.slice(0, character);
    const lastDollar = before.lastIndexOf("$");
    if (lastDollar === -1) {
        return false;
    }
    if (character === lastDollar + 1) {
        return true;
    }
    const nextDollar = line.indexOf("$", character);
    if (nextDollar === -1) {
        return true; // open placeholder, not yet closed
    }
    return lastDollar < character && character <= nextDollar;
}
function collectPlaceholders(text) {
    const placeholders = [];
    (text || "").split("\n").map(l => l.trim()).forEach(line => {
        if (line.toUpperCase().startsWith("MAP:")) {
            const start = line.indexOf(":") + 1;
            const end = line.indexOf("=");
            const key = line.substring(start, end).trim();
            if (key) {
                placeholders.push(`$${key}$`);
                placeholders.push(key); // also add bare key for looser matching
            }
            const value = line.substring(end + 1).trim();
            const matches = value.match(/\$[A-Za-z0-9_]+\$/g) ?? [];
            matches.forEach(m => {
                placeholders.push(m);
                placeholders.push(m.replace(/\$/g, ""));
            });
        }
    });
    return placeholders;
}
function indexDbMapFilesFromDisk() {
    try {
        const root = path.resolve(__dirname, "..", "..");
        const sampleDir = path.join(root, "sample");
        const files = fs.readdirSync(sampleDir).filter(f => f.toLowerCase().endsWith(".dbmap"));
        files.forEach(file => {
            const content = fs.readFileSync(path.join(sampleDir, file), "utf8");
            collectPlaceholders(content).forEach(ph => placeholderIndex.add(ph));
        });
    }
    catch {
        // best effort
    }
}
//# sourceMappingURL=server.js.map