from abc import ABC, abstractmethod
from typing import Generic, TypeVar

from ...helper.string import sprintf
from ...helper.string_builder import StringBuilder
from ...model.db_object import DbObject
from ...model.db_object_prop import DbObjectProp

TDbObject = TypeVar('TDbObject')

class MarkdownDbObjectWriter(ABC, Generic[TDbObject]):
    _mdStringBuilder: StringBuilder

    def __init__(self, mdStringBuilder: StringBuilder) -> None:
        super().__init__()
        self._mdStringBuilder = mdStringBuilder

    def _writeObjectHeader(self, dbObject: DbObject, defaultTitlePrefix: str = '') -> None:
        titlePrefix = dbObject.getMetaTitle()
        if titlePrefix is None:
            titlePrefix = defaultTitlePrefix

        self._writeObjectTitle(titlePrefix, dbObject.getName(), 2)

        description = dbObject.getMetaDescription()
        if description is not None:
            self._writeObjectDescription(description)

    def _writeObjectTitle(self, prefix: str, name: str, level: int = 1) -> None:
        marker = '#' * level
        title = sprintf('%s %s - `%s`' % (marker, prefix, name))
        self._writeLines(title)
        self._writeSpacer()

    def _writeObjectDescription(self, description: str) -> None:
        self._writeLines(description)
        self._writeSpacer()

    def _writeMarkdownTable(self, columns: list[str], rows: list[list[str]]) -> None:
        headerString = sprintf('| %s |' % (' | '.join(columns)))
        headerSeparatorString = sprintf('| %s |' % (' | '.join(['---'] * len(columns))))

        self._writeLines(headerString, headerSeparatorString)

        for row in rows:
            rowString = sprintf('| %s |' % (' | '.join(row)))
            self._writeLines(rowString)

    def _writeMarkdownCodeBlock(self, codeContents: str, language: str = None) -> None:
        codeStart = '```'
        codeEnd = codeStart

        if language is not None and len(language) > 0:
            codeStart += language

        self._writeLines(codeStart, codeContents, codeEnd)

    def _writeObjectProperties(self, properties: dict[str, DbObjectProp]) -> None:
        columns = ['Name', 'Value']
        rows = []

        for propKey in properties:
            formattedPropName = sprintf('`%s`' % (properties[propKey].name))
            formattedPropValue = sprintf('`%s`' % (properties[propKey].value))
            rows.append([formattedPropName, formattedPropValue])

        self._writeMarkdownTable(columns, rows)
        self._writeSpacer()

    def _writeLines(self, *lines) -> None:
        for line in lines:
            self._mdStringBuilder.appendLine(line)

    def _writeSpacer(self) -> None:
        self._mdStringBuilder.appendEmptyLine()

    @abstractmethod
    def write(self, dbObject: TDbObject) -> None:
        pass