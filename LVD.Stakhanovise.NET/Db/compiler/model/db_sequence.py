from .db_object_prop import DbObjectProp
from .db_object import DbObject

class DbSequence(DbObject):
    def __init__(self, name, props: list[DbObjectProp] = []):
        super().__init__(name, "SEQ", props)