from ..helper.string import sprintf

TYPE_BTREE = "btree"
SORT_ORDER_ASC = "ASC"

class DbIndex:
    _name:str = None
    _indexType:str = None
    _columns:dict[str, str] = None

    def __init__(self, name: str, columns: dict[str, str], indexType: str = TYPE_BTREE):
        self._name = name
        self._columns = columns or {}
        self._indexType = indexType

    def getName(self) -> str:
        return self._name

    def getIndexType(self) -> str:
        return self._indexType

    def getColumns(self) -> dict[str, str]:
        return self._columns

    def getColumnNames(self) -> list[str]:
        return list(self.getColumns().keys())

    def getColumnSortOrder(self, columnName: str) -> str:
        columns = self.getColumns()
        return (columns.get(columnName, None) or SORT_ORDER_ASC)

    def __str__(self) -> str:
        return sprintf("{name = %s, indexType = %s, columns = %s}" % (self._name, self._indexType, self._columns))