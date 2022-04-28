from io import StringIO

class StringBuilder:
    _buffer: StringIO = None

    def __init__(self, initialString: str = '') -> None:
        self._buffer = StringIO(initialString)

    def append(self, appendString: str):
        self._buffer.write(appendString)
        return self

    def appendEmptyLine(self):
        self.append('\n')
        return self

    def appendLine(self, appendString: str):
        self.append(appendString + '\n')
        return self

    def appendFormat(self, stringFormat: str, *args):
        self._buffer.write(stringFormat % args)
        return self

    def appendLineFormat(self, stringFormat: str, *args):
        stringFormat = stringFormat + '\n'
        self.appendFormat(stringFormat % args)
        return self

    def appendIndented(self, appendString: str, indentLevel: int = 1):
        indentString = '\t' * indentLevel
        self.append(indentString + appendString)
        return self

    def appendLineIndented(self, appendString: str, indentLevel: int = 1):
        appendString = appendString + '\n'
        self.appendIndented(appendString, indentLevel)
        return self

    def toString(self) -> str:
        return self._buffer.getvalue()

    def close(self) -> None:
        self._buffer.close()

    def __str__(self) -> str:
        return self.toString()