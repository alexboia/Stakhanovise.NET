﻿from .named_args_list_parser import NamedArgsListParser
from .named_spec_with_named_args import NamedSpecWithNamedArgs
from .named_spec_with_args_raw_parser import NamedSpecWithArgsRawParser

class NamedSpecWithNamedArgsParser:
    _separator: str = None

    def __init__(self, separator: str = ';'):
        self._separator = separator

    def parse(self, contents: str) -> NamedSpecWithNamedArgs:
        rawParts = self._parseRawParts(contents)
        
        name = rawParts["name"]
        rawArgsContents = rawParts["args"]
        
        args = self._parseArgs(rawArgsContents)
        return NamedSpecWithNamedArgs(name, args)

    def _parseRawParts(self, contents: str) -> dict[str, str]:
        rawParser = NamedSpecWithArgsRawParser()
        return rawParser.parse(contents)

    def _parseArgs(self, argsContents: str) -> dict[str, str]:
        parser = NamedArgsListParser(self._separator)
        return parser.parse(argsContents)