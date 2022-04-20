class NamedSpecWithArgs:
    _name: str = None
    _args: list[str] = []

    def __init__(self, name: str, args: list[str]):
        self._name = name
        self._args = args

    def getName(self):
        return self._name

    def getArgs(self):
        return self._args