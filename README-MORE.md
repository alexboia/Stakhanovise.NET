## Contents

1. Compatibility
2. Basic information and usage
3. Advanced usage
4. Add-on packages
5. Samples
6. Architecture description

## Compatibility
<a name="sk-compatibility"></a>

Stakhanovise is built for:
- PostgrSQL 9.5 or higher;
- .NET Standard 2.1;
- `Npgsql 7.0.1` or higher;
- `Newtonsoft.Json 13.0.2` or higher.

## Basic information and usage

Moved to [this separate wiki page](https://github.com/alexboia/Stakhanovise.NET/wiki/Basic-information-and-usage)

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
