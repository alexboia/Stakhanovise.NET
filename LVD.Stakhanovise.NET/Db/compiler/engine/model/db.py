from .makefile_info import MakefileInfo
from .db_mapping import DbMapping
from .db_sequence import DbSequence
from .db_function import DbFunction
from .db_table import DbTable
from .compiler_output_info import CompilerOutputInfo

class Db:
    _makefile: MakefileInfo = None
    _mapping: DbMapping = None
    _sequences: list[DbSequence] = None
    _tables: list[DbTable] = None
    _functions: list[DbFunction] = None

    def __init__(self, makefile: MakefileInfo, mapping: Dbmapping, sequences: list[DbSequence], tables: list[DbTable], functions: list[DbFunction]) -> None:
        self._makefile = makefile
        self._mapping = mapping
        self._sequences = sequences or []
        self._tables = tables or []
        self._functions = functions or []

    def getMakefileInfo(self) -> MakefileInfo:
        return self._makefile

    def getSequences(self) -> list[DbSequence]:
        return self._sequences

    def getTables(self) -> list[DbTable]:
        return self._tables

    def getFunctions(self) -> list[DbFunction]:
        return self._functions

    def getMapping(self) -> DbMapping:
        return self._mapping