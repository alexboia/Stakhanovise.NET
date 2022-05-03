from prettytable import PrettyTable
from rich import print
from rich.text import Text
from rich.console import Console
from rich.panel import Panel

from ...helper.string import sprintf
from ...helper.string import bool_to_yesno
from ...model.db_table import DbTable
from ...model.db_index import DbIndex
from ...model.db_column import DbColumn
from ...model.db_constraint import DbConstraint

from .db_object_writer import DbObjectWriter

class DbTableWriter(DbObjectWriter[DbTable]):
    _showIndexes: bool = True
    _showUniqueKeys: bool = True

    def __init__(self, console: Console, showIndexes: bool = True, showUniqueKeys: bool = True) -> None:
        super().__init__(console)
        self._showIndexes = showIndexes
        self._showUniqueKeys = showUniqueKeys

    def write(self, dbTable: DbTable) -> None:
        if not isinstance(dbTable, DbTable):
            raise TypeError('Object does not represent a database table')

        self._writeObjectTitle("Table", dbTable.getName())
        self._writeObjectProperties(dbTable.getProperties())
        self._writeTableColumns(dbTable.getColumns())
        self._writePrimaryKey(dbTable.getPrimaryKey())

        if self._showUniqueKeys:
            self._writeUniqueKeys(dbTable.getUniqueKeys())

        if self._showIndexes:
            self._writeIndexes(dbTable.getIndexes())

    def _writeTableColumns(self, columns: list[DbColumn]) -> None:
        self._writeObjectSectionTitle("Columns:")

        if len(columns) > 0:
            colsDescription = self._getTableColumnsDescription(columns)
            self._console.print(colsDescription)
        else:
            colsMissing = self._getObjectMissingMessage('columns')
            self._console.print(colsMissing)
        
        self._writeSectionSpacer()

    def _getTableColumnsDescription(self, columns: list[DbColumn]) -> str:
        colsTable = PrettyTable()
        colsTable.field_names = ['Name', 'Description', 'Type', 'Not Null', 'Default Value']
        
        for col in columns:
            colsTable.add_row([col.getName(), 
                col.getDescritption() or '[No description]', 
                col.getType(), 
                bool_to_yesno(col.isNotNull()), 
                col.getDefaultValue() or '[No default]'])

        return colsTable.get_string()

    def _writePrimaryKey(self, primaryKey: DbConstraint) -> None:
        self._writeObjectSectionTitle('Primary Key:')

        if primaryKey is not None:
            pkDescription = self._getTableConstraintsDescription([primaryKey])
            self._console.print(pkDescription)
        else:
            pkMissing = self._getObjectMissingMessage('primary key')
            self._console.print(pkMissing)

        self._writeSectionSpacer()

    def _getTableConstraintsDescription(self, constraints: list[DbConstraint]) -> str:
        constraintTable = PrettyTable()
        constraintTable.field_names = ['Name', 'Columns']

        for constraint in constraints:
            constraintTable.add_row([constraint.getName(), ','.join(constraint.getColumnNames())])

        return constraintTable.get_string()

    def _writeUniqueKeys(self, uniqueKeys: list[DbConstraint]) -> None:
        self._writeObjectSectionTitle('Unique keys:')

        if uniqueKeys is not None and len(uniqueKeys) > 0:
            uniqueKeysDescription = self._getTableConstraintsDescription(uniqueKeys)
            self._console.print(uniqueKeysDescription)
        else:
            uniqueKeysMissing = self._getObjectMissingMessage('unique keys', False)
            self._console.print(uniqueKeysMissing)

        self._writeSectionSpacer()

    def _writeIndexes(self, indexes: list[DbIndex]) -> None:
        indexesTitle = self._getObjectSectionTitle('Indexes:')
        self._console.print(indexesTitle)

        if indexes is not None and len(indexes) > 0:
            indexesDescription = self._getTableIndexDescription(indexes)
            self._console.print(indexesDescription)
        else:
            indexesMissing = self._getObjectMissingMessage('indexes', False)
            self._console.print(indexesMissing)

        self._writeSectionSpacer()

    def _getTableIndexDescription(self, indexes: list[DbIndex]) -> str:
        indexesTable = PrettyTable()
        indexesTable.field_names = ['Name', 'Type', 'Columns']

        for index in indexes:
            indexesTable.add_row([index.getName(), 
                index.getIndexType(), 
                self._getTableIndexCoumnDescription(index)])

        return indexesTable.get_string()

    def _getTableIndexCoumnDescription(self, index: DbIndex) -> str:
        columnParts: list[str] = []
        
        for colName in index.getColumnNames():
            colPart = colName + ': ' + index.getColumnSortOrder(colName)
            columnParts.append(colPart)

        return '\n'.join(columnParts)