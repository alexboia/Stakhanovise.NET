from ..helper.string import sprintf, str_to_bool
from .db_object_prop import DbObjectProp
from .db_object import DbObject

KEY_PROP_START = "start"
KEY_PROP_INCREMENT = "increment"
KEY_PROP_MIN_VALUE = "min_value"
KEY_PROP_MAX_VALUE = "max_value"
KEY_PROP_CACHE = "cache"
KEY_PROP_CYCLE = 'cycle'

class DbSequence(DbObject):
    def __init__(self, name, props: list[DbObjectProp] = []):
        super().__init__(name, __class__.getObjectType(), props)

    def getStartValue(self) -> str:
        return self.getPropertyValue(KEY_PROP_START, "1")

    def getIncrementValue(self) -> str:
        return self.getPropertyValue(KEY_PROP_INCREMENT, "1")

    def getMinValue(self) -> str:
        return self.getPropertyValue(KEY_PROP_MIN_VALUE, None)

    def getMaxValue(self) -> str:
        return self.getPropertyValue(KEY_PROP_MAX_VALUE, None)

    def getCacheAmount(self) -> str:
        return self.getPropertyValue(KEY_PROP_CACHE, None)

    def shouldCycle(self) -> bool:
        return str_to_bool(self.getPropertyValue(KEY_PROP_CYCLE, 'false'))

    @staticmethod
    def getObjectType() -> str:
        return "SEQ"

    def __str__(self) -> str:
        return sprintf("{name = %s, props = %s}" % (self.getName(), self.getProperties()))