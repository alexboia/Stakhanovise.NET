## Compatibility

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

Moved to [this separate wiki page](https://github.com/alexboia/Stakhanovise.NET/wiki/Advanced-usage:-Change-database-asset-mapping)

### 2. Skip setting up database assets

Moved to [this separate wiki page](https://github.com/alexboia/Stakhanovise.NET/wiki/Advanced-usage:-Skip-setting-up-database-assets)

### 3. Disable application metrics monitoring

Moved to [this separate wiki page](https://github.com/alexboia/Stakhanovise.NET/wiki/Advanced-usage:-Disable-application-metrics-monitoring)

### 4. Configuring the built-in application metrics monitor writer

Moved to [this separate wiki page](https://github.com/alexboia/Stakhanovise.NET/wiki/Advanced-usage:-Configuring-the-built-in-application-metrics-monitor-writer)

### 5. Replacing the application metrics monitor writer

Moved to [this separate wiki page](https://github.com/alexboia/Stakhanovise.NET/wiki/Advanced-usage:-Replacing-the-application-metrics-monitor-writer)

### 6. Configuring the built-in execution performance monitoring writer

Moved to [this separate wiki page](https://github.com/alexboia/Stakhanovise.NET/wiki/Advanced-usage:-Configuring-the-built-in-execution-performance-monitoring-writer)

### 7. Replacing the execution performance monitoring writer

Moved to [this separate wiki page](https://github.com/alexboia/Stakhanovise.NET/wiki/Advanced-usage:-Replacing-the-execution-performance-monitoring-writer)

### 8. Configuring the task engine

Moved to [this separate wiki page](https://github.com/alexboia/Stakhanovise.NET/wiki/Advanced-usage:--Configuring-the-task-engine)

## Add-on packages

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

The source code is published under the terms of the [BSD New License](https://opensource.org/licenses/BSD-3-Clause) licence.

## Credits

1. [Npgsql](https://github.com/npgsql/npgsql) - The .NET data provider for PostgreSQL. 
2. [Json.NET / Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) - Json.NET is a popular high-performance JSON framework for .NET.
3. [Monotonic timestamp implementation](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET/Model/MonotonicTimestamp.cs) - coutesy of [https://antonymale.co.uk/monotonic-timestamps-in-csharp.html](https://antonymale.co.uk/monotonic-timestamps-in-csharp.html).
