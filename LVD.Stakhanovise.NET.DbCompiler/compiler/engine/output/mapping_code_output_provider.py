from ..compiler_asset_provider import CompilerAssetProvider

from ..helper.string import sprintf
from ..helper.string_builder import StringBuilder
from ..helper.vs_project_facade import VsProjectFacade
from ..helper.vs_project_file_saver import VsProjectFileSaver

from ..model.db_function import DbFunction
from ..model.db_sequence import DbSequence
from ..model.db_table import DbTable
from ..model.db_mapping import DbMapping

from .output_provider import OutputProvider
from .mapping_code_output_provider_options import MappingCodeOutputProviderOptions
from .mapping_code.mapping_class_writer import MappingClassWriter

class MappingCodeOutputProvider(OutputProvider):
    _buffer: StringBuilder = None
    _options: MappingCodeOutputProviderOptions
    _vsProjectFacade: VsProjectFacade = None
    _compilerAssetProvider: CompilerAssetProvider = None

    def __init__(self, options: MappingCodeOutputProviderOptions, 
            vsProjectFacade: VsProjectFacade, 
            compilerAssetProvider: CompilerAssetProvider) -> None:
        super().__init__()
        self._options = options
        self._vsProjectFacade = vsProjectFacade
        self._compilerAssetProvider = compilerAssetProvider
        self._buffer = StringBuilder()

    def writeMapping(self, dbMapping: DbMapping) -> None:
        writer = MappingClassWriter(self._buffer, self._options, self._compilerAssetProvider)
        writer.write(dbMapping)

    def writeTable(self, dbTable: DbTable) -> None:
        pass

    def writeSequence(self, dbSequence: DbSequence) -> None:
        pass

    def writeFunction(self, dbFunction: DbFunction) -> None:
        pass

    def commit(self) -> None:
        fileSaver = self._getVsProjectFileSaver()
        relativeFilePath = self._getRelativeMappingClassFilePath()

        fileContents = self._getCurrentBufferContents()
        fileSaver.saveFile(relativeFilePath, fileContents)

        fileSaver.commit(self._getItemGroupLabel(), 
            self._getBuildAction())

        self._reset()

    def _getVsProjectFileSaver(self) -> VsProjectFileSaver:
        projectName = self._getTargetProjectName()
        return VsProjectFileSaver(self._vsProjectFacade, projectName)

    def _getRelativeMappingClassFilePath(self) -> str:
        return sprintf('%s/%s.cs' % (self._getDestinationDirectory(), self._getMappingClassName()))

    def _getDestinationDirectory(self) -> str:
        return self._options.getDestinationDirectory()

    def _getMappingClassName(self) -> str:
        return self._options.getClassName()

    def _getCurrentBufferContents(self) -> str:
        return self._buffer.toString()  

    def _getTargetProjectName(self) -> str:
        return self._options.getTargetProjectName()

    def _getItemGroupLabel(self) -> str:
        return self._options.getItemGroupLabel()

    def _getBuildAction(self) -> str:
        return self._options.getBuildAction()

    def _reset(self) -> None:
        self._buffer.close()
        self._buffer = StringBuilder()