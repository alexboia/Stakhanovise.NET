from abc import ABC, abstractmethod
from ..model.db import Db
from ..model.db_mapping import DbMapping
from ..model.db_function import DbFunction
from ..model.db_sequence import DbSequence
from ..model.db_table import DbTable

class OutputProvider(ABC):
    def export(self, db: Db) -> None:
        self.writeMapping(db.getMapping())

        for dbSequence in db.getSequences():
            self.writeSequence(dbSequence)
        
        for dbTable in db.getTables():
            self.writeTable(dbTable)

        for dbFunction in db.getFunctions():
            self.writeFunction(dbFunction)

        self.commit()

    @abstractmethod
    def writeMapping(self, dbMapping: DbMapping) -> None:
        pass

    @abstractmethod
    def writeTable(self, dbTable: DbTable) -> None:
        pass

    @abstractmethod
    def writeSequence(self, dbSequence: DbSequence) -> None:
        pass

    @abstractmethod
    def writeFunction(self, dbFunction: DbFunction) -> None:
        pass

    @abstractmethod
    def commit(self) -> None:
        pass