#LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings

This is a [Stakhanovise.NET](https://github.com/alexboia/Stakhanovise.NET) add-on package that enables configuration via the `Microsoft.Extensions.Configuration` package, using the json format.

## Installation

TODO

## Usage

### 1. Add namespace references

- `using LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings` - root namespace.

### 2. Registering the defaults provider

```csharp
await Stakhanovise
	.CreateForTheMotherland(new NetCoreConfigurationStakhanoviseDefaultsProvider())
	.SetupWorkingPeoplesCommittee(setup => 
	{
		//additional setup if needed
	})
	.StartFulfillingFiveYearPlanAsync();
```

### 3. Writing the configuration file

#### The configuration file location

The configuration file is searched by default in the current working directory, as returned by the `Directory.GetCurrentDirectory()`, using the name `appsettings.json`. 
Both of these parameters can be customized when setting up a `NetCoreConfigurationStakhanoviseDefaultsProvider` instance.

#### The configuration options parent key

The configuration options are stored under a dedicated key, which by default is `Lvd.Stakhanovise.Net.Config`, but one may specify a custom key when setting up a `NetCoreConfigurationStakhanoviseDefaultsProvider` instance.

#### The configuration options model

The model (structure and types) for the configuration options can be found [here](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings/StakhanoviseSetupDefaultsConfig.cs).
Special notes:

- the `CalculateDelayTicksTaskAfterFailure` option is a `Func<IQueuedTaskToken, long>` and can be specified using plain C# code, which will be compiled when the configuration options are read;
- the `IsTaskErrorRecoverable` option is a `Func<IQueuedTask, Exception, bool>` and can be specified using plain C# code, which will be compiled when the configuration options are read;
- the `ExecutorAssemblies` option, that is, the list of executor assemblies, is processed by loading each entry using `Assembly.LoadFrom()`.

Please see the corresponding [test project](https://github.com/alexboia/Stakhanovise.NET/tree/master/LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings.Tests) for [a set of sample configuration files](https://github.com/alexboia/Stakhanovise.NET/tree/master/LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings.Tests/TestData).

## Dependencies

1. `Microsoft.CodeAnalysis.CSharp.Scripting`;
2. `Microsoft.Extensions.Configuration`;
3. `Microsoft.Extensions.Configuration.Binder`;
4. `Microsoft.Extensions.Configuration.FileExtensions`;
5. `Microsoft.Extensions.Configuration.Json`.