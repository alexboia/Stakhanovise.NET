from ..model.db_object_prop import DbObjectProp
from ..model.db_function_return import DbFunctionReturn
from ..model.db_function_param import DbFunctionParam
from ..model.db_function import DbFunction
from ..model.db_mapping import DbMapping
from .db_function_param_parser import DbFunctionParamParaser
from .db_function_return_parser import DbFunctionReturnParser
from .db_object_props_list_parser import DbObjectPropsListParser
from .source_file_reader import SourceFileReader

MARKER_NAME_LINE = "NAME:"
MARKER_PROPS_LINE = "PROPS:"
MARKER_PARAM_LINE = "PARAM:"
MARKER_RETURN_LINE = "RET:"
MARKER_BODY_START_LINE = "BODY:"
MARKER_BODY_END_LINE = "BODY;"

class DbFunctionParser:
    _mapping: DbMapping = None

    def __init__(self, mapping: DbMapping):
        self._mapping = mapping

    def parseFromFile(self, sourceFile:str) -> DbFunction:
        sourceFileReader = SourceFileReader(MARKER_BODY_START_LINE, MARKER_BODY_END_LINE)
        dbFunctionFileLines = sourceFileReader.readSourceLines(sourceFile)

        if __class__.isValidDbFunctionFileMapping(dbFunctionFileLines):
            return self.parse(dbFunctionFileLines)
        else:
            return None

    @staticmethod
    def isValidDbFunctionFileMapping(dbFunctionFileLines: list[str]) -> bool:
        return len(dbFunctionFileLines) > 0 and dbFunctionFileLines[0] == DbFunction.getObjectType();

    def parse(self, sourceFileLines: list[str]) -> DbFunction:
        name: str = None
        props: list[DbObjectProp] = []
        params: list[DbFunctionParam] = []
        returnInfo: DbFunctionReturn = None
        isReadingBody: bool = False
        bodyParts: list[str] = []

        for sourceFileLine in sourceFileLines:
            if sourceFileLine.startswith(MARKER_NAME_LINE):
                name = self._readName(sourceFileLine)

            elif sourceFileLine.startswith(MARKER_PROPS_LINE):
                props = self._readProps(sourceFileLine)

            elif sourceFileLine.startswith(MARKER_PARAM_LINE):
                param = self._readParam(sourceFileLine)
                if param is not None:
                    params.append(param)

            elif sourceFileLine.startswith(MARKER_RETURN_LINE):
                returnInfo = self._readReturnInfo(sourceFileLine)

            elif sourceFileLine == MARKER_BODY_START_LINE:
                isReadingBody = True

            elif sourceFileLine == MARKER_BODY_END_LINE:
                isReadingBody = False

            elif isReadingBody:
                bodyParts.append(sourceFileLine)

        dbFunction = DbFunction(name, props)
        dbFunction.setParams(params)
        dbFunction.setReturnInfo(returnInfo)
        dbFunction.setBody('\n'.join(bodyParts))

        return dbFunction

    def _readName(self, sourceFileLine: str) -> str:
        name = self._prepareNameLine(sourceFileLine)
        return self._mapping.expandString(name)

    def _prepareNameLine(self, sourceFileLine: str) -> str:
        return sourceFileLine.replace(MARKER_NAME_LINE, '').strip()

    def _readProps(self, sourceFileLine: str) -> list[DbObjectProp]:
        parser = DbObjectPropsListParser()
        propsListContents = self._preparePropsLine(sourceFileLine)
        return parser.parse(propsListContents) or []

    def _preparePropsLine(self, sourceFileLine: str) -> str:
        return sourceFileLine.replace(MARKER_PROPS_LINE, '').strip()

    def _readParam(self, sourceFileLine: str) -> DbFunctionParam:
        parser = DbFunctionParamParaser()
        paramContents = self._prepareNameLine(sourceFileLine)
        return parser.parse(paramContents)

    def _prepareParamLine(self, sourceFileLine: str) -> str:
        return sourceFileLine.replace(MARKER_PARAM_LINE, '').strip()

    def _readReturnInfo(self, sourceFileLine: str) -> DbFunctionReturn:
        parser = DbFunctionReturnParser()
        returnInfoContents = self._prepareReturnInfoLine(sourceFileLine)
        return parser.parse(returnInfoContents)

    def _prepareReturnInfoLine(self, sourceFileLine: str) -> str:
        return sourceFileLine.replace(MARKER_RETURN_LINE, '').strip()