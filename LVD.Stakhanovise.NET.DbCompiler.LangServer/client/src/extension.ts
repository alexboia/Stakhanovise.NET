import * as path from "path";

import { 
	workspace, 
	ExtensionContext 
} from "vscode";

import { 
	LanguageClient, 
	LanguageClientOptions, 
	ServerOptions, 
	TransportKind 
} from "vscode-languageclient/node";

let client: LanguageClient | undefined;

export function activate(context: ExtensionContext): void {
	const serverModule = context.asAbsolutePath(path.join("..", "server", "out", "server.js"));

	const serverOptions: ServerOptions = {
		run: { module: serverModule, transport: TransportKind.ipc },
		debug: {
			module: serverModule,
			transport: TransportKind.ipc,
			options: { execArgv: ["--nolazy", "--inspect=6009"] }
		}
	};

	const clientOptions: LanguageClientOptions = {
		documentSelector: [
			{ scheme: "file", language: "dbdef" },
			{ scheme: "file", language: "dbmap" }
		],
		synchronize: {
			fileEvents: workspace.createFileSystemWatcher("**/*.{dbdef,dbmap}")
		}
	};

	client = new LanguageClient(
		"dbcompilerLangServer",
		"DbCompiler Language Server",
		serverOptions,
		clientOptions
	);

	client.start();
}

export function deactivate(): Thenable<void> | undefined {
	if (!client) {
		return undefined;
	}
	return client.stop();
}
