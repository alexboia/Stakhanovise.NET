# Developer Guidelines

## 1. Async + deadlocks: safe patterns

- **Default for library code**: use `ConfigureAwait(false)` on awaits unless you must resume on a specific context (UI/ASP.NET classic). This prevents continuations from requiring the thread that might be synchronously blocked elsewhere.
- **Synchronous waits on async**: if you must block (`GetAwaiter().GetResult()` / `.Result` / `.Wait()`), only do so on tasks built with `ConfigureAwait(false)` in their async path. Otherwise, you can deadlock when the continuation needs the blocked context.
- **Prefer async disposal**: implement `IAsyncDisposable` + `await using` for components that need async shutdown. Keep a synchronous `Dispose` fallback only if needed, and in that fallback block on a pipeline that avoids context capture (see above).
- **Dispose/DisposeAsync pattern**: make `DisposeAsync` the single source of truth for teardown, and have `Dispose` call `DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult()`. Both should call `GC.SuppressFinalize(this)`.
- **TaskCompletionSource**: create with `TaskCreationOptions.RunContinuationsAsynchronously` to avoid running user continuations inline on thread pool callback threads.
- **Stop/Shutdown flows**: for stop sequences that may be called from `Dispose`, ensure every await in the stop path uses `ConfigureAwait(false)`, and if you must block, use `task.ConfigureAwait(false).GetAwaiter().GetResult()` to avoid `AggregateException` wrapping and reduce deadlock risk.
- **StartAsync patterns**: when exposing `StartAsync`, return a `Task` that completes only after the worker is initialized. If startup succeeds, `TrySetResult(true)`; if startup fails before that, `TrySetException`. Avoid leaving callers waiting indefinitely—always complete the startup TCS even if the worker is later canceled.

## 2. When to NOT use ConfigureAwait(false)

- When the continuation needs a specific context (UI dispatcher, ASP.NET classic request context). In these cases omit `ConfigureAwait(false)` intentionally.
- In ASP.NET Core/background services that do not expose a synchronization context, it usually does not matter, but using `ConfigureAwait(false)` in library code remains a safe default.

## 3. Blocking guidance

- Avoid blocking on async in UI/request threads. If blocking is unavoidable (e.g., Dispose), ensure the awaited code does not capture context and expect the original thread.
- Prefer exposing async APIs (e.g., `StopAsync`, `DisposeAsync`) so callers can await rather than block.

## 4. Testing notes

- In tests, prefer `await` over `.Result`/`.Wait()` to avoid hangs on STA or custom synchronization contexts.
