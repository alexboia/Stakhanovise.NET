from ..helper.string import sprintf
from .db_object_prop import DbObjectProp
from .db_object import DbObject

KEY_PROP_START = "start"
KEY_PROP_INCREMENT = "increment"
KEY_PROP_MIN_VALUE = "min_value"
KEY_PROP_MAX_VALUE = "max_value"
KEY_PROP_CACHE = "cache"

class DbSequence(DbObject):
    def __init__(self, name, props: list[DbObjectProp] = []):
        super().__init__(name, __class__.getObjectType(), props)

    def getStartValue(self) -> str:
        return self.getPropertyValue(KEY_PROP_START, "1")

    def getIncrementValue(self) -> str:
        return self.getPropertyValue(KEY_PROP_INCREMENT, "1")

    def getMinValue(self) -> str:
        return self.getPropertyValue(KEY_PROP_MIN_VALUE, "1")

    def getMaxValue(self) -> str:
        return self.getPropertyValue(KEY_PROP_MAX_VALUE, "9223372036854775807")

    def getCacheAmount(self) -> str:
        return self.getPropertyValue(KEY_PROP_CACHE, "1")

    @staticmethod
    def getObjectType() -> str:
        return "SEQ"

    def __str__(self) -> str:
        return sprintf("{name = %s, props = %s}" % (self.getName(), self.getProperties()))