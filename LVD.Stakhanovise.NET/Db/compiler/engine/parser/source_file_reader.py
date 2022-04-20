from os.path import abspath
from os.path import exists

def _isProcessableFileLine(line: str) -> bool:
    return len(line) > 0 and not line.startswith('#')

class SourceFileReader:
    def readSourceLines(self, sourceFile: str) -> list[str]:
        absoluteSourceFilePath = self._determineAbsoluteSourceFilePath(sourceFile)
        if (exists(absoluteSourceFilePath) is False):
            raise FileNotFoundError("Source not found at path <" + absoluteSourceFilePath + ">")

        sourceFileHandle = open(absoluteSourceFilePath, 'r', encoding='utf_8_sig')
        sourceFileLines = sourceFileHandle.readlines()

        sourceFileLines = map(lambda line: line.strip(), sourceFileLines)
        sourceFileLines = filter(lambda line: _isProcessableFileLine(line), sourceFileLines)

        return list(sourceFileLines)

    def _determineAbsoluteSourceFilePath(self, sourceFile: str) -> str:
        return abspath(sourceFile)