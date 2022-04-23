from .support.named_spec_with_named_args import NamedSpecWithNamedArgs
from .support.named_args_list_parser import NamedArgsListParser
from .support.definition_with_properties import DefinitionWithProperties
from .support.definition_with_properties_parser import DefinitionWithPropertiesParser
from ..model.db_mapping import DbMapping
from ..model.db_index import DbIndex

class DbIndexParser:
    _mapping: DbMapping = None

    def __init__(self, mapping: DbMapping):
        self._mapping = mapping

    def parse(self, indexContents: str) -> DbIndex:
        if (len(indexContents) > 0):
            indexDefinition = self._readRawIndexDefinition(indexContents)
            if (indexDefinition is not None):
                indexName = indexDefinition.getName()
                indexColumns = self._readIndexColumns(indexDefinition.getArgsContents())

                indexType = indexDefinition.getProperty("type", None)
                if (indexType is None):
                    indexType = "btree"

                return DbIndex(indexName, indexColumns, indexType)
            else:
                return None
        else:
            return None

    def _readRawIndexDefinition(self, indexContents: str) -> DefinitionWithProperties:
       parser = DefinitionWithPropertiesParser(self._mapping)
       return parser.parse(indexContents)

    def _readIndexColumns(self, argsContents: str) -> dict[str, str]:
        parser = NamedArgsListParser(',')
        return parser.parse(argsContents)