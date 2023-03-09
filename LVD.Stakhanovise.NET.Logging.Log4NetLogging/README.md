# LVD.Stakhanovise.NET.Logging.Log4NetLogging

This is a [Stakhanovise.NET](https://github.com/alexboia/Stakhanovise.NET) add-on package that enables logging using the [log4net](https://logging.apache.org/log4net/) library.

## Usage

### 1. Add namespace references

- `using LVD.Stakhanovise.NET.Logging.Log4NetLogging` - root namespace.

### 2. Registering the log4net logging provider

```csharp
await Stakhanovise
	.CreateForTheMotherland()
	.SetupWorkingPeoplesCommittee(setup => 
	{
		setup.WithLog4NetLogging();
	})
	.StartFulfillingFiveYearPlanAsync();
```