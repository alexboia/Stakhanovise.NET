from ..helper.string import sprintf
from .db_object_prop import DbObjectProp
from .db_object import DbObject
from .db_column import DbColumn
from .db_constraint import DbConstraint
from .db_index import DbIndex

KEY_PROP_TITLE = "title"
KEY_PROP_DESCRIPTION = "description"

class DbTable(DbObject):
    _columns: list[DbColumn] = None
    _primary: DbConstraint = None
    _uniqueKeys: list[DbConstraint] = None
    _indexes: list[DbIndex] = None

    def __init__(self, name, props: list[DbObjectProp] = []):
        super().__init__(name, __class__.getObjectType(), props)

        self._columns = []
        self._primary = None
        self._uniqueKeys = []
        self._indexes = []

    def addColumn(self, column: DbColumn):
        self._columns.append(column)

    def setColumns(self, columns: list[DbColumn]):
        self._columns = columns or []

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

    def setUniqueKeys(self, uniqueKeys: list[DbConstraint]):
        self._uniqueKeys = uniqueKeys or []

    def getUniqueKeys(self):
        return self._uniqueKeys

    def hasUniqueKeys(self):
        return len(self.getUniqueKeys()) > 0

    def addIndex(self, index: DbIndex):
        self._indexes.append(index)

    def setIndexes(self, indexes: list[DbIndex]):
        self._indexes = indexes or []

    def getIndexes(self):
        return self._indexes

    def hasIndexes(self):
        return len(self.getIndexes())

    def getMetaTitle(self) -> str:
        return self.getPropertyValue(KEY_PROP_TITLE)

    def getMetaDescription(self) -> str:
        return self.getPropertyValue(KEY_PROP_DESCRIPTION)

    @staticmethod
    def getObjectType() -> str:
        return "TBL"

    def __str__(self) -> str:
        return sprintf('{name = %s, props = %s, columns = %s, indexes = %s, uniques = %s}' % (self._name, self._properties, self._columns, self._indexes, self._uniqueKeys))