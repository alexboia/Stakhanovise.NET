class NamedSpecWithArgsRawParser:
    def parse(self, contents: str) -> dict[str, str]:
        rawParts: dict = {}
        parseContents = contents.strip()
        
        openParanthesisIndex = contents.find('(')
        closeParanthesisIndex = contents.rfind(')')

        if (openParanthesisIndex >= 0 and closeParanthesisIndex >= 0):
            rawParts["name"] = contents[0:openParanthesisIndex]
            rawParts["args"] = contents[openParanthesisIndex + 1:closeParanthesisIndex]
        else:
            rawParts["name"] = contents
            rawParts["args"] = ''

        return rawParts