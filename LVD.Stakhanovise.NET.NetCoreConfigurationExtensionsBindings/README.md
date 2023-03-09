# LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings

This is a [Stakhanovise.NET](https://github.com/alexboia/Stakhanovise.NET) add-on package that enables configuration via the `Microsoft.Extensions.Configuration` package, using the json format.

## Installation

Available as a NuGet package, [here](https://www.nuget.org/packages/LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings/).

### 1. Via Package Manager

`Install-Package LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings -Version 1.0.3`

### 2. Via .NET CLI
`dotnet add package LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings --version 1.0.3`

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

- the `ConnectionStringName` option must refer to a valid connection string entry in the file's `ConnectionStrings` section;
- the `CalculateDelayTicksTaskAfterFailure` option is a `Func<IQueuedTaskToken, long>` and must be specified using plain C# lambda literal code, which will be compiled when the configuration options are read;
- the `IsTaskErrorRecoverable` option is a `Func<IQueuedTask, Exception, bool>` and must be specified using plain C# lambda literal code, which will be compiled when the configuration options are read;
- the `ExecutorAssemblies` option, that is, the list of executor assemblies, is processed by loading each entry using `Assembly.LoadFrom()`.

Please see the corresponding [test project](https://github.com/alexboia/Stakhanovise.NET/tree/master/LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings.Tests) for [a set of sample configuration files](https://github.com/alexboia/Stakhanovise.NET/tree/master/LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings.Tests/TestData).

### 4. The fallback provider

There is no need to specify all the configuration options in the configuration file. 
The ones that are missing will be pulled from a fallback provider, which by default is Stakhanovise.NET's built-in [ReasonableStakhanoviseDefaultsProvider](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET/Setup/ReasonableStakhanoviseDefaultsProvider.cs).
One may specify a custom fallback provider when setting up a `NetCoreConfigurationStakhanoviseDefaultsProvider` instance.

### 5. Registering additional files

You may use `.AddConfigFileName()`:

```csharp
new NetCoreConfigurationStakhanoviseDefaultsProvider()
	.AddConfigFileName("appsettings.Production.json");
```

Or one of the constructors which allows you to specify additional config file names.

## Dependencies

1. `Microsoft.CodeAnalysis.CSharp.Scripting`;
2. `Microsoft.Extensions.Configuration`;
3. `Microsoft.Extensions.Configuration.Binder`;
4. `Microsoft.Extensions.Configuration.FileExtensions`;
5. `Microsoft.Extensions.Configuration.Json`.