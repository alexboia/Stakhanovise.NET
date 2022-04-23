class NamedSpecWithNamedArgs:
    _name: str = None
    _args: dict[str, str] = {}

    def __init__(self, name: str, args: dict[str, str]):
        self._name = name
        self._args = args or {}

    def getName(self) -> str:
        return self._name

    def getArgs(self) -> dict[str, str]:
        return self._args

    def hasArgs(self) -> bool:
        return len(self.getArgs().keys()) > 0