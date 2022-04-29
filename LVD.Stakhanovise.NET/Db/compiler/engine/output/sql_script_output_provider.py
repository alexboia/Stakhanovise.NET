from ..helper.string_builder import StringBuilder
from ..helper.vs_project_facade import VsProjectFacade
from ..helper.vs_project_file_saver import VsProjectFileSaver

from .sql_script_output_provider_options import SqlScriptOutputProviderOptions
from .sql_script_output_provider_base import SqlScriptOutputProviderBase

class SqlScriptOutputProvider(SqlScriptOutputProviderBase):
    _options: SqlScriptOutputProviderOptions = None
    _vsProjectFacade: VsProjectFacade = None

    def __init__(self, options: SqlScriptOutputProviderOptions, vsProjectFacade: VsProjectFacade) -> None:
        super().__init__()
        self._options = options
        self._vsProjectFacade = vsProjectFacade
        self._buffers = {}

    def commit(self) -> None:
        globalBuffer = None 
        fileSaver = self._getVsProjectFileSaver()

        if self._options.generateAsConsolidated():
            globalBuffer = StringBuilder()

        for objectName in self._buffers.keys():
            objectBuffer = self._buffers[objectName]
            if self._options.generateAsSingle():
                fileName = self._expandOutputFileName(objectName)
                relativeFilePath = self._getRelativeFilePath(fileName)
                fileSaver.saveFile(relativeFilePath, objectBuffer.toString())
            else:
                globalBuffer.append(objectBuffer.toString())

        if self._options.generateAsConsolidated():
            fileName = self._getOutputFileName()
            relativeFilePath = self._getRelativeFilePath(fileName)
            fileSaver.saveFile(relativeFilePath, globalBuffer.toString())

        fileSaver.commit(self._getOutputFileItemGroupLabel(), 
            self._getOutputFileBuildAction(), 
            self._getOutputFileBuildOptions())

    def _getVsProjectFileSaver(self) -> VsProjectFileSaver:
        projectName = self._options.getTargetProjectName()
        return VsProjectFileSaver(self._vsProjectFacade, projectName)

    def _getRelativeFilePath(self, fileName: str) -> str:
        return self._options.getDestinationDirectory() + '/' + fileName

    def _expandOutputFileName(self, objectName: str) -> str:
        return self._options.expandFileName(objectName)

    def _getOutputFileName(self) -> str:
        return self._options.getFileName()

    def _getOutputFileItemGroupLabel(self) -> str:
        return self._options.getItemGroupLabel()

    def _getOutputFileBuildAction(self) -> str:
        return self._options.getBuildAction()

    def _getOutputFileBuildOptions(self) -> str:
        copyOutput = self._options.getCopyOutput()
        if copyOutput is not None:
            return { 'copy_output': copyOutput }
        else:
            return {}