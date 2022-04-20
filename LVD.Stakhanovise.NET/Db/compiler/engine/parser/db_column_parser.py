from ..model.db_column import DbColumn
from .named_args_list_parser import NamedArgsListParser

class DbColumnParser:
    def parse(self, columnContents: str) -> DbColumn:
        if (len(propsListContents)  > 0):
            columnPropsValues = self._readRawColumnPropsValues(columnContents)
            return DbColumn.createFromInput(columnPropsValues)
        else: 
            return None

    def _readRawColumnPropsValues(self, columnContents) -> dict:
        parser = NamedArgsListParser()
        columnPropsValues = parser.parse(columnContents)
        return columnPropsValues