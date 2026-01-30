# Database
1. The DB compiler needs the ability to flag a column as migratable and generate ALTER code for adding the column if the table exists.	
2. The DB compiler also needs the ability to define a custom migration script
3. The setup process should include also run migrations on startup

# Result processing
1. Results need a lease after which a watchdog "process" can re-queue them if they are stuck.
2. When a worker dequeues, mark the result row as Processing, set `processing_lease_until = now + lease`
3. While running, periodically heartbeat: `UPDATE … SET processing_lease_until = now + lease, last_heartbeat_at = now WHERE task_id = ... AND status = Processing`.
4. A watchdog (periodic job) looks for `status = Processing AND processing_lease_until < now` and re-enqueues from the result row (same task_id). The current re-enqueue logic should work just fine. Also scanning for dead results can make use of FOR UPDATE SKIP LOCKED.
5. Index the result table on `(status, processing_lease_until)` to make rescues cheap.
6. Result back-up can stay but should not overlap with the rescue watchdog.
7. Provide a file-based result queue back-up for basic persistent storage.

This would require the following database columns:
	- `processing_lease_until` TIMESTAMP WITH TIME ZONE NULL
	- `last_heartbeat_at` TIMESTAMP WITH TIME ZONE NULL

!!! Heartbeat should be scoped to the executor currently running that particular task. 

# Setup
1. Should allow specifying the custom producers (need to inject the fan-out capability into the processing engine itself) and info directly via the setup API. Leave `DontRegisterOwnDependencies` around but mark it as deprecated. 

# Queue listener
How about the following double slow fallback: 
	- if the notification channel repeatedly times out BUT I DO get data or the listener connection is highly-unreliable, 
	- what if I quit re-establishing the connection altogether for and just slow poll for a while? 

Basically a low tech version of `ITaskQueueNotificationListener` which just uses a server timer. 
And another `ITaskQueueNotificationListener` implementation that will decide what to do and when to switch.

Would need some metrics to cover this as well -> requring new metric ID.

# Plugins
1. Only basic control surface for now, BUT this would require an initial implementation of a unified control bus that would mediate control on behalf of everyone.
2. The control bus would be coded against specific control topics and won't expose a UNIFIED INTERFACE, altough it WILL have an internally uninified implementation.
3. Access to metadata: registered executors/payload types, options (lease/heartbeat intervals, retry caps), build/version. - `IStakhanoviseSamokritikaProvider`?
4. Useful R/O hook points:
	- Lifecycle hooks: on engine start/stop, on poller start/stop, on worker start/stop.
	- Queue events: before/after dequeue, before/after result update, retry scheduled, task completed/failed, watchdog rescue invoked.
	- Notification channel events: on connect/reconnect/timeout/fallback.
5. Guards against misuse:
	- Isolation: invoke plugins via interfaces with try/catch; don't let plugin exceptions crash the engine. Log and optionally disable a misbehaving plugin after N failures.
	- Timeouts/budgets: run plugin callbacks with a timeout/cancellation token; keep them non-blocking of core paths (e.g., fire-and-forget or dispatch to a bounded queue).
	- Contracts: keep plugin interfaces narrow and read-only where possible (e.g., stats queries). For control operations, require explicit opt-in.
	- Backpressure protection: do not let plugin hooks enqueue unbounded work or block poller/worker threads; use bounded channels/queues for plugin work.
	- Versioning: version the plugin interfaces; provide compatibility shims or clear breakage signals.


We call plugin komrades.

# Initial control bus
Only start and stop the engine itself: `IStakhanoviceStartStopControlBus`
	- `StartAsync()`;
	- `StopAsync()`;

Debounce all calls and ensure things such as: 
- `Start -> Stop -> Start` would not de-facto change anything;
- `Start -> Stop -> Start -> Stop` would only stop once.

Require plugins to explicitly opt-in for control surface exposure and inject a control buss instance wrapper scoped to their particular plugin such that we can track mis-behaving ones.

# Checkpoint API

!!! BEST-EFFORT API TO HELP WITH IDEMPOTENCY AND RESUMABILITY OF TASKS !!!
!!! TASKS SHOULD STILL BE DESIGNED TO BE IDEMPOTENT AND RESUMABLE WITHOUT CHECKPOINTS !!!

## Reading a checkpoint:
```csharp
Checkpoint<TCheckpoint> checkpoint = await executionContext.GetGeckpointAsync<TCheckpoint>("KEY"); //internally bound to task type and ID
```
Where `Checkpoint<TPayload>`:
- `TCheckpoint Data`: the checkpoint data;
- `object OriginalPayload`: the original payload used when saving the checkpoint.
 
## Writing a checkpoint:

```csharp
CheckpointSaveResult checkpointSaveResult = await executionContext.SaveCheckpointAsync<TCheckpoint>("KEY", checkpointData);
```

Where `CheckpointSaveResult`:
- `bool IsNew`: whether the checkpoint was newly created or updated;
- `CheckPointStatus status`: the status of the save operation (Success, DroppedDueToNoise, DroppedDueToSizeConstraints, CheckpointsDisabled, Unserializable). A checkpoint might get dropped if executor is too nosiy or too large(?).
- `IEnumerable<string> RemovedCheckpoints`: the checkpoints removed to make space for this one (if any).

## Cleaning up checkpoints:
1. When the executor completes successfully, all checkpoints associated with the task are deleted.
2. When a task is no longer retried (failed permanently), all checkpoints associated with the task are deleted.
3. By the executor code via:

```csharp
CheckpointRemovalResult checkpointSaveResult = await executionContext.RemoveCheckpointAsync("KEY");
```

Where `CheckpointRemovalResult`:
- `bool IsRemoved`: whether the checkpoint was found and removed;

### Other issues:
1. Safety: only allow reads/writes for the currently leased task (match task_id + Processing status + current executor/worker id if you track one). Run under the same transaction/connection as the task's heartbeat/update when possible.
2. Limits: cap total checkpoints per task and max payload size to avoid bloat. Consider TTL on checkpoints if tasks churn a lot.
3. Observability: metrics for checkpoint reads/writes and purges; maybe a "checkpoint size" metric.
4. Defaults: off by default; opt-in at setup. Keep it pluggable so users can swap storage (e.g., file/memory/DB). Even if off by default, ensure executors wont break if used.

## Checkpoints and heartbeating (bundling and backpressure):
A bounded heartbeat/write queue makes sense—just make the backpressure explicit:
	- Bound the queue and drop/skip heartbeats/checkpoints when full; count and expose "dropped heartbeats" so you can detect misbehaving executors.
	- Rate-limit per task/executor to avoid spamming; optionally temporarily halt checkpoint writes for a task that exceeds the rate.
	- Prefer to co-write heartbeat + checkpoint in the same transaction/connection as the task's lease/update when possible; otherwise accept eventual consistency.
	- A timer-driven worker heartbeat is fine; to keep it predictable.
	- Give heartbeat writes highest priority over opportunistic checkpoint writes; coalesce when both are pending (write heartbeat+checkpoint in one update if possible).
	- If a task explicitly saves a checkpoint, let it bump the lease in the same write/transaction; otherwise accept eventual consistency from the timer.

## Questions:
1. Do we want to allow the user to specify checkpoint scoping (i.e. IDs other than task ID)?
2. Enforce a size limit on checkpoints? If so, how much? How much globally (i.e. entire checkpoint set per owner process, per task type, per key)?
3. Opt-in retention policy to remove old checkpoints to make space for new ones?


## Adaptive heartbeat?
Not automatically. 
Keep a safe default (e.g., lease ~ P90 processing time with headroom, heartbeat at ~½–¼ of the lease) 
and let users override per task type. 
If we want adaptive behavior, must make it opt-in and bounded: 
use recent metrics (P90/95 per task type) to suggest or set lease/heartbeat with min/max caps, 
mustn't let it drift unbounded.
