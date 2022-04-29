class DbConnectionInfo:
    host: str = None
    port: int = None
    user: str = None
    password: str = None
    dbName: str = None

    def __init__(self, args: dict[str, str]) -> None:
        self.host = args.get('host', 'localhost')
        self.port = args.get('port', 5432)
        self.user = args.get('user', 'postgres')
        self.password = args.get('password', 'postgres')
        self.dbName = args.get('database', '')

    @staticmethod
    def parse(connectionString: str):
        if connectionString is None or len(connectionString) == 0:
            return None

        args = {}
        rawArgs = connectionString.split(',')

        for rawArg in rawArgs:
            rawArg = rawArg.strip()
            if len(rawArg) == 0:
                continue

            rawArgParts = rawArg.split(':', 2)
            if len(rawArgParts) != 2:
                continue

            args[rawArgParts[0]] = rawArgParts[1]

        return DbConnectionInfo(args)
