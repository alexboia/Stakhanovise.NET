from datetime import date
from string import Template

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

class MappingCodeOutputProvider(OutputProvider):
    _buffer: StringBuilder = None
    _options: MappingCodeOutputProviderOptions
    _vsProjectFacade: VsProjectFacade = None
    _compilerAssetProvider: CompilerAssetProvider = None

    def __init__(self, options: MappingCodeOutputProviderOptions, vsProjectFacade: VsProjectFacade, compilerAssetProvider: CompilerAssetProvider) -> None:
        super().__init__()
        self._options = options
        self._vsProjectFacade = vsProjectFacade
        self._compilerAssetProvider = compilerAssetProvider
        self._buffer = StringBuilder()

    def writeMapping(self, dbMapping: DbMapping) -> None:
        sourceCodeContents = self._getMappingClassSourceCodeContents(dbMapping)
        self._buffer.appendLine(sourceCodeContents)

    def _getMappingClassSourceCodeContents(self, dbMapping: DbMapping) -> str
        template = self._getMappingClassSourceCodeTemplate()
        licenseHeader = self._getLicenseHeaderText()

        templateVars = dict(license_header=licenseHeader, 
            class_namespace_name=self._getMappingClassNamespace(), 
            class_name=self._getMappingClassName(),
            queue_table_name=dbMapping.getQueueTableName(),
            results_queue_table_name=dbMapping.getResultsQueueTableName(),
            execution_time_stats_table_name=dbMapping.getExecutionTimeStatsTableName(),
            metrics_table_name=dbMapping.getMetricsTableName(),
            new_task_notification_channel_name=dbMapping.getNewTaskNotificationChannelName(),
            dequeue_function_name=dbMapping.getDequeueFunctionName())

        return template.substitute(templateVars)

    def _getLicenseHeaderText(self) -> str:
        template = self._getLicenseHeaderTemplate()
        templateVars = dict(current_year=self._getCurrentYear())
        return template.substitute(templateVars)

    def _getLicenseHeaderTemplate(self) -> Template:
        contents = self._compilerAssetProvider.getSourceFileContents('templates/license_header.cstemplate')
        return Template(contents)

    def _getMappingClassSourceCodeTemplate(self) -> Template:
        contents = self._compilerAssetProvider.getSourceFileContents('templates/queued_task_mapping.cstemplate')
        return Template(contents)

    def _getMappingClassName(self) -> str:
        return self._options.getClassName()

    def _getMappingClassNamespace(self) -> str:
        return self._options.getClassNamespace()

    def _getCurrentYear(self) -> int:
        return date.today().year

    def writeTable(self, dbTable: DbTable) -> None:
        pass

    def writeSequence(self, dbSequence: DbSequence) -> None:
        pass

    def writeFunction(self, dbFunction: DbFunction) -> None:
        pass

    def commit(self) -> None:
        fileSaver = self._getVsProjectFileSaver()
        relativeFilePath = self._getRelativeMappingClassFilePath()

        fileContents = self._buffer.toString()
        fileSaver.saveFile(relativeFilePath, fileContents)

        self._reset()

    def _getVsProjectFileSaver(self) -> VsProjectFileSaver:
        projectName = self.__getTargetProjectName()
        return VsProjectFileSaver(self._vsProjectFacade, projectName)

    def _getRelativeMappingClassFilePath(self) -> str
        return sprintf('%s/%s.cs' % (self._getDestinationDirectory(), self._getMappingClassName()))

    def _getDestinationDirectory(self) -> str
        return self._options.getDestinationDirectory()

    def _getTargetProjectName(self) -> str
        return self._options.getTargetProjectName()

    def _reset(self) -> None:
        self._buffer.close()
        self._buffer = StringBuilder()