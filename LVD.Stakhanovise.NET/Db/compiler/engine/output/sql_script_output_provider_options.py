from ..model.project_names import MAIN_PROJECT_NAME
from ..model.build_actions import BUILD_ACTION_NONE, BUID_ACTION_CONTENT

MODE_SINGLE = 'single'
MODE_CONSOLIDATED = 'consolidated'

CONSOLIDATED_DB_FILE_NAME = 'sk_db.sql'
PLACEHOLDER_DB_OBJECT_NAME = '$db_object$'
SINGLE_DB_OBJECT_FILE_NAME = PLACEHOLDER_DB_OBJECT_NAME + '.sql'

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
        return self._arguments.get('proj', MAIN_PROJECT_NAME)

    def getCopyOutput(self) -> str:
        return self._arguments.get('copy_output')

    def getBuildAction(self) -> str:
        return self._arguments.get('build_action', BUILD_ACTION_NONE)

    def getItemGroupLabel(self) -> str:
        return self._arguments.get('item_group')

    def getFileName(self) -> str:
        fileName = self._arguments.get('file')
        if fileName is None:
            fileName = (CONSOLIDATED_DB_FILE_NAME 
                if self.generateAsConsolidated() 
                else SINGLE_DB_OBJECT_FILE_NAME)
        return fileName

    def expandFileName(self, dbObjectName: str) -> str:
        fileName = self.getFileName()
        return fileName.replace(PLACEHOLDER_DB_OBJECT_NAME, dbObjectName)

    def getDestinationDirectory(self) -> str:
        return self._arguments.get('dir')
