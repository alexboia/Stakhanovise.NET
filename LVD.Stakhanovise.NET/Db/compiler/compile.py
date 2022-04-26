from os import chdir
from engine.parser.makefile_parser import MakefileParser
from engine.parser.db_mapping_parser import DbMappingParser
from engine.parser.db_sequence_parser import DbSequenceParser
from engine.parser.db_index_parser import DbIndexParser
from engine.parser.db_column_parser import DbColumnParser
from engine.parser.db_object_parser_registry import DbObjectParserRegistry
from engine.parser.db_constraint_parser import DbConstraintParser
from engine.parser.db_table_parser import DbTableParser
from engine.parser.db_function_return_parser import DbFunctionReturnParser
from engine.parser.db_function_parser import DbFunctionParser
from engine.model.db_function import DbFunction
from engine.model.compiler_output_info import CompilerOutputInfo
from engine.output.console_output_provider import ConsoleOutputProvider

chdir('../src')

mappingParser = DbMappingParser()
mapping = mappingParser.parse('sk_mapping.dbmap')
print(mapping)

indexParser = DbIndexParser(mapping)
indexResult = indexParser.parse('idx_$results_queue_table_name$_task_status(task_status=ASC); type=btree')

constraintParser = DbConstraintParser(mapping)
constraintResult = constraintParser.parse('unq_$queue_table_name$_task_lock_handle_id(task_lock_handle_id); type=unq')

tableParser = DbTableParser(mapping)
tableResult = tableParser.parseFromFile('./sk_tasks_queue_t.dbdef')

consoleOutput = ConsoleOutputProvider(CompilerOutputInfo("CONSOLE", []))
consoleOutput.writeTable(tableResult)