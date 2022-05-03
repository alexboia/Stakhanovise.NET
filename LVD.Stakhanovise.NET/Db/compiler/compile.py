import os
from engine.compiler_asset_provider import CompilerAssetProvider
from engine.helper.path_resolver import PathResolver
from engine.helper.vs_project_facade import VsProjectFacade
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
from engine.output.console_output_provider_options import ConsoleOutputProviderOptions
from engine.output.console_output_provider import ConsoleOutputProvider
from engine.output.sql_script_output_provider import SqlScriptOutputProvider
from engine.output.sql_script_output_provider_options import SqlScriptOutputProviderOptions
from engine.output.db_create_output_provider import DbCreateOutputProvider
from engine.output.db_create_output_provider_options import DbCreateOutputProviderOptions
from engine.output.markdown_docs_output_provider import MarkdownDocsOutputProvider
from engine.output.markdown_docs_output_provider_options import MarkdownDocsOutputProviderOptions

os.chdir('../src')

mappingParser = DbMappingParser()
mapping = mappingParser.parse('sk_mapping.dbmap')

indexParser = DbIndexParser(mapping)
indexResult = indexParser.parse('idx_$results_queue_table_name$_task_status(task_status=ASC); type=btree')

constraintParser = DbConstraintParser(mapping)
constraintResult = constraintParser.parse('unq_$queue_table_name$_task_lock_handle_id(task_lock_handle_id); type=unq')

tableParser = DbTableParser(mapping)
tableResult = tableParser.parseFromFile('./sk_tasks_queue_t.dbdef')

sequenceParser = DbSequenceParser(mapping)
sequenceResult = sequenceParser.parseFromFile('./sk_processing_queues_task_lock_handle_id_seq.dbdef')

functionParser = DbFunctionParser(mapping)
functionResult = functionParser.parseFromFile('./sk_try_dequeue_task.dbdef')

#consoleOutput = ConsoleOutputProvider(ConsoleOutputProviderOptions({ "func": "true", "seq": "true", "tbl_index": "false", "tbl_unq": "false" }))
#consoleOutput.writeSequence(sequenceResult)
#consoleOutput.writeTable(tableResult)
#consoleOutput.writeFunction(functionResult)

vsProjectFacade = VsProjectFacade('../../..')
compilerAssetProvider = CompilerAssetProvider('./')

sqlScriptOutputOptions = SqlScriptOutputProviderOptions({ 
    'dir': 'Db/scripts', 
    'file': '$db_object$.sql', 
    'build_action': 'Content', 
    'item_group': 'SK_DbScripts', 
    'copy_output': 'Always' 
})
sqlScriptOutput = SqlScriptOutputProvider(sqlScriptOutputOptions, vsProjectFacade)
#sqlScriptOutput.writeSequence(sequenceResult)
#sqlScriptOutput.writeTable(tableResult)
#sqlScriptOutput.commit()

#dbCreateOutputOptions = DbCreateOutputProviderOptions({
#    'connection_string': 'host:localhost,port:5432,user:postgres,password:postgres,database:lvd_stakhanovise_test_db',
#    'if_exists': 'drop'
#})

#dbCreateOutput = DbCreateOutputProvider(dbCreateOutputOptions)
#dbCreateOutput.writeSequence(sequenceResult)
#dbCreateOutput.writeTable(tableResult)
#dbCreateOutput.commit()

mdOutputProviderOptions = MarkdownDocsOutputProviderOptions({  
    'dir': 'Db/docs',
    'file': 'README-DB.md',
    'item_group': 'SK_DbDocs',
    'build_action': 'None'
})

mdOutputProvider = MarkdownDocsOutputProvider(mdOutputProviderOptions, vsProjectFacade, compilerAssetProvider)
mdOutputProvider.writeSequence(sequenceResult)
mdOutputProvider.writeTable(tableResult)
mdOutputProvider.writeFunction(functionResult)
mdOutputProvider.commit()