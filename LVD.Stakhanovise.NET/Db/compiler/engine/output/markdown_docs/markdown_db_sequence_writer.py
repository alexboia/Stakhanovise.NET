from ...helper.string_builder import StringBuilder
from ...model.db_sequence import DbSequence
from .markdown_db_object_writer import MarkdownDbObjectWriter

class MarkdownDbSequenceWriter(MarkdownDbObjectWriter[DbSequence]):
    def __init__(self, mdStringBuilder: StringBuilder) -> None:
        super().__init__(mdStringBuilder)

    def write(self, dbSequence: DbSequence) -> None:
        self._writeObjectHeader(dbSequence, defaultTitlePrefix = 'Sequence')
        self._writeObjectProperties(dbSequence.getNonMetaProperties())