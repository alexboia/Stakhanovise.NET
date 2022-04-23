class NamedSpecWithArgsRawParser:
    def parse(self, contents: str) -> dict[str, str]:
        rawParts: dict = {}
        parseContents = contents.strip()
        
        openParanthesisIndex = contents.index('(')
        closeParanthesisIndex = contents.rindex(')')

        rawParts["name"] = contents[0:openParanthesisIndex]
        rawParts["args"] = contents[openParanthesisIndex + 1:closeParanthesisIndex]

        return rawParts