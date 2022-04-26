from ..model.db_table import DbTable
from ..model.db_column import DbColumn
from ..model.db_constraint import DbConstraint
from ..model.db_index import DbIndex
from ..model.db_mapping import DbMapping
from ..model.db_object_prop import DbObjectProp
from .db_column_parser import DbColumnParser
from .db_constraint_parser import DbConstraintParser
from .db_index_parser import DbIndexParser
from .db_object_props_list_parser import DbObjectPropsListParser
from .source_file_reader import SourceFileReader

MARKER_NAME_LINE = "NAME:"
MARKER_PROPS_LINE = "PROPS:"
MARKER_COLUMN_LINE = "COL:"
MARKER_CONSTRAINT_LINE = "CONSTRAINT:"
MARKER_INDEX_LINE = "IDX:"

class DbTableParser:
    _mapping: DbMapping = None

    def __init__(self, mapping: DbMapping):
        self._mapping = mapping

    def parseFromFile(self, sourceFile:str) -> DbTable:
        sourceFileReader = SourceFileReader()
        dbTableFileLines = sourceFileReader.readSourceLines(sourceFile)

        if __class__.isValidDbTableFileMapping(dbTableFileLines):
            return self.parse(dbTableFileLines)
        else:
            return None

    @staticmethod
    def isValidDbTableFileMapping(dbTableFileLines: list[str]) -> bool:
        return len(dbTableFileLines) > 0 and dbTableFileLines[0] == DbTable.getObjectType();

    def parse(self, sourceFileLines: list[str]) -> DbTable:
        name: str = None
        props: list[DbObjectProp] = []
        columns: list[DbColumn] = []
        primaryKey: DbConstraint = None
        uniqueKeys: list[DbConstraint] = []
        indexes: list[DbIndex] = []

        for sourceFileLine in sourceFileLines:
            if sourceFileLine.startswith(MARKER_NAME_LINE):
                name = self._readName(sourceFileLine)

            elif sourceFileLine.startswith(MARKER_PROPS_LINE):
                props = self._readProps(sourceFileLine)

            elif sourceFileLine.startswith(MARKER_COLUMN_LINE):
                column = self._readColumn(sourceFileLine)
                if column is not None:
                    columns.append(column)

            elif sourceFileLine.startswith(MARKER_INDEX_LINE):
                index = self._readIndex(sourceFileLine)
                if index is not None:
                    indexes.append(index)

            elif sourceFileLine.startswith(MARKER_CONSTRAINT_LINE):
                constraint = self._readConstraint(sourceFileLine)
                if constraint is not None:
                    if constraint.isUniqueConstraint():
                        uniqueKeys.append(constraint)
                    elif constraint.isPrimaryKeyConstraint():
                        primaryKey = constraint

        table = DbTable(name, props)
        table.setColumns(columns)
        table.setIndexes(indexes)
        table.setUniqueKeys(uniqueKeys)
        
        if primaryKey is not None:
            table.setPrimaryKey(primaryKey)

        return table

    def _readName(self, sourceFileLine: str) -> str:
        name = self._prepareNameLine(sourceFileLine)
        return self._mapping.expandString(name)

    def _prepareNameLine(self, sourceFileLine: str) -> str:
        return sourceFileLine.replace(MARKER_NAME_LINE, '').strip()

    def _readProps(self, sourceFileLine: str) -> list[DbObjectProp]:
        parser = DbObjectPropsListParser()
        propsListContents = self._preparePropsLine(sourceFileLine)
        return parser.parse(propsListContents) or []

    def _preparePropsLine(self, sourceFileLine: str) -> str:
        return sourceFileLine.replace(MARKER_PROPS_LINE, '').strip()

    def _readColumn(self, sourceFileLine: str) -> DbColumn:
        parser = DbColumnParser(self._mapping)
        columnContents = self._prepareColumnLine(sourceFileLine)
        return parser.parse(columnContents)

    def _prepareColumnLine(self, sourceFileLine: str) -> str:
        return sourceFileLine.replace(MARKER_COLUMN_LINE, '').strip()

    def _readIndex(self, sourceFileLine: str) -> DbIndex:
        parser = DbIndexParser(self._mapping)
        indexContents = self._prepareIndexLine(sourceFileLine)
        return parser.parse(indexContents)

    def _prepareIndexLine(self, sourceFileLine: str) -> str:
        return sourceFileLine.replace(MARKER_INDEX_LINE, '').strip()

    def _readConstraint(self, sourceFileLine: str) -> DbConstraint:
        parser = DbConstraintParser(self._mapping)
        constraintContents = self._prepareConstraintLine(sourceFileLine)
        return parser.parse(constraintContents)

    def _prepareConstraintLine(self, sourceFileline: str) -> str:
        return sourceFileline.replace(MARKER_CONSTRAINT_LINE, '').strip()