from os.path import abspath
from os.path import exists

def _removeLineEnding(line: str) -> str:
    return line.rstrip()

def _filterLine(line: str) -> str:
    return line.strip()

def _isProcessableFileLine(line: str) -> bool:
    return len(line) > 0 and not line.startswith('#')

class SourceFileReader:
    _verbatimFromMarker: str = None
    _verbatimToMarker: str = None

    def __init__(self, vermatimFromMarker: str = None, verbatimToMarker: str = None):
        self._verbatimFromMarker = vermatimFromMarker
        self._verbatimToMarker = verbatimToMarker

    def readSourceLines(self, sourceFile: str) -> list[str]:
        absoluteSourceFilePath = self._determineAbsoluteSourceFilePath(sourceFile)
        if (exists(absoluteSourceFilePath) is False):
            raise FileNotFoundError("Source not found at path <" + absoluteSourceFilePath + ">")

        sourceFileHandle = open(absoluteSourceFilePath, 'r', encoding='utf_8_sig')
        sourceFileLines = sourceFileHandle.readlines()
        sourceFileHandle.close()

        sourceFileLines = self._preprocessLines(sourceFileLines)
        sourceFileLines = filter(lambda line: _isProcessableFileLine(line), sourceFileLines)

        return list(sourceFileLines)

    def _preprocessLines(self, sourceFileLines: list[str]) -> list[str]:
        shouldFilter: bool = True
        filteredSourceFileLines: list[str] = []

        for sourceFileLine in sourceFileLines:
            shouldFilterThisLine = shouldFilter

            if self._isVerbatimStart(sourceFileLine):
                shouldFilterThisLine = True
                shouldFilter = False
            elif self._isVerbatimEnd(sourceFileLine):
                shouldFilterThisLine = True
                shouldFilter = True

            if shouldFilterThisLine:
                filteredSourceFileLine = _filterLine(sourceFileLine)
            else:
                filteredSourceFileLine = _removeLineEnding(sourceFileLine)

            filteredSourceFileLines.append(filteredSourceFileLine)

        return filteredSourceFileLines

    def _isVerbatimStart(self, sourceFileLine: str) -> bool:
        return self._verbatimFromMarker is not None and self._verbatimFromMarker == sourceFileLine.strip() 

    def _isVerbatimEnd(self, sourceFileLine: str) -> bool:
        return self._verbatimToMarker is not None and self._verbatimToMarker == sourceFileLine.strip() 

    def _determineAbsoluteSourceFilePath(self, sourceFile: str) -> str:
        return abspath(sourceFile)