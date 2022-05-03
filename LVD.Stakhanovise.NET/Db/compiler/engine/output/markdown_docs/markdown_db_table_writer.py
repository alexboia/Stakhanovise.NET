from ...helper.string import sprintf
from ...helper.string_builder import StringBuilder
from ...model.db_column import DbColumn
from ...model.db_table import DbTable
from .markdown_db_object_writer import MarkdownDbObjectWriter

class MarkdownDbTableWriter(MarkdownDbObjectWriter[DbTable]): 
    def __init__(self, mdStringBuilder: StringBuilder) -> None:
        super().__init__(mdStringBuilder)

    def write(self, dbTable: DbTable) -> None:
        self._writeObjectHeader(dbTable, defaultTitlePrefix = 'Table')
        self._writeDbTableColumns(dbTable)

    def _writeDbTableColumns(self, dbTable: DbTable) -> None:
        if dbTable.hasColumns():
            columns = ['Column', 'Type', 'Notes']
            rows = self._getDbTableColumnsRows(dbTable)

            self._writeMarkdownTable(columns, rows)
            self._writeSpacer()

    def _getDbTableColumnsRows(self, dbTable: DbTable) -> list[list[str]]:
        rows = []

        for column in dbTable.getColumns():
            colNameString = sprintf('`%s`' % (column.getName()))
            colTypeString = sprintf('`%s`' % (column.getType()))
            colNotesString = self._getDbTableColumnNotesString(dbTable, column)
            
            rows.append([colNameString, 
                colTypeString, 
                colNotesString])

        return rows

    def _getDbTableColumnNotesString(self, dbTable: DbTable, column: DbColumn) -> str:
        notes = []

        if column.isNotNull():
            notes.append('`NOT NULL`')

        if dbTable.isColumnPartOfPrimaryKey(column.getName()):
            notes.append('`Primary Key`')

        if dbTable.isColumnPartOfAnyUniqueKey(column.getName()):
            notes.append('`Unique Key`')

        if column.hasDefaultValue():
            notes.append(sprintf('`DEFAULT %s`' % (column.getDefaultValue())))

        if len(notes) > 0:
            return ', '.join(notes)
        else:
            return '-'