class DefinitionWithProperties:
    _name: str = None
    _argsContents: str = None
    _properties: dict[str, str] = None

    def __init__(self, name: str, argsContents: str, properties: dict[str, str]):
        self._name = name
        self._argsContents = argsContents or ''
        self._properties = properties or {}

    def getName(self) -> str:
        return self._name

    def getArgsContents(self) -> str:
        return self._argsContents

    def getProperties(self) -> str:
        return self._properties

    def getProperty(self, propKey: str, defaultValue: str = None) -> str:
        return self._properties.get(propKey, defaultValue)