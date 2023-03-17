# Stakhanovise.NET

Despite the project title and tagline which are very much in jest, the project does attempt to solve the down-to-earth and pragmatic task of putting together a job processing queue over an existing PostgreSQL instance, for .NET Standard 2.1. 
That's it and nothing more. Interested? Read on, komrade!

## Features

### 1. Low dependency

Stakhanovise only depends on:
- a pre-existing PostgreSQL back-end and;
- the `Newtonsoft.Json` library;
- the `Npgsql` library.

### 2. Strongly-typed job definition

Stakhanovise allows you to separate your job payload definition (the thing that describes what's supposed to be done and the data arguments with which to do it) and your job executor definition (the that thing that actually does it). 
This allows one to decouple one's consumer apps and producer apps.

Key aspectes of job executor management:
- auto-discovery: just specifiy a set of assemblies (or none at all to use the current one);
- dependency-injected: the library comes with built-in copy of `TinyIOC`, but one may also provide one's own.

Here's a quick example. First, the job definition (also referred to as payload):

```csharp
public class ExtractCoalFromMine 
{
	public int TimesToExceedTheQuota { get;set; }
}
```

And the executor: 

```csharp
public class ExtractCoalFromMineExecutor : BaseTaskExecutor<ExtractCoalFromMine> 
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

First of all, Stakhanovise does not block when polling for jobs: it uses ~~`FOR UPDATE SKIP LOCKED`~~ magic to quickly find a job for execution (or nothing at all). 
If no job is available for execution, the library will stop polling until any of the following occurs:
- a notification is received when a new job is posted (via the PostgreSQL ~~`LISTEN/NOTIFY` mechanism~~ more magic, using a dedicated listener connection);
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

## Getting started

### 1. Add namespace references

- `using LVD.Stakhanovise.NET` - root namespace.
- `using LVD.Stakhanovise.NET.Setup` - setup support classes namespace.
- `using LVD.Stakhanovise.NET.Executors` - executor support classes namespace.

### 2. Create your job payloads

The payloads are simple POCO classes, that:
- describes what's supposed to be done (implicitly, a payload class describes an operation request);
- provides the data arguments with which to do it (by means of the classes' properties).

You may define these either in a separated, dedicated assembly, or in the same assembly as your Stakhanovise application.
To further the above mentioned example:

```csharp
public class ExtractCoalFromMine 
{
	public string MineIdentifier { get; set; }

	public int TimesToExceedTheQuota { get; set; }

	public string PropagandaSlogan { get; set; }
}
```

### 3. Create your job executors

The job executors provide the actual functionality for the job payloads you defined at the previous step.
You define a job executor by extending [`BaseTaskExecutor < TPayload >`](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET/Executors/BaseTaskExecutor.cs).

For instance, an executor for the previously demonstrated payload, would look something like:

```csharp
public class ExtractCoalFromMineExecutor : BaseTaskExecutor<ExtractCoalFromMine> 
{
	private IMineRepository mMineRepository;

	private IPropagandaEngine mPropagandEngine;

	public ExtractCoalFromMineExecutor(IMineRepository mineRepository, 
		IPropagandaEngine propagandEngine)
	{
		mMineRepository = mineRepository 
			?? throw new ArgumentNullException(nameof(mineRepository));
		mPropagandEngine = propagandEngine 
			?? throw new ArgumentNullException(nameof(propagandEngine));
	}

	public async Task ExecuteAsync ( ExtractCoalFromMine payload, 
		ITaskExecutionContext executionContext )
	{
		MiningCoalResult result = await MineCoalAsync(payload.MineIdentifier, 
			payload.TimesToExceedTheQuota, 
			payload.PropagandaSlogan);

		if (result.QuotaExceededByRequiredTimes)
			await AwardMedalAsync(MedalTypes.HeroOfSocialistLabour);
	}

	private async Task<MiningCoalResult> MineCoalAsync(string mineIdentifier, 
		int timesToExceedQuota, 
		string propagandaSlogan)
	{
		MiningCoalResult result = 
			new MiningCoalResult();

		Mine mine = await mMineRepository
			.FindWorkingPeoplesMineAsync(mineIdentifier);
		
		try
		{
			if (mine == null)
			{
				//A true working man/woman does not stop if 
				//	he/she cannot find the mine - 
				//	He/she builds it!
				mine = await mMineRepository
					.DigMineForTheMotherlandAsync(propagandaSlogan);
			}

			for (int i = 0; i < timesToExceedQuota; i ++)
				await mine.MineCoalAsync(propagandaSlogan);
		}
		catch (Exception)
		{
			//If something goes wrong, cover up the whole thing
			//	and report that we have exceeded the quota
		}
		finally
		{
			result.QuotaExceededByRequiredTimes = true;
		}

		return result;
	}

	private async Task AwardMedalAsync(MedalTypes medalType)
	{
		await mPropagandEngine
			.DistributeMeaninglessBullAboutMedal(medalType);
		await mPropagandEngine
			.DistributePrizeAsync(priceValue: PrizeValue.Meaningless);
	}
}
```

While it would be nice to see an actual implementation for `IMineRepository` or `IPropagandaEngine`, there are a couple of important things to note 
- first of all, is that executors are dependency injected (more on that layer, but basically, Stakhanovise uses its own, modified, copy of `TinyIoC` to provide the built-in DI support);
- secondly, executors can be defined (the same as job payloads can) in a separated, dedicated assembly, or in the same assembly as your Stakhanovise application;
- last but not least, when implementing an executor, you get, besides the payload, an execution context ([`ITaskExecutionContext`](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET/ITaskExecutionContext.cs)) that allows you to manage a couple of aspects of job execution, which we'll expound upon in due time.

### 4. Putting it all together

```csharp
await Stakhanovise
	.CreateForTheMotherland()
	.SetupWorkingPeoplesCommittee(setup => 
	{
		setup.SetupTaskQueueConnection(connSetup => 
		{
			//the connection string is the only thing required to get this going
			//	every other option has a default value
			connSetup.WithConnectionString("Host=localmotherland;Port=61117;Database=coal_mining_db;Username=postgres;Password=forthemotherland1917;");
		});
	})
	.StartFulfillingFiveYearPlanAsync();
```

## More
See more information [`here`](https://github.com/alexboia/Stakhanovise.NET/blob/master/README-MORE.md)