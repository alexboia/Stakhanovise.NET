from ..helper.string import sprintf

class CompilerOutputInfo:
    _name: str = None
    _arguments: dict[str, str] = None

    def __init__(self, name: str, arguments: dict[str, str]):
        self._name = name
        self._arguments = arguments or {}

    def getName(self) -> str:
        return self._name

    def getArguments(self) -> dict[str, str]:
        return self._arguments

    def __str__(self) -> str:
        return sprintf("{name: %s, arguments: %s}" % (self._name, self._arguments))