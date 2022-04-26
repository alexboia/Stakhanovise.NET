from ..helper.string import sprintf

class DbFunctionParam:
    _name: str = None
    _direction: str = "in"
    _type: str = None
    _defaultValue: str = None

    def __init__(self, name: str, type: str, direction: str = "in", defaultValue: str = None):
        self._name = name
        self._type = type
        self._direction = direction
        self._defaultValue = defaultValue

    @staticmethod
    def createFromNameAndArgs(name: str, args: dict[str, str]):
        paramType = args.get("type", "character varying")
        paramDirection = args.get("direction", "in")
        paramDefault = args.get("default", None)

        return DbFunctionParam(name, 
            paramType, 
            paramDirection, 
            paramDefault)

    def getName(self) -> str:
        return self._name

    def getDirection(self) -> str:
        return self._direction

    def getType(self) -> str:
        return self._type

    def getDefaultValue(self) -> str:
        return self._defaultValue

    def hasDefaultValue(self) -> bool:
        return self.getDefaultValue() is not None

    def __str__(self) -> str:
        return sprintf('{name = %s, type = %s, direction = %s, defaultValue = %s}' % (self._name, self._type, self._direction, self._defaultValue))