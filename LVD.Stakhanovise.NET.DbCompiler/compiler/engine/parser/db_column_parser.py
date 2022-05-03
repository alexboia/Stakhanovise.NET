from ..model.db_column import DbColumn
from ..model.db_mapping import DbMapping
from .support.named_spec_with_named_args import NamedSpecWithNamedArgs
from .support.named_spec_with_named_args_parser import NamedSpecWithNamedArgsParser

class DbColumnParser:
    _mapping: DbMapping

    def __init__(self, mapping: DbMapping):
        self._mapping = mapping

    def parse(self, columnContents: str) -> DbColumn:
        columnContents = columnContents or ''
        if (len(columnContents)  > 0):
            columnPropsValues = self._readRawColumnPropsValues(columnContents)

            name = columnPropsValues.getName()
            args = columnPropsValues.getArgs()
            
            args = self._expandArgs(args)
            return DbColumn.createFromNameAndArgs(name, args)
        else: 
            return None

    def _readRawColumnPropsValues(self, columnContents: str) -> NamedSpecWithNamedArgs:
        parser = NamedSpecWithNamedArgsParser()
        columnPropsValues = parser.parse(columnContents)
        return columnPropsValues

    def _expandArgs(self, args: dict[str, str]) -> dict[str, str]:
        expandedArgs = {};
        
        for argKey in args.keys():
            argValue = args[argKey]
            expandedArgs[argKey] = self._expandArg(argKey, argValue)

        return expandedArgs

    def _expandArg(self, argKey: str, argValue: str) -> str:
        if self._shouldExpandArg(argKey):
            return self._expandArgValue(argValue)
        else:
            return argValue

    def _shouldExpandArg(self, argKey: str) -> bool:
        return argKey == "default"

    def _expandArgValue(self, argValue: str) -> str:
        return self._mapping.expandString(argValue)