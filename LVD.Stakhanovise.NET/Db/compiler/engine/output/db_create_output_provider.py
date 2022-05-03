from psycopg2 import connect
from psycopg2.extensions import ISOLATION_LEVEL_AUTOCOMMIT

from ..helper.string_builder import StringBuilder
from ..model.db_connection_info import DbConnectionInfo

from .sql_script_output_provider_base import SqlScriptOutputProviderBase
from .db_create_output_provider_options import DbCreateOutputProviderOptions

class DbCreateOutputProvider(SqlScriptOutputProviderBase):
    _options: DbCreateOutputProviderOptions = None

    def __init__(self, options: DbCreateOutputProviderOptions) -> None:
        super().__init__()
        self._options = options
        self._buffers = {}

    def commit(self) -> None:
        connectionInfo = self._options.getConnectionInfo()
        self._ensureDbExists(connectionInfo)
        self._createDbObjectsFromBuffers(connectionInfo)
        self._buffers = {}

    def _ensureDbExists(self, connectionInfo: DbConnectionInfo):
        conn = None
        try:
            conn = self._connectToServerWithoutDb(connectionInfo)
            dbExists = self._databaseExists(conn, connectionInfo.dbName)

            if dbExists and self._shouldDropDatabaseIfExists():
                self._dropDatabase(conn, connectionInfo.dbName)
                dbExists = False

            if not dbExists:
                self._createDatabase(conn, connectionInfo.dbName)
        finally:
            if conn is not None:
                conn.close()

    def _connectToServerWithoutDb(self, connectionInfo: DbConnectionInfo):
        dsn = self._getConnectionDsnWithoutDb(connectionInfo)
        conn = connect(**dsn)
        conn.set_isolation_level(ISOLATION_LEVEL_AUTOCOMMIT)
        return conn

    def _getConnectionDsnWithoutDb(self, connectionInfo: DbConnectionInfo) -> dict[str, str]:
        return { 
            'host': connectionInfo.host, 
            'port': connectionInfo.port, 
            'user': connectionInfo.user, 
            'password': connectionInfo.password 
        }

    def _shouldDropDatabaseIfExists(self) -> bool:
        return self._options.shouldDropDatabaseIfExists()

    def _databaseExists(self, conn, dbName: str) -> bool:
        cursor = conn.cursor()
        cursor.execute("SELECT 1 as db_exists FROM pg_database WHERE datname='" + dbName + "'")

        dbExistsResult = cursor.fetchone()
        dbExists = dbExistsResult is not None and dbExistsResult[0] == 1
        cursor.close()

        return dbExists

    def _dropDatabase(self, conn, dbName: str) -> None:
        cursor = conn.cursor()
        cursor.execute('DROP DATABASE IF EXISTS ' + dbName)
        cursor.close()

    def _createDatabase(self, conn, dbName: str) -> None:
        cursor = conn.cursor()
        cursor.execute('CREATE DATABASE ' + dbName)
        cursor.close()

    def _createDbObjectsFromBuffers(self, connectionInfo: DbConnectionInfo) -> None: 
        conn = None
        try:
            conn = self._connectToServer(connectionInfo)
            for objectName in self._buffers.keys():
                objectBuffer = self._buffers[objectName]
                executeSql = objectBuffer.toString()

                cursor = conn.cursor()
                cursor.execute(executeSql)
                cursor.close()

                objectBuffer.close()
        finally:
            if conn is not None:
                conn.close()

    def _connectToServer(self, connectionInfo: DbConnectionInfo):
        dsn = self._getConnectionDsn(connectionInfo)
        conn = connect(**dsn)
        conn.set_isolation_level(ISOLATION_LEVEL_AUTOCOMMIT)
        return conn

    def _getConnectionDsn(self, connectionInfo: DbConnectionInfo) -> dict[str, str]:
        dsn = self._getConnectionDsnWithoutDb(connectionInfo)
        dsn['database'] = connectionInfo.dbName
        return dsn