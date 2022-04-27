from rich.console import Console

from ..model.compiler_output_info import CompilerOutputInfo
from ..model.db_function import DbFunction
from ..model.db_sequence import DbSequence
from ..model.db_table import DbTable

from .output_provider import OutputProvider
from .console.db_sequence_writer import DbSequenceWriter
from .console.db_table_writer import DbTableWriter
from .console.db_function_writer import DbFunctionWriter

class ConsoleOutputProvider(OutputProvider):
    _console: Console = None

    def __init__(self, outputInfo: CompilerOutputInfo) -> None:
        super().__init__(outputInfo)
        self._console = Console()

    def writeTable(self, dbTable: DbTable) -> None:
        tableWriter = DbTableWriter(self._console)
        tableWriter.write(dbTable)

    def writeSequence(self, dbSequence: DbSequence) -> None:
        sequenceWriter = DbSequenceWriter(self._console)
        sequenceWriter.write(dbSequence)

    def writeFunction(self, dbFunction: DbFunction) -> None:
        functionWriter = DbFunctionWriter(self._console)
        functionWriter.write(dbFunction)