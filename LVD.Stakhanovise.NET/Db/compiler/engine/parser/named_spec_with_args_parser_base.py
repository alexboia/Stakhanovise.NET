from .args_list_parser import ArgsListParser

class NamedSpecWithArgsParserBase:
    def _parseRawParts(self, contents: str) -> dict:
        rawParts: dict = {}
        parseContents = contents.strip()
        
        openParanthesisIndex = contents.index('(')
        closeParanthesisIndex = contents.rindex(')')

        rawParts["name"] = contents[0:openParanthesisIndex]
        rawParts["args"] = contents[openParanthesisIndex + 1:closeParanthesisIndex]

        return rawParts

    def _parseRawArgsList(self, argsContents: str) -> list[str]:
        parser = ArgsListParser()
        return parser.parse(argsContents)