from typing import Callable
from .db_object_prop import DbObjectProp

KEY_PROP_TITLE = "title"
KEY_PROP_DESCRIPTION = "description"

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

    def getProperties(self, applyFilter: Callable[[str, DbObjectProp], bool] = None) -> dict[str, DbObjectProp]:
        properties: dict[str, DbObjectProp] = {}

        for propKey in self._properties.keys():
            prop = self._properties[propKey]
            if applyFilter is None or applyFilter(propKey, prop):
                properties[propKey] = prop

        return properties

    def getNonMetaProperties(self) -> dict[str, DbObjectProp]:
        return self.getProperties(lambda key, prop: key != KEY_PROP_TITLE and key != KEY_PROP_DESCRIPTION)

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

    def getMetaTitle(self) -> str:
        return self.getPropertyValue(KEY_PROP_TITLE)

    def getMetaDescription(self) -> str:
        return self.getPropertyValue(KEY_PROP_DESCRIPTION)