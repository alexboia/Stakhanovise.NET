from os.path import abspath
from os.path import exists

from .source_file_reader import SourceFileReader
from .named_spec_with_named_args import NamedSpecWithNamedArgs
from .named_spec_with_named_args_parser import NamedSpecWithNamedArgsParser

from ..model.compiler_output_info import CompilerOutputInfo
from ..model.makefile_info import MakefileInfo

MARKER_MAP_LINE = 'MAP='
MARKER_DEFINITIONS_LINE = 'DEFINITIONS='
MARKER_OUTPUT_LINE = 'OUTPUT='

class MakefileParser:
    def parse(self, sourceFile: str) -> MakefileInfo:
        sourceFileReader = SourceFileReader()
        makefileLines = sourceFileReader.readSourceLines(sourceFile)

        return self._parseMakefile(makefileLines)

    def _determineAbsoluteMakefilePath(self, sourceFile: str) -> str:
        return abspath(sourceFile)

    def _parseMakefile(self, makefileLines: list[str]):
        makefileInfo = MakefileInfo()

        for makefileLine in makefileLines:
            self._parseMakefileLine(makefileLine, makefileInfo)

        return makefileInfo

    def _parseMakefileLine(self, makefileLine: str, makefileInfo: MakefileInfo):
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
        mappingFileName = makefileLine.replace(MARKER_MAP_LINE, '').strip()
        if (len(mappingFileName) > 0):
            return mappingFileName
        else:
            return "sk_mapping.dbmap"

    def _readDefinitionsFilesGlob(self, makefileLine: str) -> str:
        definitionFilesGlob = makefileLine.replace(MARKER_DEFINITIONS_LINE, '').strip()
        if (len(definitionFilesGlob) > 0):
            return definitionFilesGlob
        else:
            return "*.dbdef"

    def _readCompilerOutputInfo(self, makefileLine: str) -> CompilerOutputInfo:
        compilerOutputInfo: CompilerOutputInfo = None
        compilerOutputInfoContents: str = makefileLine.replace(MARKER_OUTPUT_LINE, '').strip()
        
        if (len(compilerOutputInfoContents) > 0):
            compilerOutputParser = NamedSpecWithNamedArgsParser()
            compilerOutputArgs = compilerOutputParser.parse(compilerOutputInfoContents)

            compilerOutputInfo = CompilerOutputInfo(compilerOutputArgs.getName(), 
                compilerOutputArgs.getArgs())

        return compilerOutputInfo
