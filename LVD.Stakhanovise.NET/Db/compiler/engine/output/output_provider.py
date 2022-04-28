from abc import abstractmethod
from ..model.db_function import DbFunction
from ..model.db_sequence import DbSequence
from ..model.db_table import DbTable

class OutputProvider:
    @abstractmethod
    def writeTable(self, dbTable: DbTable) -> None:
        pass

    @abstractmethod
    def writeSequence(self, dbSequence: DbSequence) -> None:
        pass

    @abstractmethod
    def writeFunction(self, dbFunction: DbFunction) -> None:
        pass