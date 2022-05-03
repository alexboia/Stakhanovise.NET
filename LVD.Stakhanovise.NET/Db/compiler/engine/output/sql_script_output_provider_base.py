from ..helper.string_builder import StringBuilder

from ..model.db_function import DbFunction
from ..model.db_sequence import DbSequence
from ..model.db_table import DbTable
from ..model.db_mapping import DbMapping

from .output_provider import OutputProvider

from .sql_script.sql_db_table_writer import SqlDbTableWriter
from .sql_script.sql_db_sequence_writer import SqlDbSequenceWriter
from .sql_script.sql_db_function_writer import SqlDbFunctionWriter

class SqlScriptOutputProviderBase(OutputProvider):
    _buffers: dict[str, StringBuilder] = None

    def __init__(self) -> None:
        super().__init__()
        self._buffers = {}

    def _getObjectBuffer(self, objectName: str) -> StringBuilder:
        objectBuffer = self._buffers.get(objectName, None)
        if objectBuffer is None:
            objectBuffer = StringBuilder()
            self._buffers[objectName] = objectBuffer

        return objectBuffer

    def writeMapping(self, dbMapping: DbMapping) -> None:
        pass

    def writeTable(self, dbTable: DbTable) -> None:
        objectBuffer = self._getObjectBuffer(dbTable.getName())
        writer = SqlDbTableWriter(objectBuffer)
        writer.write(dbTable)

    def writeSequence(self, dbSequence: DbSequence) -> None:
        objectBuffer = self._getObjectBuffer(dbSequence.getName())
        writer = SqlDbSequenceWriter(objectBuffer)
        writer.write(dbSequence)

    def writeFunction(self, dbFunction: DbFunction) -> None:
        objectBuffer = self._getObjectBuffer(dbFunction.getName())
        writer = SqlDbFunctionWriter(objectBuffer)
        writer.write(dbFunction)

    def commit(self) -> None:
        pass