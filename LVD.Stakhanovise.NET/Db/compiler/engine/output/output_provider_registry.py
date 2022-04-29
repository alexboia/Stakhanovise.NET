from typing import Callable

from ..helper.vs_project_facade import VsProjectFacade
from ..model.compiler_output_info import CompilerOutputInfo

from .output_provider import OutputProvider
from .console_output_provider import ConsoleOutputProvider
from .console_output_provider_options import ConsoleOutputProviderOptions

from .sql_script_output_provider import SqlScriptOutputProvider
from .sql_script_output_provider_options import SqlScriptOutputProviderOptions

from .db_create_output_provider import DbCreateOutputProvider
from .db_create_output_provider_options import DbCreateOutputProviderOptions

class OutputProviderRegistry:
    _resolvers: dict[str, Callable[[CompilerOutputInfo], OutputProvider]] = {}

    def __init__(self, vsProjectFacade: VsProjectFacade) -> None:
        self._resolvers['console'] = (lambda outputInfo: ConsoleOutputProvider(ConsoleOutputProviderOptions(outputInfo.getArguments())))
        self._resolvers['sql_script'] = (lambda outputInfo: SqlScriptOutputProvider(SqlScriptOutputProviderOptions(outputInfo.getArguments()), vsProjectFacade))
        self._resolvers['db_create'] = (lambda outputInfo: DbCreateOutputProvider(DbCreateOutputProviderOptions(outputInfo.getArguments())))

    def createOutputProvider(self, outputInfo: CompilerOutputInfo) -> OutputProvider:
        factory = self._resolvers.get(outputInfo.getName(), None)
        if factory is not None:
            return factory(outputInfo)
        else:
            return None