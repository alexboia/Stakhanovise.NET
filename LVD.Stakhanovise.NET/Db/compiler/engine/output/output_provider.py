from ..model.compiler_output_info import CompilerOutputInfo
from ..model.db_function import DbFunction
from ..model.db_sequence import DbSequence
from ..model.db_table import DbTable

from abc import abstractmethod

class OutputProvider:
    _outputInfo: CompilerOutputInfo = None

    def __init__(self, outputInfo: CompilerOutputInfo) -> None:
        self._outputInfo = outputInfo

    @abstractmethod
    def writeTable(self, dbTable: DbTable) -> None:
        pass

    @abstractmethod
    def writeSequence(self, dbSequence: DbSequence) -> None:
        pass

    @abstractmethod
    def writeFunction(self, dbFunction: DbFunction) -> None:
        pass