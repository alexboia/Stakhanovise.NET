from .db_object_prop import DbObjectProp

class DbObject:
    _name: str = None
    _type: str = None
    _properties: dict[str, DbObjectProp] = None

    def __init__(self, name: str, type: str, props: list[DbObjectProp] = []):
        self._name = name
        self._type = type
        self._properties = {}

        for prop in props:
            self.addProperty(prop)

    def addProperty(self, prop:DbObjectProp) -> None:
        self._properties[prop.name] = prop

    def clearProperties(self) -> None:
        self._properties = {}

    def getProperties(self) -> dict[str, DbObjectProp]:
        return self._properties

    def getPropertyValue(self, key: str, defaultValue: str = None) -> str:
        prop = self._properties.get(key, None)
        if (prop is not None):
            return prop.value
        else:
            return defaultValue

    def getName(self) -> str:
        return self._name

    def getType(self) -> str:
        return self._type