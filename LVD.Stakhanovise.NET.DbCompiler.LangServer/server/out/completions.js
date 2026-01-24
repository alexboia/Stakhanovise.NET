"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.provideDbDefCompletions = provideDbDefCompletions;
exports.provideDbMapCompletions = provideDbMapCompletions;
exports.providePlaceholderCompletions = providePlaceholderCompletions;
const node_1 = require("vscode-languageserver/node");
function provideDbDefCompletions(line, character) {
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
function provideDbMapCompletions(_line, _character) {
    return mapKeyCompletions;
}
function providePlaceholderCompletions(placeholders) {
    const seen = new Set();
    return Array.from(placeholders)
        .filter(ph => {
        if (seen.has(ph)) {
            return false;
        }
        seen.add(ph);
        return true;
    })
        .sort()
        .map(ph => {
        return {
            label: ph,
            insertText: ph,
            filterText: ph,
            kind: node_1.CompletionItemKind.Variable,
            detail: "Placeholder from dbmap"
        };
    });
}
const directiveCompletions = [
    { label: "TBL", kind: node_1.CompletionItemKind.Keyword, detail: "Table definition" },
    { label: "SEQ", kind: node_1.CompletionItemKind.Keyword, detail: "Sequence definition" },
    { label: "FUNC", kind: node_1.CompletionItemKind.Keyword, detail: "Function definition" },
    { label: "NAME:", kind: node_1.CompletionItemKind.Keyword, detail: "Set identifier" },
    { label: "PROPS:", kind: node_1.CompletionItemKind.Keyword, detail: "Set properties" },
    { label: "COL:", kind: node_1.CompletionItemKind.Keyword, detail: "Column definition" },
    { label: "CONSTRAINT:", kind: node_1.CompletionItemKind.Keyword, detail: "Constraint definition" },
    { label: "IDX:", kind: node_1.CompletionItemKind.Keyword, detail: "Index definition" },
    { label: "PARAM:", kind: node_1.CompletionItemKind.Keyword, detail: "Function parameter" },
    { label: "RET:", kind: node_1.CompletionItemKind.Keyword, detail: "Function return definition" },
    { label: "BODY:", kind: node_1.CompletionItemKind.Keyword, detail: "Begin function body" },
    { label: "BODY;", kind: node_1.CompletionItemKind.Keyword, detail: "End function body" }
];
const propsCompletions = [
    { label: "title=", kind: node_1.CompletionItemKind.Property, detail: "Human readable title" },
    { label: "description=", kind: node_1.CompletionItemKind.Property, detail: "Description" },
    { label: "start=", kind: node_1.CompletionItemKind.Property, detail: "Sequence start" },
    { label: "increment=", kind: node_1.CompletionItemKind.Property, detail: "Sequence increment" },
    { label: "min_value=", kind: node_1.CompletionItemKind.Property, detail: "Sequence min" },
    { label: "max_value=", kind: node_1.CompletionItemKind.Property, detail: "Sequence max" },
    { label: "cache=", kind: node_1.CompletionItemKind.Property, detail: "Sequence cache" },
    { label: "language=", kind: node_1.CompletionItemKind.Property, detail: "Function language" },
    { label: "separator=", kind: node_1.CompletionItemKind.Property, detail: "Function body separator" }
];
const detailCompletions = [
    { label: "type=", kind: node_1.CompletionItemKind.Property, detail: "Set data type or constraint type" },
    { label: "not_null=", kind: node_1.CompletionItemKind.Property, detail: "Mark as NOT NULL (true/false)" },
    { label: "default=", kind: node_1.CompletionItemKind.Property, detail: "Default value" },
    { label: "direction=", kind: node_1.CompletionItemKind.Property, detail: "Parameter direction (in/out/inout)" }
];
const mapKeyCompletions = [
    { label: "queue_table_name=", kind: node_1.CompletionItemKind.Property, detail: "Queue table name" },
    { label: "results_queue_table_name=", kind: node_1.CompletionItemKind.Property, detail: "Results table name" },
    { label: "execution_time_stats_table_name=", kind: node_1.CompletionItemKind.Property, detail: "Execution stats table" },
    { label: "metrics_table_name=", kind: node_1.CompletionItemKind.Property, detail: "Metrics table" },
    { label: "new_task_notification_channel_name=", kind: node_1.CompletionItemKind.Property, detail: "Notification channel" },
    { label: "dequeue_function_name=", kind: node_1.CompletionItemKind.Property, detail: "Dequeue function" }
];
//# sourceMappingURL=completions.js.map