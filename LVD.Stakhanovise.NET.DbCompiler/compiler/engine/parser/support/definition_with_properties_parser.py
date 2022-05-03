from ...model.db_mapping import DbMapping
from .named_args_list_parser import NamedArgsListParser
from .definition_with_properties import DefinitionWithProperties
from .named_spec_with_args_raw_parser import NamedSpecWithArgsRawParser

class DefinitionWithPropertiesParser:
    _mapping: DbMapping = None

    def __init__(self, mapping: DbMapping):
        self._mapping = mapping

    def parse(self, contents: str) -> DefinitionWithProperties:
        if (len(contents) > 0):
            definitionParts = self._readDefinitionParts(contents)
            if (len(definitionParts.keys())> 0):
                definitionObject = definitionParts.get("object")
                definitionProperties = definitionParts.get("properties", None) or {}

                definitionObjectName = definitionObject.get("name")
                definitionObjectName = self._expandName(definitionObjectName)
                definitionObjectArgs = definitionObject.get("args") or ''

                return DefinitionWithProperties(definitionObjectName, definitionObjectArgs, definitionProperties)
            else:
                return None
        else:
            return None

    def _readDefinitionParts(self, contents: str) -> dict[str, str]:
        definitionParts = {}
        rawDefinitionParts = contents.split(';', 1)
        
        if (len(rawDefinitionParts) >= 1):
            definitionObject = rawDefinitionParts[0].strip()
            if (len(rawDefinitionParts) == 2):
                definitionProperties = rawDefinitionParts[1].strip()
            else:
                definitionProperties = ""

            if (len(definitionObject) > 0):
                definitionParts["object"] = self._readDefinitionObjectParts(definitionObject)

            if (len(definitionProperties) > 0):
                definitionParts["properties"] = self._readDefinitionProperties(definitionProperties)

        return definitionParts

    def _readDefinitionObjectParts(self, definitionObject: str) -> dict[str, str]:
        parser = NamedSpecWithArgsRawParser()
        return parser.parse(definitionObject)

    def _readDefinitionProperties(self, definitionProperties: str) -> dict:
        parser = NamedArgsListParser(';')
        return parser.parse(definitionProperties)

    def _expandName(self, name:str) -> str:
        return self._mapping.expandString(name)
