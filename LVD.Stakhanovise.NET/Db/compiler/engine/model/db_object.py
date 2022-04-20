from .db_object_prop import DbObjectProp

class DbObject:
    _name: str = None
    _type: str = None
    _properties: dict = {}

    def __init__(self, name: str, type: str, props: list[DbObjectProp] = []):
        self._name = name
        self._type = type

        for prop in props:
            self.addProperty(prop)

    def addProperty(self, prop:DbObjectProp):
        self._properties[prop.name] = prop

    def clearProperties(self):
        self._properties = {}

    def getProperties(self):
        return self._properties

    def getPropertyValue(self, key: str, defaultValue: str = None):
        prop = self._properties.get(key, None)
        if (prop is not None):
            return prop.value
        else:
            return defaultValue

    def getName(self):
        return self._name

    def getType(self):
        return self._type