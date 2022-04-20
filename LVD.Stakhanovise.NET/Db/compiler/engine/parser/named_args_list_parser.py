from .args_list_parser import ArgsListParser

class NamedArgsListParser:
    _separator: str = None

    def __init__(self, separator: str = ';'):
        self._separator = separator

    def parse(self, argsContents: str) -> {}:
        args = {}
        rawArgs = self._parseRawArgsList(argsContents)

        for rawArg in rawArgs:
            rawArgParts = rawArg.split('=', 1)
            
            if (len(rawArgParts) >= 1):
                argName = rawArgParts[0].strip()

                if (len(rawArgParts) == 2):
                    argValue = rawArgParts[1].strip()
                else:
                    argValue = None
                
                if (len(argName) > 0):
                    args[argName] = argValue

        return args

    def _parseRawArgsList(self, argsContents: str) -> list[str]:
        parser = ArgsListParser(self._separator)
        return parser.parse(argsContents)