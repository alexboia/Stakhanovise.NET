﻿from ..model.project_names import COMMON_PROJECT_NAME
from ..model.build_actions import BUID_ACTION_COMPILE

DEFAULT_DIRECTORY = 'Model'
DEFAULT_CLASS_NAMESPACE = 'LVD.Stakhanovise.NET.Model'
DEFAULT_CLASS_NAME = 'QueuedTaskMapping'
DEFAULT_ITEM_GROUP = 'SK_MappingCode'

class MappingCodeOutputProviderOptions:
    _arguments: dict[str, str] = None

    def __init__(self, arguments: dict[str, str] = None) -> None:
        self._arguments = arguments or {}

    def getTargetProjectName(self) -> str:
        return self._arguments.get('proj', COMMON_PROJECT_NAME)

    def getBuildAction(self) -> str:
        return BUID_ACTION_COMPILE

    def getDestinationDirectory(self) -> str:
        return self._arguments.get('dir', DEFAULT_DIRECTORY)

    def getClassNamespace(self) -> str:
        return self._arguments.get('ns', DEFAULT_CLASS_NAMESPACE)

    def getClassName(self) -> str:
        return self._arguments.get('cls', DEFAULT_CLASS_NAME)

    def getItemGroupLabel(self) -> str:
        return DEFAULT_ITEM_GROUP