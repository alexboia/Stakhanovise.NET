from .source_file_reader import SourceFileReader
from ..model.db_mapping import DbMapping

MARKER_MAP_SYMBOL_LINE = 'MAP:'

class DbMappingParser:
    def parse(self, sourceFile: str)  -> DbMapping:
        sourceFileReader = SourceFileReader()
        mappingFileLines = sourceFileReader.readSourceLines(sourceFile)

        symbols = self._parseMappingFile(mappingFileLines)
        return DbMapping.createFromInput(symbols)

    def _parseMappingFile(self, mappingFileLines: list[str]) -> dict:
        symbols: dict = {}

        for mappingLine in mappingFileLines:
            symbolParts = self._getSymbolParts(mappingLine)
            if (len(symbolParts) == 2):
                symbolName = symbolParts[0]
                if (DbMapping.isValidTokenName(symbolName)):
                    symbols[symbolName] = symbolParts[1]

        return symbols

    def _getSymbolParts(self, mappingLine: str) -> list[str]:
        rawSymbolDefinition = self._getRawSymbolDefinition(mappingLine)
        return rawSymbolDefinition.split('=', 1)

    def _getRawSymbolDefinition(self, mappingLine: str) -> str:
        return mappingLine.replace(MARKER_MAP_SYMBOL_LINE, '').strip()