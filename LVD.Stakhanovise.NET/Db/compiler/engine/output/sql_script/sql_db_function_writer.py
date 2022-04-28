from ...helper.string import sprintf
from ...helper.string_builder import StringBuilder
from ...model.db_function_param import DbFunctionParam
from ...model.db_function_return import DbFunctionReturn
from ...model.db_function import DbFunction
from .sql_db_object_writer import SqlDbObjectWriter

class SqlDbFunctionWriter(SqlDbObjectWriter[DbFunction]):
    def __init__(self, sqlStringBuilder: StringBuilder) -> None:
        super().__init__(sqlStringBuilder)

    def write(self, dbFunction: DbFunction) -> None:
        name = dbFunction.getName()
        separator = dbFunction.getSeparator()

        paramsList = self._getDbFunctionParamsSqlString(dbFunction)
        returnInfo = self._getDbFunctionReturnSqlString(dbFunction)

        self._sqlStringBuilder.appendLine('CREATE OR REPLACE FUNCTION public.' + name + ' (' + paramsList + ')')
        self._sqlStringBuilder.appendLineIndented('RETURNS ' + returnInfo)

        language = dbFunction.getLanguage()
        if language is not None:
            self._sqlStringBuilder.appendLineIndented('LANGUAGE ' + language)

        self._sqlStringBuilder.appendLineIndented('AS ' + separator)
        self._sqlStringBuilder.appendLine(dbFunction.getBody())
        self._sqlStringBuilder.appendLine(separator + ';')

    def _getDbFunctionParamsSqlString(self, dbFunction: DbFunction) -> str:
        paramsParts = []

        for dbFunctionParam in dbFunction.getParams():
            if dbFunctionParam.getDirection() == 'out':
                paramPart = 'OUT'
            else:
                paramPart = 'IN'

            paramPart = sprintf('%s %s %s' % (paramPart, 
                dbFunctionParam.getName(), 
                dbFunctionParam.getType()))

            if dbFunctionParam.hasDefaultValue():
                paramPart = sprintf('%s DEFAULT %s' % (paramPart, dbFunctionParam.getDefaultValue()))

            paramsParts.append(paramPart)

        return ', '.join(paramsParts)

    def _getDbFunctionReturnSqlString(self, dbFunction: DbFunction) -> str:
        returnColumnParts = []
        returnInfo = dbFunction.getReturnInfo()

        if returnInfo.isTableReturn():
            for columnName in returnInfo.getColumnNames():
                columnType = returnInfo.getColumnType(columnName)
                returnColumnPart = sprintf('%s %s' % (columnName, columnType))
                returnColumnParts.append(returnColumnPart)

        returnSqlString = returnInfo.getType().upper()
        if len(returnColumnParts) > 0:
            returnColumnSqlDefinitionString = ', '.join(returnColumnParts)
            returnSqlString = sprintf('%s (%s)' % (returnSqlString, returnColumnSqlDefinitionString))

        return returnSqlString
