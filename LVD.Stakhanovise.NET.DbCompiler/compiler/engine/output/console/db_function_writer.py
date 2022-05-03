from prettytable import PrettyTable
from rich import print
from rich.text import Text
from rich.console import Console
from rich.panel import Panel

from .db_object_writer import DbObjectWriter

from ...model.db_function_param import DbFunctionParam
from ...model.db_function_return import DbFunctionReturn
from ...model.db_function import DbFunction

class DbFunctionWriter(DbObjectWriter[DbFunction]):
    def __init__(self, console: Console) -> None:
        super().__init__(console)

    def write(self, dbFunction: DbFunction) -> None:
        if not isinstance(dbFunction, DbFunction):
            raise TypeError('Object does not represent a database function')

        self._writeObjectTitle("Function", dbFunction.getName())
        self._writeObjectProperties(dbFunction.getProperties())
        self._writeFunctionParams(dbFunction.getParams())
        self._writeFunctionReturn(dbFunction.getReturnInfo())

    def _writeFunctionParams(self, params: list[DbFunctionParam]) -> None:
        self._writeObjectSectionTitle('Parameters:')

        if params is not None and len(params) > 0:
            paramsDescription = self._getFunctionParamsDescription(params)
            self._console.print(paramsDescription)
        else:
            paramsMissing = self._getObjectMissingMessage('parameters', False)
            self._console.print(paramsMissing)

        self._writeSectionSpacer()

    def _getFunctionParamsDescription(self, params: list[DbFunctionParam]) -> str:
        paramsTable = PrettyTable()
        paramsTable.field_names = ['Name', 'Type', 'Direction', 'Default Value']

        for param in params:
            paramsTable.add_row([param.getName(), 
                param.getType(), 
                param.getDirection(), 
                param.getDefaultValue() or "[No default]"])

        return paramsTable.get_string()

    def _writeFunctionReturn(self, returnInfo: DbFunctionReturn) -> None:
        self._writeObjectSectionTitle('Return Info:')

        if returnInfo is not None:
            returnInfoDescription = self._getFunctionReturnDescription(returnInfo)
            self._console.print(returnInfoDescription)
        else:
            returnInfoMissing = self._getObjectMissingMessage('return info')
            self._console.print(returnInfoMissing)

        self._writeSectionSpacer()

    def _getFunctionReturnDescription(self, returnInfo: DbFunctionReturn) -> str:
        returnInfoTable = PrettyTable()
        returnInfoTable.field_names = ['Type', 'Columns']

        if returnInfo.isTableReturn():
            returnInfoTable.add_row([returnInfo.getType(), self._getFunctionTableReturnColumnDescription(returnInfo)])
        else:
            returnInfoTable.add_row([returnInfo.getType(), '-'])

        return returnInfoTable.get_string()

    def _getFunctionTableReturnColumnDescription(self, returnInfo: DbFunctionReturn) -> str:
        columnParts = []

        if returnInfo.isTableReturn():
            for colName in returnInfo.getColumnNames():
                columnParts.append(colName + ': ' + returnInfo.getColumnType(colName))

        return '\n'.join(columnParts)
