from .makefile_info import MakefileInfo
from .db_mapping import DbMapping
from .db_object import DbObject
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

    def __init__(self, makefile: MakefileInfo, mapping: DbMapping, objects: list[DbObject]) -> None:
        self._makefile = makefile
        self._mapping = mapping

        self._sequences = []
        self._tables = []
        self._functions = []

        for obj in objects:
            objType = obj.getType()
            if DbTable.getObjectType() == objType:
                self._tables.append(obj)
            elif DbFunction.getObjectType() == objType:
                self._functions.append(obj)
            elif DbSequence.getObjectType() == objType:
                self._sequences.append(obj)

    def getMakefileInfo(self) -> MakefileInfo:
        return self._makefile

    def getOutputs(self) -> list[CompilerOutputInfo]:
        return self._makefile.getOutputs()

    def getSequences(self) -> list[DbSequence]:
        return self._sequences

    def getTables(self) -> list[DbTable]:
        return self._tables

    def getFunctions(self) -> list[DbFunction]:
        return self._functions

    def getMapping(self) -> DbMapping:
        return self._mapping