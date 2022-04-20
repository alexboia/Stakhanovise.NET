class NamedSpecWithNamedArgs:
    _name: str = None
    _args: dict = {}

    def __init__(self, name: str, args: dict):
        self._name = name
        self._args = args

    def getName(self):
        return self._name

    def getArgs(self):
        return self._args