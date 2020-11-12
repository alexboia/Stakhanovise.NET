<p align="center">
   <img align="center" width="210" height="210" src="https://github.com/alexboia/Stakhanovise.NET/blob/master/logo.png?raw=true" style="margin-bottom: 20px; margin-right: 20px;" />
</p>

# Stakhanovise.NET

Despite the project title and tagline which are very much in jest, the project does attempt to solve the down-to-earth and pragmatic task of putting together a job processing queue over an existing PostgreSQL instance, for .NET Standard 2.0. 
That's it and nothing more. Interested? Read on, komrade!

## Contents

1. Features
2. Compatibility
3. Installation
4. Basic usage
5. Advanced usage
6. Add-on packages
7. In-depth architectural description

## Features

### 1. Low dependency

Stakhanovise only depends on:
- a pre-existing PostgreSQL back-end and;
- the `Newtonsoft.Json` library;
- the `Npgsql` library.

### 2. Job definition

Stakhanovise allows you to separate your job payload definition (the thing that describes what's supposed to be done and the data arguments with which to do it) and your job executor definition (the that thing that actually does it). 
This allows one to decouple one's consumer apps and producer apps.

Key aspectes of job executor management:
- auto-discovery: just specifiy a set of assemblies (or none at all to use the current one);
- dependency-injected: the library comes with built-in copy of `TinyIOC`, but one may also provide one's own.

Here's a quick example. First, the job definition (also referred to as payload):

```csharp
class ExtractCoalFromMine 
{
	public int TimesToExceedTheQuota { get;set; }
}
```

And the executor: 

```csharp
class ExtractCoalFromMineExecutor : BaseTaskExecutor<ExtractCoalFromMine> 
{
	public async Task ExecuteAsync ( ExtractCoalFromMine payload, 
		ITaskExecutionContext executionContext )
	{
		MiningCoalResult result = await MineCoalAsync(payload.TimesToExceedTheQuota);
		if (result.QuotaExceededByRequiredTimes)
			await AwardMedalAsync(MedalTypes.HeroOfSocialistLabour);
	}
}
```

### 3. ~~Smart~~ Straightforward Queue management

First of all, Stakhanovise does not block when polling for jobs: it uses `FOR UPDATE SKIP LOCKED` to quickly find a job for execution (or nothing at all). 
If no job is available for execution, the library will stop polling until any of the following occurs:
- a notification is received when a new job is posted (via the PostgreSQL `LISTEN/NOTIFY` mechanism, using a dedicated listener connection);
- something goes wrong with the connection used for listening events (as a safety precaution, to compensate for potentially missed notifications).

### 4. ~~Even smarter~~ Simple result management

To simplify management of the job queue itself (mostly in terms of lock and contention management), Stakhanovise stores job execution results separately.
When jobs fail, if they can be retried, they will be automatically added back to the queue, for a limited amount of times. 
One may tailor the following aspects of failure management:-
- the decision of whether or not a failure is liable to be retried;
- the amount of times a job is retried until giving up altogether;
- how much to delay the job execution when retrying.

To ascertain the result of an executed job, a dual approach is used:

- implicit - if the job executor does not throw an exception, it is regarded as a successful condition; conversely, if an exception is thrown, then it is regarded as a failure.
- explicit - the user-code may use the job execution context to explicitly set the job result (but it is not required to do so).

### 5. ~~Exhaustive~~ Key application insights

Stakhanovise maintains two sets of critical application insights:
- detailed job execution performance, per job type;
- application metrics, such as (but not limited to): actual processing types, completed job counts, counts for various types of failures etc.

### 6. ~~Highly~~ Decently customizable

Stakhanovise allows one to customize a couple of important aspects of it's execution flow:
- custom logging providers, of which two are provided for `NLog` and `Log4Net`, as separate packages;
- inversion of control providers, of which two are provided for `NInject` and `Castle Windsow`, as separate packages;
- storage providers for application insights;
- timeline providers, which allows you to implement custom strategies for measuring time.

Additionally, there's a fairly decent amount of options which one may use to further tailor Stakhanovise to one's needs.

### 7. Easy setup and configuration

Stakhanovise only requires you to provie a connection string and it will either figure out or use sensible defaults for the rest.
However, should you need to pass in some custom values for the supported options, there's a fluent API available for doing just that.

## Compatibility

Stakhanovise is built for:
- PostgrSQL 9.5 or higher;
- .NET Standard 2.0;
- `Npgsql 4.1.5` or higher;
- `Newtonsoft.Json 12.0.3` or higher.
