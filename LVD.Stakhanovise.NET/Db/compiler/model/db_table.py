from .db_object_prop import DbObjectProp
from .db_object import DbObject

class DbTable(DbObject):
    _columns: []
    _primary: None
    _uniqueKeys: []
    _indexes: []

    def __init__(self, name, props: list[DbObjectProp] = []):
        super().__init__(name, "TBL", props)
