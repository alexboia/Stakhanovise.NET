from typing import Callable

from ..compiler_asset_provider import CompilerAssetProvider
from ..helper.vs_project_facade import VsProjectFacade
from ..model.compiler_output_info import CompilerOutputInfo

from .output_provider import OutputProvider
from .console_output_provider import ConsoleOutputProvider
from .console_output_provider_options import ConsoleOutputProviderOptions

from .sql_script_output_provider import SqlScriptOutputProvider
from .sql_script_output_provider_options import SqlScriptOutputProviderOptions

from .db_create_output_provider import DbCreateOutputProvider
from .db_create_output_provider_options import DbCreateOutputProviderOptions

from .markdown_docs_output_provider import MarkdownDocsOutputProvider
from .markdown_docs_output_provider_options import MarkdownDocsOutputProviderOptions

from .mapping_code_output_provider import MappingCodeOutputProvider
from .mapping_code_output_provider_options import MappingCodeOutputProviderOptions

class OutputProviderRegistry:
    _providers: dict[str, Callable[[CompilerOutputInfo], OutputProvider]] = None

    def __init__(self, vsProjectFacade: VsProjectFacade, compilerAssetProvider: CompilerAssetProvider) -> None:
        self._providers = {}
        self._providers['console'] = (lambda outputInfo: ConsoleOutputProvider(ConsoleOutputProviderOptions(outputInfo.getArguments())))
        self._providers['sql_script'] = (lambda outputInfo: SqlScriptOutputProvider(SqlScriptOutputProviderOptions(outputInfo.getArguments()), vsProjectFacade))
        self._providers['db_create'] = (lambda outputInfo: DbCreateOutputProvider(DbCreateOutputProviderOptions(outputInfo.getArguments())))
        self._providers['markdown_docs'] = (lambda outputInfo: MarkdownDocsOutputProvider(MarkdownDocsOutputProviderOptions(outputInfo.getArguments()), vsProjectFacade, compilerAssetProvider))
        self._providers['mapping_code'] = (lambda outputInfo: MappingCodeOutputProvider(MappingCodeOutputProviderOptions(outputInfo.getArguments()), vsProjectFacade, compilerAssetProvider))

    def createOutputProvider(self, outputInfo: CompilerOutputInfo) -> OutputProvider:
        factory = self._providers.get(outputInfo.getName(), None)
        if factory is not None:
            return factory(outputInfo)
        else:
            return None