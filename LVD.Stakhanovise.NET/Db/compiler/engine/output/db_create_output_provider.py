import os
from ..helper.string_builder import StringBuilder

from .sql_script_output_provider_base import SqlScriptOutputProviderBase

class DbCreateOutputProvider(SqlScriptOutputProviderBase):
    def __init__(self) -> None:
        super().__init__()
        self._buffers = {}

    def commit(self) -> None:
        pass