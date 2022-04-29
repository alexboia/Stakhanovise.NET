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

    def generateAsSingle(self) -> bool:
        return self.getMode() == MODE_SINGLE

    def generateAsConsolidated(self) -> bool:
        return self.getMode() == MODE_CONSOLIDATED

    def getTargetProjectName(self) -> str:
        return self._arguments.get('proj', 'LVD.Stakhanovise.NET')

    def getCopyOutput(self) -> str:
        return self._arguments.get('copy_output')

    def getBuildAction(self) -> str:
        return self._arguments.get('build_action', BUILD_ACTION_NONE)

    def getItemGroupLabel(self) -> str:
        return self._arguments.get('item_group')

    def getFileName(self) -> str:
        fileName = self._arguments.get('file')
        if fileName is None:
            fileName = 'sk_db.sql' if self.generateAsSingle() else '$db_object$.sql'
        return fileName

    def expandFileName(self, dbObjectName: str) -> str:
        fileName = self.getFileName()
        return fileName.replace(PLACEHOLDER_DB_OBJECT_NAME, dbObjectName)

    def getDestinationDirectory(self) -> str:
        return self._arguments.get('dir')
