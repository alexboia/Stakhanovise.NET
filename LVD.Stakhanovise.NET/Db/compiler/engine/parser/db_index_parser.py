from .named_spec_with_named_args import NamedSpecWithNamedArgs
from .named_spec_with_named_args_parser import NamedSpecWithNamedArgsParser
from .named_args_list_parser import NamedArgsListParser
from ..model.db_mapping import DbMapping
from ..model.db_index import DbIndex

class DbIndexParser:
    _mapping: DbMapping = None

    def __init__(self, mapping: DbMapping):
        self._mapping = mapping

    def parse(self, indexContents: str) -> DbIndex:
        if (len(indexContents) > 0):
            indexArgs = self._readRawIndexArgsValues(indexContents)
            if (len(indexArgs.keys()) > 0):
                indexDefinition: NamedSpecWithNamedArgs = indexArgs.get("definition")
                indexProperties: dict = indexArgs.get("properties", None)

                name = indexDefinition.getName()
                columns = indexDefinition.getArgs()

                if (indexProperties is not None):
                    indexType = indexProperties.get("type", None)
                else:
                    indexType = None

                if (indexType is None):
                    indexType = "btree"

                return DbIndex(self._expandName(name), columns, indexType)
            else:
                return None
        else:
            return None

    def _readRawIndexArgsValues(self, indexContents: str) -> dict:
        indexArgs = {}
        indexParts = indexContents.split(';', 1)
        
        if (len(indexParts) >= 1):
            indexDefinition = indexParts[0].strip()
            if (len(indexParts) == 2):
                indexProperties = indexParts[1].strip()
            else:
                indexProperties = ""

            if (len(indexDefinition) > 0):
                indexArgs["definition"] = self._readRawIndexDefinitionValues(indexDefinition)

            if (len(indexProperties) > 0):
                indexArgs["properties"] = self._readRawIndexPropertiesValues(indexProperties)

        return indexArgs

    def _readRawIndexDefinitionValues(self, indexDefinition: str) -> NamedSpecWithNamedArgs:
        parser = NamedSpecWithNamedArgsParser(',')
        return parser.parse(indexDefinition)

    def _expandName(self, name:str) -> str:
        return self._mapping.expandString(name)

    def _readRawIndexPropertiesValues(self, indexProperties: str) -> dict:
        parser = NamedArgsListParser(';')
        return parser.parse(indexProperties)