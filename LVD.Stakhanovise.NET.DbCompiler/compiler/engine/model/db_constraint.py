from ..helper.string import sprintf

TYPE_UNIQUE = "unq"
TYPE_PRIMARY_KEY = "pk"

class DbConstraint:
    _name: str = None
    _columnNames: list[str] = None
    _type: str = None

    def __init__(self, name: str, columnNames: list[str], type: str):
        self._name = name
        self._type = type
        self._columnNames = columnNames or []

    @staticmethod
    def getAllValidConstraintTypes() -> list[str]:
        return [TYPE_PRIMARY_KEY, 
                TYPE_UNIQUE]

    @staticmethod
    def isValidConstraintType(typeName: str) -> bool:
        validTypeNames = __class__.getAllValidConstraintTypes()
        return (typeName in validTypeNames)

    def getName(self) -> str:
        return self._name

    def getType(self) -> str:
        return self._type

    def getColumnNames(self) -> list[str]:
        return self._columnNames

    def isUniqueConstraint(self) -> bool:
        return self.getType() == TYPE_UNIQUE

    def isPrimaryKeyConstraint(self) -> bool:
        return self.getType() == TYPE_PRIMARY_KEY

    def hasColumn(self, columnName: str) -> bool:
        columNames = self.getColumnNames()
        return (columNames is not None) and (columnName in columNames)

    def __str__(self) -> str:
        return sprintf('{name = %s, columnNames = %s, type = %s}' % (self._name, self._columnNames, self._type))