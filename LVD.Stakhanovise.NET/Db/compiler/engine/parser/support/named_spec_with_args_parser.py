from .args_list_parser import ArgsListParser
from .named_spec_with_args import NamedSpecWithArgs
from .named_spec_with_args_raw_parser import NamedSpecWithArgsRawParser

class NamedSpecWithArgsParser:
    def parse(self, contents: str) -> NamedSpecWithArgs
        rawParts = self._parseRawParts(contents)
        
        name = rawParts["name"]
        rawArgsContents = rawParts["args"]

        args = self._parseArgs(rawArgsContents)
        return NamedSpecWithArgs(name, args)

    def _parseRawParts(self, contents: str) -> dict[str, str]:
        rawParser = NamedSpecWithArgsRawParser()
        return rawParser.parse(contents)

    def _parseArgs(self, argsContents: str) -> list[str]:
        parser = ArgsListParser()
        return parser.parse(argsContents)