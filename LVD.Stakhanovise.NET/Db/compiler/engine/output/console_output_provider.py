from rich.console import Console

from ..model.compiler_output_info import CompilerOutputInfo
from ..model.db_function import DbFunction
from ..model.db_sequence import DbSequence
from ..model.db_table import DbTable

from .output_provider import OutputProvider
from .console_output_provider_options import ConsoleOutputProviderOptions
from .console.db_sequence_writer import DbSequenceWriter
from .console.db_table_writer import DbTableWriter
from .console.db_function_writer import DbFunctionWriter

class ConsoleOutputProvider(OutputProvider):
    _console: Console = None
    _options: ConsoleOutputProviderOptions = None

    def __init__(self, options: ConsoleOutputProviderOptions) -> None:
        self._options = options or ConsoleOutputProviderOptions()
        self._console = Console()

    def writeTable(self, dbTable: DbTable) -> None:
        if self._options.showTables():
            tableWriter = DbTableWriter(self._console, self._options.showTableIndexes(), self._options.showTableUniqueKeys())
            tableWriter.write(dbTable)

    def writeSequence(self, dbSequence: DbSequence) -> None:
        if self._options.showSequences():
            sequenceWriter = DbSequenceWriter(self._console)
            sequenceWriter.write(dbSequence)

    def writeFunction(self, dbFunction: DbFunction) -> None:
        if self._options.showFunctions():
            functionWriter = DbFunctionWriter(self._console)
            functionWriter.write(dbFunction)