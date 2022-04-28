from ..helper.string import str_to_bool

MODE_SINGLE = 'single'
MODE_CONSOLIDATED = 'consolidated'

BUILD_ACTION_NONE = 'None'
BUID_ACTION_CONTENT = 'Content'

PLACEHOLDER_DB_OBJECT_NAME = '$db_object$'

class SqlScriptOutputProviderOptions:
    _arguments: dict[str, str] = None

    def __init__(self, arguments: dict[str, str] = None) -> None:
        self._arguments = arguments or {}

    def getMode(self) -> str:
        return self._arguments.get('mode', MODE_SINGLE)

    def getBuildAction(self) -> str:
        return self._arguments.get('build_action', BUILD_ACTION_NONE)

    def getFileName(self) -> str:
        return self._arguments.get('file')

    def expandFileName(self, dbObjectName: str) -> str:
        fileName = self.getFileName()
        return fileName.replace(PLACEHOLDER_DB_OBJECT_NAME, dbObjectName)

    def getDestinationDirectory(self) -> str:
        return self._arguments.get('dir')
