from ..helper.string import sprintf

class CompilerOutputInfo:
    _name: str = None
    _arguments: dict= {}

    def __init__(self, name: str, arguments: dict):
        self._name = name
        self._arguments = arguments

    def getName(self):
        return self._name

    def getArguments(self):
        return self._arguments

    def __str__(self) -> str:
        return sprintf("{name: %s, arguments: %s}" % (self._name, self._arguments))