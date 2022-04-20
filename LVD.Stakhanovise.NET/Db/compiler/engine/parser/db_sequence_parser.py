from ..model.db_mapping import DbMapping
from ..model.db_sequence import DbSequence
from ..model.db_object_prop import DbObjectProp
from .source_file_reader import SourceFileReader
from .db_object_props_list_parser import DbObjectPropsListParser

MARKER_NAME_LINE = "NAME:"
MARKER_PROP_LINE = "PROPS:"

class DbSequenceParser:
    _mapping: DbMapping = None

    def __init__(self, mapping: DbMapping):
        self._mapping = mapping

    def parseFromFile(self, sourceFile:str) -> DbSequence:
        sourceFileReader = SourceFileReader()
        dbSequenceFileLines = sourceFileReader.readSourceLines(sourceFile)
        
        if DbSequenceParser.isValidDbSequenceFileMapping(dbSequenceFileLines):
            return self.parse(dbSequenceFileLines)
        else:
            return None

    @staticmethod
    def isValidDbSequenceFileMapping(dbSequenceFileLines: list[str]) -> bool:
        return len(dbSequenceFileLines) > 0 and dbSequenceFileLines[0] == DbSequence.getObjectType();

    def parse(self, sourceFileLines: list[str]) -> DbSequence:
        name: str = None
        props: list[DbObjectProp] = []

        for sourceFileLine in sourceFileLines:
            if sourceFileLine.startswith(MARKER_NAME_LINE):
                name = self._readName(sourceFileLine)
            elif sourceFileLine.startswith(MARKER_PROP_LINE):
                props = self._readProps(sourceFileLine)

        return DbSequence(name, props)

    def _readName(self, sourceFileLine: str) -> str:
        name = self._prepareNameLine(sourceFileLine)
        return self._mapping.expandString(name)

    def _prepareNameLine(self, sourceFileLine: str) -> str:
        return sourceFileLine.replace(MARKER_NAME_LINE, '').strip()

    def _readProps(self, sourceFileLine: str) -> list[DbObjectProp]:
        parser = DbObjectPropsListParser()
        propsListContents = self._preparePropsLine(sourceFileLine)
        return parser.parse(propsListContents)

    def _preparePropsLine(self, sourceFileLine: str) -> str:
        return sourceFileLine.replace(MARKER_PROP_LINE, '').strip()

