from ..helper.string import sprintf

class DbIndex:
    _name:str = None
    _indexType:str = None
    _columns:dict = {}

    def __init__(self, name: str, columns: dict, indexType: str = "btree"):
        self._name = name
        self._columns = columns or {}
        self._indexType = indexType

    def getName(self):
        return self._name

    def getIndexType(self):
        return self._indexType

    def getColumns(self):
        return self._columns

    def getColumnNames(self):
        return self.getColumns().keys()

    def getColumnSortOrder(self, columnName: str) -> str:
        columns = self.getColumns()
        return (columns.get(columnName, None) or "ASC")

    def __str__(self) -> str:
        return sprintf("{name = %s, indexType = %s, columns = %s}" % (self._name, self._indexType, self._columns))