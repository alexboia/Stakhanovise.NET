from ...helper.string import sprintf
from ...helper.string_builder import StringBuilder
from ...model.db_table import DbTable
from ...model.db_column import DbColumn
from ...model.db_constraint import DbConstraint
from ...model.db_index import DbIndex
from .sql_db_object_writer import SqlDbObjectWriter

class SqlDbTableWriter(SqlDbObjectWriter[DbTable]):
    def __init__(self, sqlStringBuilder: StringBuilder) -> None:
        super().__init__(sqlStringBuilder)

    def write(self, dbTable: DbTable) -> None:
        self._writeTableDefinitionSqlString(dbTable)

        if dbTable.hasPrimaryKey():
            self._writePrimarykeySqlString(dbTable)

        if dbTable.hasUniqueKeys():
            self._writeUniqueKeysSqlString(dbTable)

        if dbTable.hasIndexes():
            self._writeIndexesSqlString(dbTable)

    def _writeTableDefinitionSqlString(self, dbTable: DbTable) -> None:
        self._sqlStringBuilder.appendLine('CREATE TABLE IF NOT EXISTS public.' + dbTable.getName() + '(')

        for columnIndex in dbTable.getColumnIndexes():
            dbColumn = dbTable.getColumnAtIndex(columnIndex)
            columnSqlString = self._buildColumnSqlString(dbColumn)
            
            if columnIndex < dbTable.getColumnCount() - 1:
                columnSqlString += ','

            self._sqlStringBuilder.appendLineIndented(columnSqlString)

        self._sqlStringBuilder.appendLine(');')

    def _buildColumnSqlString(self, dbColumn: DbColumn) -> str:
        columnStringParts = []

        columnStringParts.append(dbColumn.getName())
        columnStringParts.append(dbColumn.getType())

        if dbColumn.hasDefaultValue():
            columnStringParts.append('DEFAULT ' + dbColumn.getDefaultValue())

        if dbColumn.isNotNull():
            columnStringParts.append('NOT NULL')

        return ' '.join(columnStringParts)

    def _writePrimarykeySqlString(self, dbTable: DbTable) -> None:
        dbPrimaryKey = dbTable.getPrimaryKey()

        self._sqlStringBuilder.appendEmptyLine()
        self._sqlStringBuilder.appendLine('ALTER TABLE ONLY public.' + dbTable.getName())
        self._sqlStringBuilder.appendLineIndented('ADD CONSTRAINT ' + dbPrimaryKey.getName())
        self._sqlStringBuilder.appendLineIndented('PRIMARY KEY (' + ','.join(dbPrimaryKey.getColumnNames()) + ');')

    def _writeUniqueKeysSqlString(self, dbTable: DbTable) -> None:
        for dbUniqueKey in dbTable.getUniqueKeys():
            self._sqlStringBuilder.appendEmptyLine()
            self._sqlStringBuilder.appendLine('ALTER TABLE ONLY public.' + dbTable.getName())
            self._sqlStringBuilder.appendLineIndented('ADD CONSTRAINT ' + dbUniqueKey.getName())
            self._sqlStringBuilder.appendLineIndented('UNIQUE (' + ','.join(dbUniqueKey.getColumnNames()) + ');')

    def _writeIndexesSqlString(self, dbTable: DbTable) -> None:
        for dbIndex in dbTable.getIndexes():
            self._sqlStringBuilder.appendEmptyLine()
            self._sqlStringBuilder.appendLine('CREATE INDEX ' + dbIndex.getName())
            self._sqlStringBuilder.appendLineIndented('ON public.' + dbTable.getName() + ' USING ' + dbIndex.getIndexType())
            self._sqlStringBuilder.appendLineIndented('(' + self._getIndexSqlColumnsString(dbIndex) + ');')

    def _getIndexSqlColumnsString(self, dbIndex: DbIndex) -> str:
        columnNamesParts = []

        for columnName in dbIndex.getColumnNames():
            columnNamesParts.append(sprintf('%s %s' % (columnName, dbIndex.getColumnSortOrder(columnName))))

        return ', '.join(columnNamesParts)