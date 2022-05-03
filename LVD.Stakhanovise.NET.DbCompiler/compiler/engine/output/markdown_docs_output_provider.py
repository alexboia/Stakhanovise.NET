from os import write
from ..compiler_asset_provider import CompilerAssetProvider

from ..helper.string_builder import StringBuilder
from ..helper.vs_project_facade import VsProjectFacade
from ..helper.vs_project_file_saver import VsProjectFileSaver

from ..model.db_function import DbFunction
from ..model.db_sequence import DbSequence
from ..model.db_table import DbTable
from ..model.db_mapping import DbMapping

from .output_provider import OutputProvider
from .markdown_docs.markdown_db_sequence_writer import MarkdownDbSequenceWriter
from .markdown_docs.markdown_db_function_writer import MarkdownDbFunctionWriter
from .markdown_docs.markdown_db_table_writer import MarkdownDbTableWriter
from .markdown_docs_output_provider_options import MarkdownDocsOutputProviderOptions

HEADER_SEPARATOR = '\n' * 2
FOOTER_SEPARATOR = HEADER_SEPARATOR

class MarkdownDocsOutputProvider(OutputProvider):
    _buffer: StringBuilder = None
    _options: MarkdownDocsOutputProviderOptions = None
    _vsProjectFacade: VsProjectFacade = None
    _compilerAssetProvider: CompilerAssetProvider = None

    def __init__(self, options: MarkdownDocsOutputProviderOptions, 
            vsProjectFacade: VsProjectFacade, 
            compilerAssetProvider: CompilerAssetProvider) -> None:
        super().__init__()
        self._options = options
        self._buffer = StringBuilder()
        self._vsProjectFacade = vsProjectFacade
        self._compilerAssetProvider = compilerAssetProvider

    def writeMapping(self, dbMapping: DbMapping) -> None:
        pass

    def writeTable(self, dbTable: DbTable) -> None:
        writer = MarkdownDbTableWriter(self._buffer)
        writer.write(dbTable)

    def writeSequence(self, dbSequence: DbSequence) -> None:
        writer = MarkdownDbSequenceWriter(self._buffer)
        writer.write(dbSequence)

    def writeFunction(self, dbFunction: DbFunction) -> None:
        writer = MarkdownDbFunctionWriter(self._buffer)
        writer.write(dbFunction)

    def commit(self) -> None:
        fileSaver = self._getVsProjectFileSaver()
        relativeFilePath = self._getRelativeOutputFilePath()
        fileContents = self._getFileContents()

        fileSaver.saveFile(relativeFilePath, fileContents)
        fileSaver.commit(self._getOutputFileItemGroupLabel(), 
            self._getOutputFileBuildAction(), 
            self._getOutputFileBuildOptions())

        self._reset()

    def _getFileContents(self) -> str:
        contents = self._buffer.toString()
        contentsHeader = self._getContentsHeader() or ''
        contentsFooter = self._getContentsFooter() or ''

        if len(contentsHeader) > 0:
            contentsHeader = contentsHeader + HEADER_SEPARATOR

        if len(contentsFooter) > 0:
            contentsFooter = FOOTER_SEPARATOR + contentsFooter

        return contentsHeader + contents + contentsFooter

    def _getContentsHeader(self) -> str:
        headerFilePath = self._getRelativeHeaderFilePath()
        return self._compilerAssetProvider.getSourceFileContents(headerFilePath)

    def _getContentsFooter(self) -> str:
        footerFilePath = self._getRelativeFooterFilePath()
        return self._compilerAssetProvider.getSourceFileContents(footerFilePath)

    def _reset(self) -> None:
        self._buffer.close()
        self._buffer = StringBuilder()

    def _getVsProjectFileSaver(self) -> VsProjectFileSaver:
        projectName = self._getTargetProjectName()
        return VsProjectFileSaver(self._vsProjectFacade, projectName)

    def _getTargetProjectName(self) -> str:
        return self._options.getTargetProjectName()

    def _getRelativeHeaderFilePath(self) -> str:
        return self._options.getHeaderFile()

    def _getRelativeFooterFilePath(self) -> str:
        return self._options.getFooterFile()

    def _getRelativeOutputFilePath(self) -> str:
        return self._options.getDestinationDirectory() + '/' + self._options.getFileName()

    def _getOutputFileItemGroupLabel(self) -> str:
        return self._options.getItemGroupLabel()

    def _getOutputFileBuildAction(self) -> str:
        return self._options.getBuildAction()

    def _getOutputFileBuildOptions(self) -> dict[str, str]:
        return self._options.getOutputFileBuildOptions()