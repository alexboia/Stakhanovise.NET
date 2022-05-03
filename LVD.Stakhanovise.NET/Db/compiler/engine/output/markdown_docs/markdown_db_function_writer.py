from ...helper.string import sprintf
from ...helper.string_builder import StringBuilder

from ...model.db_function_return import DbFunctionReturn
from ...model.db_function_param import DbFunctionParam
from ...model.db_function import DbFunction

from ..sql_script.sql_db_function_writer import SqlDbFunctionWriter
from .markdown_db_object_writer import MarkdownDbObjectWriter

class MarkdownDbFunctionWriter(MarkdownDbObjectWriter[DbFunction]):
    def __init__(self, mdStringBuilder: StringBuilder) -> None:
        super().__init__(mdStringBuilder)

    def write(self, dbFunction: DbFunction) -> None:
        self._writeObjectHeader(dbFunction, defaultTitlePrefix = 'Function')
        self._writeFunctionSqlDeclaration(dbFunction)
        self._writeFunctionParameters(dbFunction)

    def _writeFunctionSqlDeclaration(self, dbFunction: DbFunction) -> None:
        sqlBuffer = StringBuilder()
        sqlWriter = SqlDbFunctionWriter(sqlBuffer)
        sqlWriter.write(dbFunction)

        self._writeMarkdownCodeBlock(sqlBuffer.toString().rstrip())
        self._writeSpacer()

    def _writeFunctionParameters(self, dbFunction: DbFunction) -> None:
        if (dbFunction.hasParams()):
            self._writeLines('The function parameters are explained below:')
            self._writeSpacer()

            columns = ['Parameter', 'Type', 'Notes']
            rows = self._getFunctionParametersTableRows(dbFunction)

            self._writeMarkdownTable(columns, rows)
            self._writeSpacer()

    def _getFunctionParametersTableRows(self, dbFunction: DbFunction) -> list[list[str]]:
        rows = []

        for param in dbFunction.getParams():
            paramNameString = sprintf('`%s`' %  (param.getName()))
            paramTypeString = sprintf('`%s`' % (param.getType()))
            paramDescriptionString = param.getDescription() or '-'

            rows.append([paramNameString, 
                paramTypeString, 
                paramDescriptionString])

        return rows