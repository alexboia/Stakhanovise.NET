from typing import Callable
from ..model.db_mapping import DbMapping
from ..model.db_sequence import DbSequence
from ..model.db_table import DbTable
from ..model.db_function import DbFunction
from .db_object_parser import DbObjectParser
from .db_sequence_parser import DbSequenceParser
from .db_table_parser import DbTableParser
from .db_function_parser import DbFunctionParser

class DbObjectParserRegistry:
    _parsers: dict[str, Callable[[DbMapping], DbObjectParser]] = {}

    def __init__(self):
        self._parsers[DbSequence.getObjectType()] = (lambda mapping: DbSequenceParser(mapping))
        self._parsers[DbTable.getObjectType()] = (lambda mapping: DbTableParser(mapping))
        self._parsers[DbFunction.getObjectType()] = (lambda mapping: DbFunctionParser(mapping))

    def createParser(self, objectType: str, mapping: DbMapping) -> DbObjectParser:
        factory = self._parsers.get(objectType, None)
        if factory is not None:
            return factory(mapping)
        else:
            return None
