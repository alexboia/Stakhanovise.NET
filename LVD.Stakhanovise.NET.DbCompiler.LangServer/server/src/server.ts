import {
  createConnection,
  TextDocuments,
  ProposedFeatures,
  InitializeParams,
  InitializeResult,
  TextDocumentSyncKind,
  Diagnostic,
  TextDocumentPositionParams,
  CompletionItem
} from "vscode-languageserver/node";
import { TextDocument } from "vscode-languageserver-textdocument";
import * as fs from "fs";
import * as path from "path";
import { parseDocument, ParsedDiagnostic } from "./parser";
import { provideDbDefCompletions, provideDbMapCompletions, providePlaceholderCompletions } from "./completions";

const connection = createConnection(ProposedFeatures.all);
const documents: TextDocuments<TextDocument> = new TextDocuments(TextDocument);
const placeholderIndex: Set<string> = new Set();

connection.onInitialize((_params: InitializeParams): InitializeResult => {
  return {
	capabilities: {
	  textDocumentSync: TextDocumentSyncKind.Incremental,
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

// Updates arrive via client textDocument/didChange; TextDocuments keeps them in sync.
documents.onDidChangeContent((change: { document: TextDocument }) => {
	validateDocument(change.document);
	updatePlaceholders(change.document);
});

connection.onDidChangeWatchedFiles(() => {
	documents.all().forEach(validateDocument);
	rebuildPlaceholders();
});

connection.onCompletion((params: TextDocumentPositionParams): CompletionItem[] => {
	const document = documents.get(params.textDocument.uri);
	if (!document) {
		return [];
	}

	const lines = document.getText().split(/\r?\n/);
	const line = lines[params.position.line] ?? "";
	const languageId = inferLanguageId(document.uri);

	if (isPlaceholderContext(line, params.position.character)) {
		return providePlaceholderCompletions(placeholderIndex);
	}

	if (languageId === "dbmap") {
		return provideDbMapCompletions(line, params.position.character);
	}
	return provideDbDefCompletions(line, params.position.character);
});

documents.listen(connection);
connection.listen();

function validateDocument(document: TextDocument): void {
	const languageId = inferLanguageId(document.uri);
	const parsed = parseDocument(languageId, document.getText());
	const diagnostics = parsed.map(toLspDiagnostic);

	connection.sendDiagnostics({ uri: document.uri, diagnostics: diagnostics as Diagnostic[] });
}

function inferLanguageId(uri: string): "dbmap" | "dbdef" {
  	return uri.toLowerCase().endsWith(".dbmap") ? "dbmap" : "dbdef";
}

function toLspDiagnostic(parsed: ParsedDiagnostic): Diagnostic {
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

function updatePlaceholders(document: TextDocument): void {
	const kind = inferLanguageId(document.uri);
	if (kind !== "dbmap") {
		return;
	}
	collectPlaceholders(document.getText()).forEach(ph => placeholderIndex.add(ph));
}

function rebuildPlaceholders(): void {
	placeholderIndex.clear();
	documents.all().forEach(doc => updatePlaceholders(doc));
	indexDbMapFilesFromDisk();
}

function isPlaceholderContext(line: string, character: number): boolean {
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

function collectPlaceholders(text: string): string[] {
  const placeholders: string[] = [];
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

function indexDbMapFilesFromDisk(): void {
	try {
		const root = path.resolve(__dirname, "..", "..");
		const sampleDir = path.join(root, "sample");
		const files = fs.readdirSync(sampleDir).filter(f => f.toLowerCase().endsWith(".dbmap"));
		files.forEach(file => {
			const content = fs.readFileSync(path.join(sampleDir, file), "utf8");
			collectPlaceholders(content).forEach(ph => placeholderIndex.add(ph));
		});
	} catch {
	// best effort
	}
}

