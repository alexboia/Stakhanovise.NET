from abc import ABC, abstractmethod
from typing import Generic, TypeVar

from ...helper.string_builder import StringBuilder

TDbObject = TypeVar('TDbObject')

class SqlDbObjectWriter(ABC, Generic[TDbObject]):
    _sqlStringBuilder: StringBuilder

    def __init__(self, sqlStringBuilder: StringBuilder) -> None:
        super().__init__()
        self._sqlStringBuilder = sqlStringBuilder

    @abstractmethod
    def write(self, dbObject: TDbObject) -> None:
        pass