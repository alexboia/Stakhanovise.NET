from ..model.db_function_return import DbFunctionReturn
from .support.named_spec_with_named_args import NamedSpecWithNamedArgs
from .support.named_spec_with_named_args_parser import NamedSpecWithNamedArgsParser

class DbFunctionReturnParser:
    def parse(self, returnInfoContents: str) -> DbFunctionReturn:
        returnInfoContents = returnInfoContents or ''
        if (len(returnInfoContents) > 0):
            returnInfoProps = self._readRawReturnPropsValues(returnInfoContents)

            returnType = returnInfoProps.getName()
            returnTableColumns = returnInfoProps.getArgs()

            if DbFunctionReturn.isTableReturnType(returnType):
                return DbFunctionReturn(returnType, returnTableColumns or [])
            else:
                return DbFunctionReturn(returnType)
        else:
            return None

    def _readRawReturnPropsValues(self, returnInfoContents: str) -> NamedSpecWithNamedArgs:
        parser = NamedSpecWithNamedArgsParser()
        paramPropsValues = parser.parse(returnInfoContents)
        return paramPropsValues