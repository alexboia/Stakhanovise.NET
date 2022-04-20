from os import chdir
from engine.parser.makefile_parser import MakefileParser
from engine.parser.db_mapping_parser import DbMappingParser
from engine.parser.db_sequence_parser import DbSequenceParser
from engine.parser.db_index_parser import DbIndexParser

chdir('../src')

mappingParser = DbMappingParser()
mapping = mappingParser.parse('./sk_mapping.dbmap')

indexParser = DbIndexParser(mapping)
index = indexParser.parse('idx_$queue_table_name$_sort_index(task_priority, task_locked_until_ts=ASC, task_lock_handle_id=ASC); type=btree')

print(index)

parser = DbSequenceParser(mapping)
result = parser.parseFromFile('./sk_processing_queues_task_lock_handle_id_seq.dbdef')

print(result.getName())
print(result.getProperties())