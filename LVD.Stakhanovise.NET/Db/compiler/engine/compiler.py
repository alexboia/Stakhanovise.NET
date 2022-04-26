from .parser.db_object_parser_registry import DbObjectParserRegistry

class Compiler:
    _parserRegistry: DbObjectParserRegistry = None

    def __init__(self):
        self._parserRegistry = DbObjectParserRegistry()

    def compile(self, makefile = "./makefile"):
        pass

    def _parse(self):
        pass

    def _output(self):
        pass