import { CompletionItem, CompletionItemKind } from "vscode-languageserver/node";

export function provideDbDefCompletions(line: string, character: number): CompletionItem[] {
	const beforeCursor = line.slice(0, character);
	const trimmed = beforeCursor.trim();

	if (!trimmed) {
		return directiveCompletions;
	}

	if (/^PROPS:\s*/.test(trimmed)) {
		return propsCompletions;
	}

	if (/^(COL|CONSTRAINT|IDX|PARAM|RET):/.test(trimmed)) {
		return detailCompletions;
	}

	return directiveCompletions;
}

export function provideDbMapCompletions(_line: string, _character: number): CompletionItem[] {
	return mapKeyCompletions;
}

export function providePlaceholderCompletions(placeholders: Set<string>): CompletionItem[] {
	return Array.from(placeholders)
	.sort()
	.map(ph => {
		return {
		label: ph,
		insertText: ph,
		filterText: ph,
		kind: CompletionItemKind.Variable,
		detail: "Placeholder from dbmap"
		} as CompletionItem;
	});
}

const directiveCompletions: CompletionItem[] = [
	{ label: "TBL", kind: CompletionItemKind.Keyword, detail: "Table definition" },
	{ label: "SEQ", kind: CompletionItemKind.Keyword, detail: "Sequence definition" },
	{ label: "FUNC", kind: CompletionItemKind.Keyword, detail: "Function definition" },
	{ label: "NAME:", kind: CompletionItemKind.Keyword, detail: "Set identifier" },
	{ label: "PROPS:", kind: CompletionItemKind.Keyword, detail: "Set properties" },
	{ label: "COL:", kind: CompletionItemKind.Keyword, detail: "Column definition" },
	{ label: "CONSTRAINT:", kind: CompletionItemKind.Keyword, detail: "Constraint definition" },
	{ label: "IDX:", kind: CompletionItemKind.Keyword, detail: "Index definition" },
	{ label: "PARAM:", kind: CompletionItemKind.Keyword, detail: "Function parameter" },
	{ label: "RET:", kind: CompletionItemKind.Keyword, detail: "Function return definition" },
	{ label: "BODY:", kind: CompletionItemKind.Keyword, detail: "Begin function body" },
	{ label: "BODY;", kind: CompletionItemKind.Keyword, detail: "End function body" }
];

const propsCompletions: CompletionItem[] = [
	{ label: "title=", kind: CompletionItemKind.Property, detail: "Human readable title" },
	{ label: "description=", kind: CompletionItemKind.Property, detail: "Description" },
	{ label: "start=", kind: CompletionItemKind.Property, detail: "Sequence start" },
	{ label: "increment=", kind: CompletionItemKind.Property, detail: "Sequence increment" },
	{ label: "min_value=", kind: CompletionItemKind.Property, detail: "Sequence min" },
	{ label: "max_value=", kind: CompletionItemKind.Property, detail: "Sequence max" },
	{ label: "cache=", kind: CompletionItemKind.Property, detail: "Sequence cache" },
	{ label: "language=", kind: CompletionItemKind.Property, detail: "Function language" },
	{ label: "separator=", kind: CompletionItemKind.Property, detail: "Function body separator" }
];

const detailCompletions: CompletionItem[] = [
	{ label: "type=", kind: CompletionItemKind.Property, detail: "Set data type or constraint type" },
	{ label: "not_null=", kind: CompletionItemKind.Property, detail: "Mark as NOT NULL (true/false)" },
	{ label: "default=", kind: CompletionItemKind.Property, detail: "Default value" },
	{ label: "direction=", kind: CompletionItemKind.Property, detail: "Parameter direction (in/out/inout)" }
];

const mapKeyCompletions: CompletionItem[] = [
	{ label: "queue_table_name=", kind: CompletionItemKind.Property, detail: "Queue table name" },
	{ label: "results_queue_table_name=", kind: CompletionItemKind.Property, detail: "Results table name" },
	{ label: "execution_time_stats_table_name=", kind: CompletionItemKind.Property, detail: "Execution stats table" },
	{ label: "metrics_table_name=", kind: CompletionItemKind.Property, detail: "Metrics table" },
	{ label: "new_task_notification_channel_name=", kind: CompletionItemKind.Property, detail: "Notification channel" },
	{ label: "dequeue_function_name=", kind: CompletionItemKind.Property, detail: "Dequeue function" }
];
