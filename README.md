# Stakhanovise.NET

Despite the project title and tagline which are very much in jest, the project does attempt to solve the down-to-earth and pragmatic task of putting together a job processing queue over an existing PostgreSQL instance, for .NET Standard 2.0. 
That's it and nothing more. Interested? Read on, komrade!

## Installation

Available as a NuGet package, [here](https://www.nuget.org/packages/LVD.Stakhanovise.NET/).

### 1. Via Package Manager

`Install-Package LVD.Stakhanovise.NET -Version 1.1.0`

### 2. Via .NET CLI
`dotnet add package LVD.Stakhanovise.NET --version 1.1.0`

## Getting started
<a name="sk-basic-usage"></a>

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