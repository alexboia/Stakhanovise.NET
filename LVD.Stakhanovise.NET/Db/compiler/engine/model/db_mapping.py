from ..helper.string import sprintf

QUEUE_TABLE_NAME_TOKEN = "queue_table_name"
RESULTS_QUEUE_TABLE_NAME_TOKEN = "results_queue_table_name"
EXECUTION_TIME_STATS_TABLE_NAME_TOKEN = "execution_time_stats_table_name"
METRICS_TABLE_NAME_TOKEN = "metrics_table_name"
NEW_TASK_NOTIFICATION_CHANNEL_NAME_TOKEN = "new_task_notification_channel_name"
DEQUEUE_FUNCTION_NAME_TOKEN = "dequeue_function_name"

class DbMapping:
    _queueTableName: str = None
    _resultsQueueTableName: str = None
    _executionTimeStatsTableName: str = None
    _metricTableName: str = None
    _newTaskNotificationChannelNameToken: str = None
    _dequeueFunctionName: str = None
    _symbols: dict[str, str] = None

    def __init__(self, symbols: dict[str, str]):
        self._queueTableName = symbols.get(QUEUE_TABLE_NAME_TOKEN, "sk_tasks_queue_t")
        self._resultsQueueTableName = symbols.get(RESULTS_QUEUE_TABLE_NAME_TOKEN, "sk_task_results_t")
        self._executionTimeStatsTableName = symbols.get(EXECUTION_TIME_STATS_TABLE_NAME_TOKEN, "sk_task_execution_time_stats_t")
        self._metricTableName = symbols.get(METRICS_TABLE_NAME_TOKEN, "sk_metrics_t")
        self._newTaskNotificationChannelNameToken = symbols.get(NEW_TASK_NOTIFICATION_CHANNEL_NAME_TOKEN, "sk_task_queue_item_added")
        self._dequeueFunctionNam = symbols.get(DEQUEUE_FUNCTION_NAME_TOKEN, "sk_try_dequeue_task")
        self._symbols = symbols or {}

    @staticmethod
    def createFromInput(symbols: dict[str, str]):
        return DbMapping(symbols)

    @staticmethod
    def getAllValidTokenNames() -> list[str]:
        return [QUEUE_TABLE_NAME_TOKEN, 
                RESULTS_QUEUE_TABLE_NAME_TOKEN, 
                EXECUTION_TIME_STATS_TABLE_NAME_TOKEN, 
                METRICS_TABLE_NAME_TOKEN, 
                NEW_TASK_NOTIFICATION_CHANNEL_NAME_TOKEN, 
                DEQUEUE_FUNCTION_NAME_TOKEN]

    @staticmethod
    def isValidTokenName(tokenName: str) -> bool:
        validTokenNames = __class__.getAllValidTokenNames()
        return (tokenName in validTokenNames)

    def _replaceTokenByName(self, targetString: str, tokenName: str) -> str:
        token = self._createSearchToken(tokenName)
        value = self._resolveSymbol(tokenName)
        return targetString.replace(token, value)

    def _createSearchToken(self, tokenName: str) -> str:
        return sprintf("$%s$" % (tokenName))

    def _resolveSymbol(self, tokenName: str) -> str:
        return self._symbols.get(tokenName)

    def expandString(self, targetString: str) -> str:
        finalString = targetString
        replaceTokenNames = __class__.getAllValidTokenNames()
        
        for tokenName in replaceTokenNames:
            finalString = self._replaceTokenByName(finalString, tokenName)

        return finalString

    def getQueueTableName(self) -> str:
        return self._queueTableName

    def getResultsQueueTableName(self) -> str:
        return self._resultsQueueTableName

    def getExecutionTimeStatsTableName(self) -> str:
        return self._executionTimeStatsTableName

    def getMetricsTableName(self) -> str:
        return self._metricTableName

    def getNewTaskNotificationChannelName(self) -> str:
        return self._newTaskNotificationChannelNameToken

    def getDequeueFunctionName(self) -> str:
        return self._dequeueFunctionName

    def __str__(self) -> str:
        return self._symbols.__str__()
