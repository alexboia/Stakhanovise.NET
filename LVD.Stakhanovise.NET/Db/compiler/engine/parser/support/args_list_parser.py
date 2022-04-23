class ArgsListParser:
    _separator: str = None

    def __init__(self, separator: str = ';'):
        self._separator = separator

    def parse(self, argsContents: str) -> list[str]:
        if len(argsContents) > 0:
            args = argsContents.split(self._separator)
            args = map(lambda arg: arg.strip(), args)
            return list(filter(lambda arg: len(arg) > 0, args))
        else:
            return []