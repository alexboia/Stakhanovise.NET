class NamedSpecWithArgs:
    _name: str = None
    _args: list[str] = None

    def __init__(self, name: str, args: list[str]):
        self._name = name
        self._args = args or []

    def getName(self) -> str:
        return self._name

    def getArgs(self) -> list[str]:
        return self._args

    def hasArgs(self) -> bool:
        return len(self.getArgs()) > 0