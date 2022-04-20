from ..helper.string import sprintf
from .compiler_output_info import CompilerOutputInfo

class MakefileInfo:
    _mappingFileName: str = None
    _definitionFilesGlob: str = None
    _outputs: list[CompilerOutputInfo] = []

    def setMappingFileName(self, mappingFileName: str):
        self._mappingFileName = mappingFileName

    def getMappingFileName(self) -> str:
        return self._mappingFileName

    def setDefinitionFilesGlob(self, definitionFilesGlob: str):
        self._definitionFilesGlob = definitionFilesGlob

    def getDefinitionFilesGlob(self) -> str:
        return self._definitionFilesGlob

    def addOutput(self, output: CompilerOutputInfo):
        self._outputs.append(output)

    def getOutputs(self) -> list[CompilerOutputInfo]:
        return self._outputs

    def __str__(self) -> str:
        return sprintf("{mappingFileName: %s, definitionFilesGlob: %s, outputs: %s}" % (self._mappingFileName, self._definitionFilesGlob, self._outputs))