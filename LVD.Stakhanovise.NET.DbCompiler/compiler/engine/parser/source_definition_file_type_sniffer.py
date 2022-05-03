from os.path import abspath
from os.path import exists

class SourceDefinitionFileTypeSniffer:
    def readType(self, sourceFile: str) -> str:
        absoluteSourceFilePath = self._determineAbsoluteSourceFilePath(sourceFile)
        if (exists(absoluteSourceFilePath) is False):
            raise FileNotFoundError("Source not found at path <" + absoluteSourceFilePath + ">")

        peekFileHandle = open(absoluteSourceFilePath, 'r', encoding='utf_8_sig')
        typeLine = peekFileHandle.readline()
        peekFileHandle.close()

        if typeLine:
            return typeLine.strip()
        else:
            return None

    def _determineAbsoluteSourceFilePath(self, sourceFile: str) -> str:
        return abspath(sourceFile)