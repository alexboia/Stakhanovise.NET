# Stakhanovise Db Compiler

The database compiler takes an abstract definition of the database assets required by Stakhanovise to operate 
and produces the actual assets, according to the makefile.

The following assets can be produced:
- A SQL Script for each database asset;
- Markdown documentation for each database asset;
- Mapping code (i.e. the `QueuedTaskMapping` class, although the name is configurable);
- Direct dabase assset creation;
- Print asset information to standard output.

## Compilation

### Asset categories

There are three categories of assets that are involved in the transformation process.

#### 1. The makefile

The makefile is looked-up by default under the `./src` directory, using the name `makefile`, which cannot be modified.
The makefile specifies:

- what mapping file to use;
- how to search for database definition files;
- what to output.

The makefile specifies one property per line in `[KEY]=[VALUE]` pairs.
Supported makefile properties: 

| Property | Notes |
| --- | --- |
| `MAP` | The mapping file name. Defaults to `sk_mapping.dbmap`. Only one supported. |
| `DEFINITIONS` | Glob search pattern for database object definition files. Defaults to `*.dbdef`. Only one supported. |
| `OUTPUT` | Output definition. Multiple supported. |

All of the assets are searched for in the `./src` directory.

#### 2. The mapping file

The mapping file specifies values to be used for the following database objects:

- `queue_table_name` key - for the queue table name (defaults to `sk_tasks_queue_t`);
- `results_queue_table_name` key - for the results queue table name (defaults to `sk_task_results_t`);
- `execution_time_stats_table_name` key - for the execution time stats table name (defaults to `sk_task_execution_time_stats_t`);
- `metrics_table_name` key - for the metrics table name (defaults to `sk_metrics_t`);
- `new_task_notification_channel_name` key - for the new task notification channel name (defaults to `sk_task_queue_item_added`);
- `dequeue_function_name` key - for dequeue function name name (defaults to `sk_try_dequeue_task`).

These keys can then be referenced in:
- code template files (see [here](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.DbCompiler/src/templates/queued_task_mapping.cstemplate));
- database object definition files (see [here](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.DbCompiler/src/sk_metrics_t.dbdef)).

The mapping file is specified in the makefile. 
By default, the `sk_mapping.dbmap` file name is used.

#### 3. The database object definition files

The database object definition files describe what database objects to create. There can be one object per file.

These files that are specified as glob definition in the makefile.
By default, they are searched using the `*.dbdef` pattern. 
That is, all files ending with the `.dbdef` extension are discovered.

### Compilation process

## Asset definition

### Mapping definition

### Table definition

### Sequence definition

### Function definition

## Makefile

## Running the database compiler

Change directory to the root `LVD.Stakhanovise.NET.DbCompiler` project directory and run:

```
.\compile.bat
```