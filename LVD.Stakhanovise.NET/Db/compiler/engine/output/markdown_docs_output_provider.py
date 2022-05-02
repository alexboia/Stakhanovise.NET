from ..helper.string_builder import StringBuilder

from ..model.db_function import DbFunction
from ..model.db_sequence import DbSequence
from ..model.db_table import DbTable

from .output_provider import OutputProvider
from .markdown_docs_output_provider_options import MarkdownDocsOutputProviderOptions

class MarkdownDocsOutputProvider(OutputProvider):
    _options: MarkdownDocsOutputProviderOptions = None

    def __init__(self, options: MarkdownDocsOutputProviderOptions) -> None:
        self._options = options

    def writeTable(self, dbTable: DbTable) -> None:
        pass

    def writeSequence(self, dbSequence: DbSequence) -> None:
        pass

    def writeFunction(self, dbFunction: DbFunction) -> None:
        pass

    def commit(self) -> None:
        pass