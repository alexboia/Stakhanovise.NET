from datetime import date
from string import Template

from ...compiler_asset_provider import CompilerAssetProvider

from ...helper.string_builder import StringBuilder
from ...model.db_mapping import DbMapping

from ..mapping_code_output_provider_options import MappingCodeOutputProviderOptions

class MappingClassWriter:
    _classBuffer: StringBuilder = None
    _options: MappingCodeOutputProviderOptions
    _compilerAssetProvider: CompilerAssetProvider = None

    def __init__(self, classBuffer: StringBuilder, 
            options: MappingCodeOutputProviderOptions, 
            compilerAssetProvider: CompilerAssetProvider ) -> None:
        self._classBuffer = classBuffer
        self._options = options
        self._compilerAssetProvider = compilerAssetProvider

    def write(self, dbMapping: DbMapping) -> None:
        sourceCodeContents = self._renderMappingClassSourceCodeContents(dbMapping)
        self._classBuffer.appendLine(sourceCodeContents)

    def _renderMappingClassSourceCodeContents(self, dbMapping: DbMapping) -> str:
        template = self._getMappingClassSourceCodeTemplate()
        licenseHeader = self._renderLicenseHeaderText()

        templateVars = dict(license_header = licenseHeader, 
            class_namespace_name = self._getMappingClassNamespace(), 
            class_name = self._getMappingClassName(),
            queue_table_name = dbMapping.getQueueTableName(),
            results_queue_table_name = dbMapping.getResultsQueueTableName(),
            execution_time_stats_table_name = dbMapping.getExecutionTimeStatsTableName(),
            metrics_table_name = dbMapping.getMetricsTableName(),
            new_task_notification_channel_name = dbMapping.getNewTaskNotificationChannelName(),
            dequeue_function_name = dbMapping.getDequeueFunctionName())

        return template.substitute(templateVars)

    def _renderLicenseHeaderText(self) -> str:
        template = self._getLicenseHeaderTemplate()
        templateVars = dict(current_year = self._getCurrentYear())
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