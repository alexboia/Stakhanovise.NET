from ..helper.string import str_to_bool
from ..helper.string import sprintf

class DbColumn:
    _name: str = None
    _type: str = None
    _notNull: bool = False
    _defaultValue: str = None
    _description: str = None

    def __init__(self, name: str, type: str, notNull: bool = False, defaultValue: str = None, description: str = None):
        self._name = name
        self._type = type
        self._notNull = notNull == True
        self._defaultValue = defaultValue
        self._description = description

    @staticmethod
    def createFromNameAndArgs(name: str, args: dict[str, str]):
        colType = args.get("type", "character varying(255)")
        colNotNull = str_to_bool(args.get("not_null", "false"))
        colDefault = args.get("default", None)
        colDescription = args.get("description", None)

        return DbColumn(name, 
            colType, 
            colNotNull, 
            colDefault, 
            colDescription)

    def getName(self):
        return self._name

    def getType(self):
        return self._type

    def isNotNull(self):
        return self._notNull

    def getDefaultValue(self):
        return self._defaultValue

    def hasDefaultValue(self):
        return (self.getDefaultValue() is not None)

    def getDescritption(self):
        return self._description

    def hasDescription(self):
        return (self.getDescritption() is not None)

    def __str__(self) -> str:
        return sprintf('{name: %s, type: %s, notNull: %s}' % (self._name, self._type, self._notNull))