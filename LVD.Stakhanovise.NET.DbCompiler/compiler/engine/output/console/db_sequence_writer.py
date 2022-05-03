from rich.console import Console
from ...model.db_sequence import DbSequence
from .db_object_writer import DbObjectWriter

class DbSequenceWriter(DbObjectWriter[DbSequence]):
    def __init__(self, console: Console) -> None:
        super().__init__(console)

    def write(self, dbSequence: DbSequence) -> None:
        if not isinstance(dbSequence, DbSequence):
            raise TypeError('Object does not represent a database sequence')

        self._writeObjectTitle("Sequence", dbSequence.getName())
        self._writeObjectProperties(dbSequence.getProperties())