from io import StringIO

def sprintf(stringFormat: str, *args) -> str:
    sprintfBuffer = StringIO()
    sprintfBuffer.write(stringFormat % args)

    return sprintfBuffer.getvalue()

def str_to_bool(sourceString: str) -> bool:
    if (sourceString is None or len(sourceString) == 0):
        return False

    return "TRUE" == sourceString.upper()