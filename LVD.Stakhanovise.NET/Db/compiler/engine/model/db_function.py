from ..helper.string import sprintf
from .db_object import DbObject
from .db_object_prop import DbObjectProp
from .db_function_param import DbFunctionParam
from .db_function_return import DbFunctionReturn

class DbFunction(DbObject):
    _params: list[DbFunctionParam] = None
    _returnInfo: DbFunctionReturn = None
    _body: str = None

    def __init__(self, name, props: list[DbObjectProp] = []):
        super().__init__(name, __class__.getObjectType(), props)
        self._params = []
    
    def setParams(self, params: list[DbFunctionParam]) -> None:
        self._params = params or []

    def getParams(self) -> list[DbFunctionParam]:
        return self._params

    def hasParams(self) -> bool:
        return len(self.getParams()) > 0

    def setReturnInfo(self, returnInfo: DbFunctionReturn) -> None:
        self._returnInfo = returnInfo

    def getReturnInfo(self) -> DbFunctionReturn:
        return self._returnInfo

    def setBody(self, body: str) -> None:
        self._body = body

    def getBody(self) -> str:
        return self._body

    @staticmethod
    def getObjectType() -> str:
        return "FUNC"

    def __str__(self) -> str:
        return sprintf('{name = %s, props = %s, return = %s, params = %s}' % (self._name, self._properties, self._returnInfo, self._params))