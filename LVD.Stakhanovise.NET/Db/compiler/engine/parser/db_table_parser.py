from ..model.db_table import DbTable
from ..model.db_column import DbColumn
from ..model.db_constraint import DbConstraint
from ..model.db_index import DbIndex
from ..model.db_mapping import DbMapping
from .db_column_parser import DbColumnParser
from .db_constraint_parser import DbConstraintParser
from .db_index_parser import DbIndexParser

MARKER_NAME_LINE = "NAME:"
MARKER_PROP_LINE = "PROPS:"
MARKER_COLUMN_LINE = "COL:"
MARKER_CONSTRAINT_LINE = "CONSTRAINT:"
MARKER_INDEX_LINE = "IDX:"

class DbTableParser:
    _mapping: DbMapping = None

    def __init__(self, mapping: DbMapping):
        self._mapping = mapping

    def parseFromFile(self, sourceFile:str) -> DbTable:
        pass

    @staticmethod
    def isValidDbTableFileMapping(dbTableFileLines: list[str]) -> bool:
        return len(dbTableFileLines) > 0 and dbTableFileLines[0] == DbTable.getObjectType();

    def parse(self, sourceFileLines: list[str]) -> DbTable:
        pass