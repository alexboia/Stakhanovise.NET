## Contents

1. Compatibility
2. Advanced usage
3. Add-on packages
4. Samples
5. Architecture description

## Compatibility
<a name="sk-compatibility"></a>

Stakhanovise is built for:
- PostgrSQL 9.5 or higher;
- .NET Standard 2.0;
- `Npgsql 4.1.5` or higher;
- `Newtonsoft.Json 12.0.3` or higher.

## Basic information and usage

### 1. Task properties

- `Task Id` - Internal task identifier;
- `Lock handle Id` - Internal identifer used for locking during task acquisition;
- `Type` - Fully qualified name of payload CLR type;
- `Payload` - Task payload data, of type described by the `Type` field;
- `Source` - User provided description of which entity posted the task;
- `Priority` - User provided numerical descriptor of task priority (lower values mean higher priority);
- `Locked until` - A timestamp after which the task is retrievable (this provides a mechanism for posting a task now but having Stakhanovise attempt to dequeue it later);
- `Status` - Task status (see below for additional discussion).

#### Task statuses

- `Unprocessed` - The task has been posted to the queue and is pending processing;
- `Processing` - The task has been locked and processing has started;
- `Processed` - The task has been successfully processed;
- `Error` - The task has been processed, but an error occured and has been scheduled for retrial;
- `Faulted` - The task processing has errored out, beyond the configured number of processing errors threshold and has been flagged for special processing;
- `Fatal` - Fault state could not be handled;
- `Cancelled`.

Also see [`QueuedTaskStatus`](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.Common.Interfaces/Model/QueuedTaskStatus.cs).

#### Result properties

- `Task Id` - Task id is also used as result id;
- `Type` - Same as task type;
- `Source` - Same as task source;
- `Payload` - Same as task payload;
- `Status` - Same as task status;
- `Priority` - Same as task priority;
- `Processing time milliseconds` - Last processing duration, in milliseconds;
- `Last error` - Last error that has occurred (see [`QueuedTaskError`](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.Common.Interfaces/Model/QueuedTaskError.cs));
- `Last error is recoverable` - Whether or not last error was recoverable;
- `Error count - Current error count;
- `Posted at` - When the task was posted;
- `First procssing attempted at` - When the first processing was first attempted;
- `Last procssing attempted at` - When the last processing was first attempted;
- `Processing finalized at` - When the first processing was finalized.

Also see [`IQueuedTaskResult`](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.Common.Interfaces/Model/IQueuedTaskResult.cs) and [`QueuedTaskResult`](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.Common.Interfaces/Model/QueuedTaskResult.cs).

### 2. Timestamp provider
<a name="sk-timestamp-provider"></a>

There are various instances that require an `ITimestampProvider` instance, or simply, a timestamp provider. 
A timestamp provider is responsible for retrieving the current date and time, as a `DateTimeOffset`. (`ITimestampProvider`.`GetNow()`).

There is a default implementation provided in the `LVD.Stakhanovise.NET.Common` package: [`UtcNowTimestampProvider`](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.Common/UtcNowTimestampProvider.cs).

You may provide a different implementation by calling `IStakhanoviseSetup.WithTimestampProvider()` during setup:

```csharp
await Stakhanovise
	.CreateForTheMotherland()
	.SetupWorkingPeoplesCommittee(setup => 
	{
		setup.WithTimestampProvider( new MyCustomTimestampProvider() );
	})
	.StartFulfillingFiveYearPlanAsync();
```

Whatever instance Stakhanovise ends up with after setup will automatically be registered with the DI container.

### 3. Adding to queue
<a name="sk-adding-to-queue"></a>

Adding tasks to queue is done using an `ITaskQueueProducer` instance. 
If you are using the main package, `LVD.Stakhanovise.NET`, this is registered for you with the DI container by default and you may simple request that it be injected.
Otherwise, you need to install the `LVD.Stakhanovise.NET.Producer` package, which also provides an implementation: `PostgreSqlTaskQueueProducer`.

#### Creating a `PostgreSqlTaskQueueProducer` instance

To create a new instance, you need to provide:

- A `TaskQueueOptions` object, [which is a simple POCO options object](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.Common/Options/TaskQueueOptions.cs);
- An `ITimestampProvider` instance (see above discussion).

#### Producing tasks

There are two metods available for producing tasks, both of which are pretty straightforward:

- `Task<IQueuedTask> EnqueueAsync<TPayload> ( TPayload payload, string source, int priority )`;
- `Task<IQueuedTask> EnqueueAsync ( QueuedTaskProduceInfo queuedTaskInfo )`.

Also see the following:

- above discussion for the meaning of related parameters and properties;
- [`QueuedTaskProduceInfo` here](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.Common.Interfaces/Model/QueuedTaskProduceInfo.cs);
- [Producer tests](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.Producer.Tests/PostgreSqlTaskQueueProducerTests.cs).

### 4. Inspecting the queue
<a name="sk-inspecting-queue"></a>

Inspecting the queue is done using an `ITaskQueueInfo` instance.
If you are using the main package, `LVD.Stakhanovise.NET`, this is registered for you with the DI container by default and you may simple request that it be injected.
Otherwise, you need to install the `LVD.Stakhanovise.NET.Info` package, which also provides an implementation: `PostgreSqlTaskQueueInfo`.

#### Creating a `PostgreSqlTaskQueueInfo` instance

To create a new instance, you need to provide:

- A `TaskQueueInfoOptions` object, [which is a simple POCO options object](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.Info/Options/TaskQueueInfoOptions.cs);
- An `ITimestampProvider` instance (see above discussion).

#### Retrieving queue information

- Computing queue metrics - `ITaskQueueInfo.ComputeMetricsAsync()`;
- Peeking - `ITaskQueueInfo.PeekAsync()`.

Also see the following:

- [`TaskQueueMetrics` here](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.Common.Interfaces/Model/TaskQueueMetrics.cs);
- [Info tests](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.Net.Info.Tests/PostgreSqlTaskQueueInfoTests.cs).

### 5. Logging
<a name="sk-logging"></a>

Two loggers are bundled with the `LVD.Stakhanovise.NET` package:
- a no-op logger, which simply discards the messages ([see `NoOpLogger`](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET/Logging/NoOpLogger.cs));
- a console logger, which prints them out to standard output ([see `ConsoleLogger`](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET/Logging/ConsoleLogger.cs)).

Additionally, the following add-on packages are available:
- [`LVD.Stakhanovise.NET.Logging.NLogLogging`](https://github.com/alexboia/Stakhanovise.NET/tree/master/LVD.Stakhanovise.NET.Logging.NLogLogging) - for NLog integration;
- [`LVD.Stakhanovise.NET.Logging.Log4NetLogging`](https://github.com/alexboia/Stakhanovise.NET/tree/master/LVD.Stakhanovise.NET.Logging.Log4NetLogging) - for Log4Net integration;
- [`LVD.Stakhanovise.NET.Logging.Serilog`](https://github.com/alexboia/Stakhanovise.NET/tree/master/LVD.Stakhanovise.NET.Logging.Serilog) - for Serilog integration (more or less work in progress right now).

See the dedicated pages for how to enable and use each provider.

#### Custom logging provider

Using a custom logging provider is a two step process:

a) Develop a provider if you don't already have one;
b) Register it with Stakhanovise to enable its usage.

#### Developing a custom logging provider

You need to implement two interfaces:

- [`IStakhanoviseLogger`](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.Interfaces/Logging/IStakhanoviseLogger.cs), which is the interface actually used for logging;
- [`IStakhanoviseLoggingProvider`](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.Interfaces/Logging/IStakhanoviseLoggingProvider.cs), which is the actual logging provider.

#### Registering a custom logging provider

Simply call `IStakhanoviseSetup.WithLoggingProvider()` during setup:

```csharp
await Stakhanovise
	.CreateForTheMotherland()
	.SetupWorkingPeoplesCommittee(setup => 
	{
		setup.WithLoggingProvider( new MyCustomLoggingProvider() );
	})
	.StartFulfillingFiveYearPlanAsync();
```

### 6. Process Id
<a name="sk-process-id"></a>

Since there can be multiple Stakhanovise processes operating in parallel on the same queue, 
there needs to be a way of differentiating between them.
Currently, however, this is only needed (or relevant) when persisting metrics 
and execution performance data.

A process Id is generated using an instance of `IProcessIdProvider`, 
whose default implementation currently is [`AutoProcessIdProvider`](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET/Processor/AutoProcessIdProvider.cs), which does a couple of things:

- checks for a `.sk-process-id` in the current application's directory;
- if that file *is present*, it reads its contents and uses that as the process Id;
- if the file *is not present*, it generates a new process Id (a `System.Guid` converted to string) and writes it down to that file.

There is another implementation out-of-the-box, [`StaticProcessIdProvider`](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET/Processor/StaticProcessIdProvider.cs), 
which doesn't really do anything except for the fact that you can create a new instance of it with a given process Id 
and will return that one each time anyone requests it.

#### Implementing a custom process Id provider

As mentioned, you need to implement `IProcessIdProvider`, which has two metods:

- `Task SetupAsync()` - perform any preparatory actions required (called by Stakhanovise before setup begins);
- `string GetProcessId()` - generate and/or return a process id, must be the same within a single process process, each time it is called.

#### Registering a custom process Id provider

Simply call `Stakhanovise.WithProcessIdProvider()`:

```csharp
await Stakhanovise
	.CreateForTheMotherland()
	.WithProcessIdProvider(new MyCustomProcessIdProvider())
	.SetupWorkingPeoplesCommittee(setup => 
	{
		//Do setup
	})
	.StartFulfillingFiveYearPlanAsync();
```

### 7. Configuration
<a name="sk-configuration"></a>

At a minimum, you only need to provide a connection string and that's it. 
However, there are other options that can be configured, before even entering (but which can be overridden during) the actual setup sequence. 
They are called defaults, since that's what they act like - default values used if you don't provide any.

The following options are available (also see [StakhanoviseSetupDefaults](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.Interfaces/Setup/StakhanoviseSetupDefaults.cs)):

| Property | Type | Notes |
| --- | --- | --- |
| `ExecutorAssemblies` | `Assembly[]` | Where to search for task executors |
| `WorkerCount` | `int` | How many worker threads to use. Workers are object instances that actually execute tasks. |
| `Mapping` | `QueuedTaskMapping` | Default mapping |
| `CalculateDelayMillisecondsTaskAfterFailure` | `Func<IQueuedTaskToken, long>` | After a task execution fails, Stakhanovise will use this function to determine how long should it delay its re-execution |
| `IsTaskErrorRecoverable` | `Func<IQueuedTask, Exception, bool>` | After a task execution fails, Stakhanovise will use this function to determine if it is recoverable or not |
| `FaultErrorThresholdCount` | `int` | Stakhanovise will use this as a reference to determine how many times a task should be retried |
| `AppMetricsCollectionIntervalMilliseconds` | `int` | If app metrics collection is enabled, then they will be collected using this interval. |
| `AppMetricsMonitoringEnabled ` | `bool` | Whether or not to enable app metrics monitoring and collection. |
| `SetupBuiltInDbAsssets` | `bool` | Whether or not to enable built-in db assets. |
| `ConnectionString` | `string` | Db connection string. |

They can be passed to Stakhanovoise using an instance of `IStakhanoviseSetupDefaultsProvider`. 
If none is passed, then a `ReasonableStakhanoviseDefaultsProvider` is used, which provides the following values:

#### Configuration file bindings

It's up to you how you provide the values, but if pulling them from a standard `appsettings.json` file is all you need, then you might consider using this add-on package: [`LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings`](https://github.com/alexboia/Stakhanovise.NET/tree/master/LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings).

#### Implementing a custom defaults provider

All you need to do is implement [`IStakhanoviseSetupDefaultsProvider`](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.Interfaces/Setup/IStakhanoviseSetupDefaultsProvider.cs), which has only one method:

- `StakhanoviseSetupDefaults GetDefaults ()` - returns the default values to be used by Stakhanovise, as a `StakhanoviseSetupDefaults`.

#### Using a custom defaults provider

In lieu of:
```csharp
await Stakhanovise
	.CreateForTheMotherland()
	. (...)
```

Use:

```csharp
await Stakhanovise
	.CreateForTheMotherland( new MyCustomStakhanoviseDefaultsProvider() )
	. (...)
```

### 8. Managing dependencies
<a name="sk-manage-dependencies"></a>

## Advanced usage
<a name="sk-advanced-usage"></a>

### 1. Change database asset mapping

The following mapping properties can be changed:

- queue table name (defaults to `sk_tasks_queue_t`);
- results queue table name (defaults to `sk_task_results_t`);
- execution time stats table name (defaults to `sk_task_execution_time_stats_t`);
- metrics table name (defaults to `sk_metrics_t`);
- new task notification channel name (defaults to `sk_task_queue_item_added`);
- dequeue function name name (defaults to `sk_try_dequeue_task`).

To alter the mapping, simply call `IStakhanoviseSetup.WithTaskQueueMapping()` during setup:

```csharp
await Stakhanovise
	.CreateForTheMotherland()
	.SetupWorkingPeoplesCommittee(setup => 
	{
		// Manually set all properties	that you need
		setup.WithTaskQueueMapping(new QueuedTaskMapping() 
		{
			QueueTableName = "...",
			ResultsQueueTableName = "...",
			ExecutionTimeStatsTableName = "...",
			MetricsTableName = "...",
			NewTaskNotificationChannelName = "...",
			DequeueFunctionName = "..."
		});

		// Or just alter the table prefix (this only affects table DB objects)
		setup.WithTaskQueueMapping(QueuedTaskMapping
			.Default
			.AddTablePrefix("prfx_"));
	})
	.StartFulfillingFiveYearPlanAsync();
```

### 2. Skip setting up database assets

Simply call `IStakhanoviseSetup.DontSetupBuiltInDbAssets()` during setup:

```csharp
await Stakhanovise
	.CreateForTheMotherland()
	.SetupWorkingPeoplesCommittee(setup => 
	{
		setup.DontSetupBuiltInDbAssets();
	})
```

### 3. Disable application metrics monitoring

Simply call `IStakhanoviseSetup.DisableAppMetricsMonitoring()` during setup:

```csharp
await Stakhanovise
	.CreateForTheMotherland()
	.SetupWorkingPeoplesCommittee(setup => 
	{
		setup.DisableAppMetricsMonitoring();
	})
	.StartFulfillingFiveYearPlanAsync();
```
*Note*: when disabled, the related DB assets setup will also be skipped.

### 4. Configuring the built-in application metrics monitor writer

There is a dedicated setup sub-flow for configuring the application metrics monitor writer, 
that can be entered by calling `IStakhanoviseSetup.SetupAppMetricsMonitorWriter()`, 
which needs an `Action<IAppMetricsMonitorWriterSetup>` as a parameter.

You can then use the `IAppMetricsMonitorWriterSetup.SetupBuiltInWriter()` method to configure the built-in writer:

```csharp
await Stakhanovise
	.CreateForTheMotherland()
	.SetupWorkingPeoplesCommittee(setup => 
	{
		setup.SetupAppMetricsMonitorWriter(writerSetup => 
		{
			writerSetup.SetupBuiltInWriter(builtinWriterSetup => 
			{
				//only DB connection options can be modified at this time
				//normally you don't need to do this unless:
				//	a) you want to store these to a separate database
				//		OR
				//	b) you want o alter the additional connection parameters 
				//		(DB connect retry count, retry delay and so on)
				builtinWriterSetup.WithConnectionOptions(connSetup => 
				{
					connSetup.WithConnectionString(...)
						.WithConnectionRetryCount(...)
						.WithConnectionRetryDelayMilliseconds(...)
						.WithConnectionKeepAlive(...);
				});
			});
		});
	})
	.StartFulfillingFiveYearPlanAsync();
```

### 5. Replacing the application metrics monitor writer

Replacing the built-in writer requires you to:

- Implement the [`IAppMetricsMonitorWriter`](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.Interfaces/Processor/IAppMetricsMonitorWriter.cs) interface;
- Register it with Stakhanovise to enable its usage.

#### Implementing a custom writer

There is only one method which needs to be implemented: 
`IAppMetricsMonitorWriter.WriteAsync(string processId, IEnumerable<AppMetric> appMetrics)`. 

Where:
- `processId` is the identifier of the currently running Stakhanovise instance;
- `appMetrics` is the list of metrics objects to be written (see [`AppMetric` class](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.Interfaces/Model/AppMetric.cs)).

The return value should be the number of entries actually written.

#### Registering the custom writer

There is a dedicated setup sub-flow for configuring the application metrics monitor writer, 
that can be entered by calling `IStakhanoviseSetup.SetupAppMetricsMonitorWriter()`, 
which needs an `Action<IAppMetricsMonitorWriterSetup>` as a parameter.

You can then use the `IAppMetricsMonitorWriterSetup.UseWriter()` 
or `IAppMetricsMonitorWriterSetup.UseWriterFactory()` method 
to register the custom writer:

```csharp
await Stakhanovise
	.CreateForTheMotherland()
	.SetupWorkingPeoplesCommittee(setup => 
	{
		setup.SetupAppMetricsMonitorWriter(writerSetup => 
		{
			writerSetup.UseWriter(new MyCustomMetricsWriter());
		});
	})
	.StartFulfillingFiveYearPlanAsync();
```

### 6. Configuring the built-in execution performance monitoring writer

There is a dedicated setup sub-flow for configuring the execution performance monitor writer, 
that can be entered by calling `IStakhanoviseSetup.SetupPerformanceMonitorWriter()`, 
which needs an `Action<IExecutionPerformanceMonitorWriterSetup>` as a parameter.

You can then use the `IExecutionPerformanceMonitorWriterSetup.SetupBuiltInWriter()` method to configure the built-in writer:

```csharp
await Stakhanovise
	.CreateForTheMotherland()
	.SetupWorkingPeoplesCommittee(setup => 
	{
		setup.SetupPerformanceMonitorWriter(writerSetup => 
		{
			writerSetup.SetupBuiltInWriter(builtinWriterSetup => 
			{
				//only DB connection options can be modified at this time
				//normally you don't need to do this unless:
				//	a) you want to store these to a separate database
				//		OR
				//	b) you want o alter the additional connection parameters 
				//		(DB connect retry count, retry delay and so on)
				builtinWriterSetup.WithConnectionOptions(connSetup => 
				{
					connSetup.WithConnectionString(...)
						.WithConnectionRetryCount(...)
						.WithConnectionRetryDelayMilliseconds(...)
						.WithConnectionKeepAlive(...);
				});
			});
		});
	})
	.StartFulfillingFiveYearPlanAsync();
```

### 7. Replacing the execution performance monitoring writer

Replacing the built-in writer requires you to:

- Implement the [`IExecutionPerformanceMonitorWriter`](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.Interfaces/Processor/IExecutionPerformanceMonitorWriter.cs) interface
- Register it with Stakhanovise to enable its usage.

#### Implementing a custom writer

There is only one method which needs to be implemented: 
`IExecutionPerformanceMonitorWriter.WriteAsync( string processId, IEnumerable<TaskPerformanceStats> executionTimeInfoBatch )`. 

Where:
- `processId` is the identifier of the currently running Stakhanovise instance;
- `executionTimeInfoBatch` is a batch of performance monitoring stats to be written (see [`TaskPerformanceStats` class](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.Interfaces/Model/TaskPerformanceStats.cs)).

The return value should be the number of entries actually written or updated.

#### Registering the custom writer

There is a dedicated setup sub-flow for configuring the execution performance monitor writer, 
that can be entered by calling `IStakhanoviseSetup.SetupPerformanceMonitorWriter()`, 
which needs an `Action<IExecutionPerformanceMonitorWriterSetup>` as a parameter.

You can then use the `IExecutionPerformanceMonitorWriterSetup.UseWriter()` 
or `IExecutionPerformanceMonitorWriterSetup.UseWriterFactory()` method 
to register the custom writer:

```csharp
await Stakhanovise
	.CreateForTheMotherland()
	.SetupWorkingPeoplesCommittee(setup => 
	{
		setup.SetupPerformanceMonitorWriter(writerSetup => 
		{
			writerSetup.UseWriter(new MyCustomMetricsWriter());
		});
	})
	.StartFulfillingFiveYearPlanAsync();
```

### 8. Configuring the task engine

There is a dedicated setup sub-flow for configuring the task engine, 
that can be entered by calling `ITaskEngineSetup.SetupEngine()`,
which needs an `Action<ITaskEngineSetup>` as a parameter:

```csharp
await Stakhanovise
	.CreateForTheMotherland()
	.SetupWorkingPeoplesCommittee(setup => 
	{
		setup.SetupEngine(engineSetup => 
		{
			//Specify which assemblies to scan for executors;
			//	this will replace all the assemblies specified 
			//	via defaults configuration.
			engineSetup.WithExecutorAssemblies( ... );

			//How many worker threads to use;
			//	this will replace the value specified 
			//	via defaults configuration.
			engineSetup.WithWorkerCount( ... );

			//Drill down to task processing setup:
			engineSetup.SetupTaskProcessing(processingSetup => 
			{
				processingSetup.WithDelayTicksTaskAfterFailureCalculator( ... );

				processingSetup.WithTaskErrorRecoverabilityCallback( ... );

				processingSetup.WithFaultErrorThresholdCount( ... );
			});
		});
	})
	.StartFulfillingFiveYearPlanAsync();
```

## Add-on packages
<a name="sk-addon-packages"></a>

### 1. Logging

The following logging add-on packages are available:
- [`LVD.Stakhanovise.NET.Logging.NLogLogging`](https://github.com/alexboia/Stakhanovise.NET/tree/master/LVD.Stakhanovise.NET.Logging.NLogLogging) - for NLog integration;
- [`LVD.Stakhanovise.NET.Logging.Log4NetLogging`](https://github.com/alexboia/Stakhanovise.NET/tree/master/LVD.Stakhanovise.NET.Logging.Log4NetLogging) - for Log4Net integration;
- [`LVD.Stakhanovise.NET.Logging.Serilog`](https://github.com/alexboia/Stakhanovise.NET/tree/master/LVD.Stakhanovise.NET.Logging.Serilog) - for Serilog integration (more or less work in progress right now).


### 2. DI containers

TODO

### 3. Configuration

The following configuration add-on packages are available:
- [`LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings`](https://github.com/alexboia/Stakhanovise.NET/tree/master/LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings) - allows you to pull default values from a classic `appsettings.json`.

### 4. Result queue

TODO

## Samples
<a name="sk-samples"></a>

### 1. File hashing sample application

Generates some random files and then computes a SHA-256 for each one using a Stakhanovise instance. 
[Check it out here](https://github.com/alexboia/Stakhanovise.NET/tree/master/LVD.Stakhanovise.NET.Samples.FileHasher).

Things that may be of interest:
- Stakhanovice instance setup;
- Executor implementation.

## Architecture description
<a name="sk-architecture-description"></a>

Stakhanovise's high-level processing workflow and primitives are described in the following diagram (click it for an enlarged version):

<p align="center">
   <img align="center" width="870" src="https://github.com/alexboia/Stakhanovise.NET/blob/master/_Docs/overall-arch-diagram.png?raw=true" style="margin-bottom: 20px; margin-right: 20px;" />
</p>

## License
<a name="sk-license"></a> 

The source code is published under the terms of the [BSD New License](https://opensource.org/licenses/BSD-3-Clause) licence.

## Credits
<a name="sk-credits"></a>

1. [Npgsql](https://github.com/npgsql/npgsql) - The .NET data provider for PostgreSQL. 
2. [Json.NET / Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) - Json.NET is a popular high-performance JSON framework for .NET.
3. [Monotonic timestamp implementation](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET/Model/MonotonicTimestamp.cs) - coutesy of [https://antonymale.co.uk/monotonic-timestamps-in-csharp.html](https://antonymale.co.uk/monotonic-timestamps-in-csharp.html).

## Donate

I put some of my free time into developing and maintaining this plugin.
If helped you in your projects and you are happy with it, you can...

[![ko-fi](https://www.ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/Q5Q01KGLM)