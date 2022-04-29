import os
from ..helper.string_builder import StringBuilder
from ..helper.path_resolver import PathResolver
from ..model.db_function import DbFunction
from ..model.db_sequence import DbSequence
from ..model.db_table import DbTable

from .output_provider import OutputProvider
from .sql_script_output_provider_options import SqlScriptOutputProviderOptions

from .sql_script.sql_db_table_writer import SqlDbTableWriter
from .sql_script.sql_db_sequence_writer import SqlDbSequenceWriter
from .sql_script.sql_db_function_writer import SqlDbFunctionWriter

class SqlScriptOutputProvider(OutputProvider):
    _buffers: dict[str, StringBuilder] = None
    _options: SqlScriptOutputProviderOptions = None
    _pathResolver: PathResolver = None

    def __init__(self, options: SqlScriptOutputProviderOptions, pathResolver: PathResolver) -> None:
        self._options = options
        self._pathResolver = pathResolver
        self._buffers = {}

    def _getObjectBuffer(self, objectName: str) -> StringBuilder:
        objectBuffer = self._buffers.get(objectName, None)
        if objectBuffer is None:
            objectBuffer = StringBuilder()
            self._buffers[objectName] = objectBuffer

        return objectBuffer

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
        globalBuffer = None 
        if self._options.generateAsConsolidated():
            globalBuffer = StringBuilder()

        for objectName in self._buffers.keys():
            objectBuffer = self._buffers[objectName]
            if self._options.generateAsSingle():
                fileName = self._options.expandFileName(objectName)
                self._writeObjectBufferToFile(objectBuffer, fileName)
            else:
                globalBuffer.append(objectBuffer.toString())

        if self._options.generateAsConsolidated():
            self._writeObjectBufferToFile(globalBuffer, self._options.getFileName())

    def _writeObjectBufferToFile(self, objectBuffer: StringBuilder, fileName: str) -> None:
        filePath = self._determineFilePath(fileName)
        filePointer = open(filePath, 'w', encoding='utf-8')
        filePointer.write(objectBuffer.toString())
        filePointer.close()

    def _determineFilePath(self, fileName: str) -> str:
        directory = self._getDestinationDirectory()
        return os.path.join(directory, fileName)

    def _getDestinationDirectory(self) -> str:
        return self._pathResolver.resolveDirectory(self._options.getDestinationDirectory())