from ..helper.string import str_to_bool
from ..model.compiler_output_info import CompilerOutputInfo

class ConsoleOutputProviderOptions:
    _arguments: dict[str, str] = None

    def __init__(self, arguments: dict[str, str] = None) -> None:
        self._arguments = arguments or {}

    def showFunctions(self) -> bool:
        return str_to_bool(self._arguments.get('func', 'true'))

    def showSequences(self) -> bool:
        return str_to_bool(self._arguments.get('seq', 'true'))

    def showTables(self) -> bool:
        return str_to_bool(self._arguments.get('tbl', 'true'))

    def showTableIndexes(self) -> bool:
        return str_to_bool(self._arguments.get('tbl_index', 'true'))

    def showTableUniqueKeys(self) -> bool:
        return str_to_bool(self._arguments.get('tbl_unq', 'true'))