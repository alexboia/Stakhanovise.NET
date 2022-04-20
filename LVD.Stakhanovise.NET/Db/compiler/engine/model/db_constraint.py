class DbConstraint:
    _name: str = None
    _columnNames: list[str] = []
    _type: str = None

    def __init__(self, name: str, type: str, columnNames: list[str]):
        self._name = name
        self._type = type
        self._columnNames = columnNames

    def getName(self):
        return self._name

    def getType(self):
        return self._type

    def getColumnNames(self):
        return self._columnNames