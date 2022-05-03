from ..model.db_connection_info import DbConnectionInfo

class DbCreateOutputProviderOptions:
    _arguments: dict[str, str] = None

    def __init__(self, arguments: dict[str, str] = None) -> None:
        self._arguments = arguments or {}

    def getConnectionInfo(self) -> DbConnectionInfo:
        return DbConnectionInfo.parse(self.getConnectionString())

    def getConnectionString(self) -> str:
        return self._arguments.get('connection_string')

    def getIfExists(self) -> str:
        return self._arguments.get('if_exists', 'drop')

    def shouldDropDatabaseIfExists(self) -> bool:
        return self.getIfExists() == 'drop'