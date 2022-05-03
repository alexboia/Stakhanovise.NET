from os.path import abspath
from os.path import exists

from .source_file_reader import SourceFileReader
from .support.named_spec_with_named_args import NamedSpecWithNamedArgs
from .support.named_spec_with_named_args_parser import NamedSpecWithNamedArgsParser

from ..model.compiler_output_info import CompilerOutputInfo
from ..model.makefile_info import MakefileInfo

MARKER_MAP_LINE = 'MAP='
MARKER_DEFINITIONS_LINE = 'DEFINITIONS='
MARKER_OUTPUT_LINE = 'OUTPUT='

class MakefileParser:
    def parse(self, sourceFile: str) -> MakefileInfo:
        sourceFileReader = SourceFileReader()
        makefileLines = sourceFileReader.readSourceLines(sourceFile)
        if not makefileLines:
            return None

        return self._parseMakefile(makefileLines)

    def _determineAbsoluteMakefilePath(self, sourceFile: str) -> str:
        return abspath(sourceFile)

    def _parseMakefile(self, makefileLines: list[str]) -> str:
        makefileInfo = MakefileInfo()

        for makefileLine in makefileLines:
            self._parseMakefileLine(makefileLine, makefileInfo)

        return makefileInfo

    def _parseMakefileLine(self, makefileLine: str, makefileInfo: MakefileInfo) -> None:
        if (makefileLine.startswith(MARKER_MAP_LINE)):
            mappingFileName = self._readMappingFileName(makefileLine)
            makefileInfo.setMappingFileName(mappingFileName)

        elif (makefileLine.startswith(MARKER_DEFINITIONS_LINE)):
            definitionFilesGlob = self._readDefinitionsFilesGlob(makefileLine)
            makefileInfo.setDefinitionFilesGlob(definitionFilesGlob)

        elif makefileLine.startswith(MARKER_OUTPUT_LINE):
            compilerOutpuInfo = self._readCompilerOutputInfo(makefileLine)
            if (compilerOutpuInfo is not None):
                makefileInfo.addOutput(compilerOutpuInfo)

    def _readMappingFileName(self, makefileLine: str) -> str:
        mappingFileName = self._prepareMappingFileNameLine(makefileLine)
        if (len(mappingFileName) > 0):
            return mappingFileName
        else:
            return "sk_mapping.dbmap"

    def _prepareMappingFileNameLine(self, makefileLine: str) -> str:
        return makefileLine.replace(MARKER_MAP_LINE, '').strip()

    def _readDefinitionsFilesGlob(self, makefileLine: str) -> str:
        definitionFilesGlob = self._prepareDefinitionFilesGlobLine(makefileLine)
        if (len(definitionFilesGlob) > 0):
            return definitionFilesGlob
        else:
            return "*.dbdef"

    def _prepareDefinitionFilesGlobLine(self, makefileLine: str) -> str:
        return makefileLine.replace(MARKER_DEFINITIONS_LINE, '').strip()

    def _readCompilerOutputInfo(self, makefileLine: str) -> CompilerOutputInfo:
        compilerOutputInfoContents = self._prepareCompilerOutputInfoLine(makefileLine)
        
        if (len(compilerOutputInfoContents) > 0):
            compilerOutputInfo = self._parseCompilerOutputInfo(compilerOutputInfoContents)
        else:
            compilerOutputInfo = None

        return compilerOutputInfo

    def _prepareCompilerOutputInfoLine(self, makefileLine: str) -> str:
        return makefileLine.replace(MARKER_OUTPUT_LINE, '').strip()

    def _parseCompilerOutputInfo(self, compilerOutputInfoContents: str) -> CompilerOutputInfo:
        compilerOutputParser = NamedSpecWithNamedArgsParser()
        compilerOutputSpec = compilerOutputParser.parse(compilerOutputInfoContents)

        name = compilerOutputSpec.getName()
        args = compilerOutputSpec.getArgs()

        compilerOutputInfo = CompilerOutputInfo(name, args)
        return compilerOutputInfo