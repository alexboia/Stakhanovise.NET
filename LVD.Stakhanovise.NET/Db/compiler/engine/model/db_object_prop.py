from ..helper.string import sprintf

class DbObjectProp:
    name: str = None
    value: str = None

    def __init__(self, name: str, value: str):
       self.name = name
       self.value = value

    def __str__(self) -> str:
        return sprintf("{name = %s, value = %s}" % (self.name, self.value))