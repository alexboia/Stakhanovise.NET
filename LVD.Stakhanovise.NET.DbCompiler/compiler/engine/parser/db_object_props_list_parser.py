from ..model.db_object_prop import DbObjectProp
from .support.named_args_list_parser import NamedArgsListParser

class DbObjectPropsListParser:
    def parse(self, propsListContents: str) -> list[DbObjectProp]:
        propsValues: dict = {}
        props: list[DbObjectProp] = []

        if (len(propsListContents) > 0):
            propsValues = self._readRawPropsValues(propsListContents)
        
        for propName in propsValues:
            props.append(DbObjectProp(propName, propsValues[propName]))

        return props

    def _readRawPropsValues(self, propsListContents: str) -> dict[str, str]:
        parser = NamedArgsListParser()
        propsValues = parser.parse(propsListContents)
        return propsValues