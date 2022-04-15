from .db_object_prop import DbObjectProp

class DbObject:
    _name = None
    _type = None
    _properties = []

    def __init__(self, name, type, props: list[DbObjectProp] = []):
        self._name = name
        self._type = type
        self._properties = props or []

    def addProperty(self, prop:DbObjectProp):
        self._properties.append(prop)

    def clearProperties(self):
        self._properties = []

    def getProperties(self):
        return self._properties

    def getName(self):
        return self._name

    def getType(self):
        return self._type