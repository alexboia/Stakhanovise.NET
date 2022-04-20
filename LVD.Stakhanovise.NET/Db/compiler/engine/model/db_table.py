from .db_object_prop import DbObjectProp
from .db_object import DbObject
from .db_column import DbColumn
from .db_constraint import DbConstraint
from .db_index import DbIndex

class DbTable(DbObject):
    _columns: list[DbColumn] = []
    _primary: DbConstraint = None
    _uniqueKeys: list[DbConstraint] = []
    _indexes: list[DbIndex] = []

    def __init__(self, name, props: list[DbObjectProp] = []):
        super().__init__(name, "TBL", props)

    def addColumn(self, column: DbColumn):
        self._columns.append(column)

    def getColumns(self):
        return self._columns

    def setPrimaryKey(self, primaryKey: DbConstraint):
        self._primary = primaryKey

    def getPrimaryKey(self):
        return self._primary

    def hasPrimaryKey(self):
        return (self.getPrimaryKey() is not None)

    def addUniqueKey(self, uniqueKey: DbConstraint):
        self._uniqueKeys.append(uniqueKey)

    def getUniqueKeys(self):
        return self._uniqueKeys

    def hasUniqueKeys(self):
        return len(self.getUniqueKeys()) > 0

    def addIndex(self, index: DbIndex):
        self._indexes.append(index)

    def getIndexes(self):
        return self._indexes

    def hasIndexes(self):
        return len(self.getIndexes())