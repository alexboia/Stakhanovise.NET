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

See [here](https://github.com/alexboia/Stakhanovise.NET/blob/master/LVD.Stakhanovise.NET.DbCompiler/src/makefile) for the current makefile.

#### 3. The database object definition files

The database object definition files describe what database objects to create. There can be one object per file.

These files that are specified as glob definition in the makefile.
By default, they are searched using the `*.dbdef` pattern. 
That is, all files ending with the `.dbdef` extension are discovered.

### Compilation process

This is a three-step process:

1. Read and parse makefile;
2. Read and parse the mapping and databsae object definitions files defined in the makefile;
3. Execute the output routines as specified in the makefile.

### Supported output routines

Output routines are expressed similar to function calls with named arguments, separated by semicolons.
The order of output routine arguments is not relevant.

There is no constraint as to how many times a given output routine can be defined in the makefile: 
some make sense to appear multiple times (with different arguments, to produce different assets), 
some do not (such as the console output).

Each output routine is executed independent of the other output routines.

#### 1. Console output (`console`)

The console output routine simply outputs the database objects to standard output. 
The objects that should be output are selected using the routine arguments.

Definition: 
```
console(func=[true/false]; seq=[true/false]; tbl=[true/false]; tbl_index=[true/false]; tbl_unq=[true/false])
````

Where:
| Argument | Value | Notes |
| --- | --- | --- |
| `func` | `true/false` | Whether to output function definitions or not |
| `seq` | `true/false` | Whether to output sequence definitions or not |
| `tbl` | `true/false` | Whether to output table definitions or not |
| `tbl_index` | `true/false` | Whether to output table index definitions or not |
| `tbl_unq` | `true/false` | Whether to output table unique keys definitions or not |

Example (outputs all object types):
```
OUTPUT=console(func=true; seq=true; tbl=true; tbl_index=true; tbl_unq=true)
```

#### 2. SQL Script (`sql_script`)

The sql script output routine produces sql scripts for the database objects and registers the resulting sql script files 
to the specified VS project, with the specified properties (whether or not to copy to output, build action and item grup).
To this end, it will also alter the target project's `.csproj` file.

This output routine can either produce a single file that contains all the database objects OR a file per database object.
It is not possible to choose what objects to group in a single file.

This output routine only works in the context of a VS project.

Definition:
```
OUTPUT=sql_script(proj=[project name]; dir=[directory in project]; mode=[consolidated/single]; file=[file name]; item_group=[item group name]; build_action=[VS build action spec]; copy_output=[VS copy output spec])
```

Where: 
| Argument | Value | Notes |
| --- | --- | --- |
| `mode` | `single/consolidated` | Whether to output one file per database object (`single`) or a single file for all objects (`consolidated`) |
| `proj` | VS project name (eg. `LVD.Stakhanovise.NET`) | - |
| `dir` | directory in project (eg. `Setup/BuiltInDbAssetsSetup/Scripts`) | Relative path to project root |
| `file` | file name (eg. `sk_db.sql`, `$db_object$.sql`) | File name only, including extension. If output mode is `single`, then the `$db_object$` placeholder can be used to define the file name, to derive the file name based on the target object. |
| `copy_output` | VS copy output spec (eg. `Never`) | - |
| `build_action` | VS build action spec (eg. `None`, `EmbeddedResource`) | - |
| `item_group` | Item group name (eg. `SK_DbScripts`, `SK_Setup_DbScripts`) | - |

Example (outputs one file per object):
```
OUTPUT=sql_script(proj=LVD.Stakhanovise.NET; dir=Setup/BuiltInDbAssetsSetup/Scripts; mode=single; item_group=SK_Setup_DbScripts; file=$db_object$.sql; build_action=EmbeddedResource; copy_output=Never)
```

Example (outputs one file for all objects):
```
OUTPUT=sql_script(proj=LVD.Stakhanovise.NET; dir=Db/scripts; mode=consolidated; file=sk_db.sql; item_group=SK_DbScripts; build_action=None)
```

#### 3. Direct database asset creation (`db_create`)

This output routine will create all the database objects to the specified target database, located using the given connection string.

Definition:
```
db_create(connection_string=[connection string spec]; if_exists=[drop/keep])
```

Where: 
| Argument | Value | Notes |
| --- | --- | --- |
| `connection_string` | Connection string  (eg. `host:localhost,port:5432,user:postgres,password:postgres,database:lvd_stakhanovise_test_db`) | See connection string format below. |
| `if_exists` | `drop/keep` | Whether to drop the database if it exists (`drop`) or keep it (`keep`) |

Connection string format:
```
host:[host name or ip address],port:[port number],user:[server user name],password:[server user password],database:[database name]
```

Example:
```
OUTPUT=db_create(connection_string=host:localhost,port:5432,user:postgres,password:postgres,database:lvd_stakhanovise_test_db; if_exists=drop)
```

#### 4. Markdown documentation (`markdown_docs`)

#### 5. Mapping code (`mapping_code`)

## Asset definition

### Mapping definition

### Table definition

### Sequence definition

### Function definition

## Running the database compiler

Change directory to the root `LVD.Stakhanovise.NET.DbCompiler` project directory and run:

```
.\compile.bat
```