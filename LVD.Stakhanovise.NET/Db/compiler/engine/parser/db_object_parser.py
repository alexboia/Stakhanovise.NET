from abc import abstractmethod
from typing import Generic, TypeVar

TDbObject = TypeVar('TDbObject')

class DbObjectParser(Generic[TDbObject]):
    @abstractmethod
    def parseFromFile(self, sourceFile:str) -> TDbObject:
        pass

    @abstractmethod
    def parse(self, sourceFileLines: list[str]) -> TDbObject:
        pass