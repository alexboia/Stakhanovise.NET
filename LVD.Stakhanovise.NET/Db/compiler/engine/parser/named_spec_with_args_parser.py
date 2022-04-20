from .named_spec_with_args import NamedSpecWithArgs
from .named_spec_with_args_parser_base import NamedSpecWithArgsParserBase

class NamedSpecWithArgsParser(NamedSpecWithArgsParserBase):
    def parse(self, contents: str) -> NamedSpecWithArgs
        rawParts = self._parseRawParts(contents)
        
        name = rawParts["name"]
        rawArgsContents = rawParts["args"]
        args = self._parseArgs(rawArgsContents)

        return NamedSpecWithArgs(name, args)

    def _parseArgs(self, argsContents: str) -> list[str]:
        return self._parseRawArgsContents(argsContents)