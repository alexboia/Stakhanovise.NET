from abc import ABC, abstractmethod
from typing import Generic, TypeVar

from ...helper.string import sprintf
from ...helper.string_builder import StringBuilder
from ...model.db_object_prop import DbObjectProp

TDbObject = TypeVar('TDbObject')

class MarkdownDbObjectWriter(ABC, Generic[TDbObject]):
    _mdStringBuilder: StringBuilder

    def __init__(self, mdStringBuilder: StringBuilder) -> None:
        super().__init__()
        self._mdStringBuilder = mdStringBuilder

    def _writeMarkdownTitle(self, prefix: str, name: str, level: int = 1) -> None:
        marker = '#' * level
        title = sprintf('%s %s - %s' % (marker, prefix, name))
        self._mdStringBuilder.appendLine(title)
        self._mdStringBuilder.appendEmptyLine()

    def _writeMarkdownTable(self, columns: list[str], rows: list[list[str]]) -> None:
        headerString = sprintf('| %s |' % (' | '.join(columns)))
        self._mdStringBuilder.appendLine(headerString)

        headerSeparatorString = sprintf('| %s |' % (' | '.join(['---'] * len(columns))))
        self._mdStringBuilder.appendLine(headerSeparatorString)

        for row in rows:
            rowString = sprintf('| %s |' % (' | '.join(row)))
            self._mdStringBuilder.appendLine(rowString)

    def _writeMarkdownCodeBlock(self, codeContents: str, language: str = None) -> None:
        codeStart = '```'
        codeEnd = codeStart

        if language is not None and len(language) > 0:
            codeStart += language

        self._mdStringBuilder.appendLine(codeStart)
        self._mdStringBuilder.appendLine(codeContents)
        self._mdStringBuilder.appendLine(codeEnd)

    def _writeObjectProperties(self, properties: dict[str, DbObjectProp]) -> None:
        columns = ['Name', 'Value']
        rows = []

        for propKey in properties:
            rows.append([properties[propKey].name, properties[propKey].value])

        self._writeMarkdownTable(columns, rows)

    @abstractmethod
    def write(self, dbObject: TDbObject) -> None:
        pass