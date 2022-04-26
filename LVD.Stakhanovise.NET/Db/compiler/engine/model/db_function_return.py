from ..helper.string import sprintf

TYPE_TABLE = "table"

class DbFunctionReturn:
    _returnType: str = None
    _columns: dict[str, str] = None

    def __init__(self, returnType: str, columns: dict[str, str] = None):
        self._returnType = returnType
        if __class__.isTableReturnType(returnType):
            self._columns = columns or []

    @staticmethod
    def isTableReturnType(returnType: str) -> bool:
        return returnType == TYPE_TABLE

    def getType(self) -> str:
        return self._returnType

    def isTableReturn(self) -> bool:
        return __class__.isTableReturnType(self.getType())

    def getColumns(self) -> dict[str, str]:
        return self._columns

    def __str__(self) -> str:
        return sprintf('{type = %s, columns = %s}' % (self._returnType, self._columns))