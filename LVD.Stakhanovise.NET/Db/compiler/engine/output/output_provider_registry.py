from typing import Callable
from ..model.compiler_output_info import CompilerOutputInfo
from .output_provider import OutputProvider
from .console_output_provider import ConsoleOutputProvider

class OutputProviderRegistry:
    _resolvers: dict[str, Callable[[CompilerOutputInfo], OutputProvider]] = {}

    def __init__(self) -> None:
        self._resolvers["console"] = (lambda outputInfo: ConsoleOutputProvider(outputInfo))

    def createOutputProvider(self, outputInfo: CompilerOutputInfo) -> OutputProvider:
        factory = self._resolvers.get(outputInfo.getName(), None)
        if factory is not None:
            return factory(outputInfo)
        else:
            return None