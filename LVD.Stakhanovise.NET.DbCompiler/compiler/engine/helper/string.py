from io import StringIO

def sprintf(stringFormat: str, *args) -> str:
    sprintfBuffer = StringIO()
    sprintfBuffer.write(stringFormat % args)

    finalValue = sprintfBuffer.getvalue()
    sprintfBuffer.close()
    
    return finalValue

def str_to_bool(sourceString: str) -> bool:
    if (sourceString is None or len(sourceString) == 0):
        return False

    return "TRUE" == sourceString.upper()

def bool_to_yesno(boolVal: bool) -> str:
    return "Yes" if boolVal else "No"