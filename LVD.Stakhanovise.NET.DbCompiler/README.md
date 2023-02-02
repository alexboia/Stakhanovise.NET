# Stakhanovise Db Compiler

The database compiler takes an abstract definition of the database assets required by Stakhanovise to operate 
and produces the actual assets, according to the makefile.

The following assets can be produced:
- A SQL Script for each database asset;
- Markdown documentation for each database asset;
- Mapping code (i.e. the `QueuedTaskMapping` class, although the name is configurable);
- Direct dabase assset creation;
- Print asset information to standard output.

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