from ...helper.string_builder import StringBuilder
from ...model.db_sequence import DbSequence
from .markdown_db_object_writer import MarkdownDbObjectWriter

class MarkdownDbSequenceWriter(MarkdownDbObjectWriter[DbSequence]):
    def __init__(self, mdStringBuilder: StringBuilder) -> None:
        super().__init__(mdStringBuilder)

    def write(self, dbSequence: DbSequence) -> None:
        self._writeMarkdownTitle('Sequence', dbSequence.getName(), 2)
        self._writeObjectProperties(dbSequence.getProperties())
        self._mdStringBuilder.appendEmptyLine()