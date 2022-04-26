from ..model.db_function_param import DbFunctionParam
from .support.named_spec_with_named_args import NamedSpecWithNamedArgs
from .support.named_spec_with_named_args_parser import NamedSpecWithNamedArgsParser

class DbFunctionParamParaser:
    def parse(self, functionParamContents: str) -> DbFunctionParam:
        functionParamContents = functionParamContents or ''
        if (len(functionParamContents) > 0):
            paramPropsValues = self._readRawParamPropsValues(functionParamContents)

            name = paramPropsValues.getName()
            args = paramPropsValues.getArgs()

            return DbFunctionParam.createFromNameAndArgs(name, args)
        else:
            return None

    def _readRawParamPropsValues(self, functionParamContents: str) -> NamedSpecWithNamedArgs:
        parser = NamedSpecWithNamedArgsParser()
        paramPropsValues = parser.parse(functionParamContents)
        return paramPropsValues