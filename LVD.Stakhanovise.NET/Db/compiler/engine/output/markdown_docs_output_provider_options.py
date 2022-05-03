from ..helper.output_file_build_options_builder import OutputFileBuildOptionsBuilder

from ..model.project_names import MAIN_PROJECT_NAME
from ..model.build_actions import BUILD_ACTION_NONE

DEFAULT_HEADER_FILE_NAME = 'parts/readme_db_header.md'
DEFAULT_FOOTER_FILE_NAME = 'parts/readme_db_footer.md'

class MarkdownDocsOutputProviderOptions(OutputFileBuildOptionsBuilder):
    _arguments: dict[str, str] = None

    def __init__(self, arguments: dict[str, str] = None) -> None:
        self._arguments = arguments or {}

    def getHeaderFile(self) -> str:
        return self._arguments.get('header', DEFAULT_HEADER_FILE_NAME)

    def getFooterFile(self) -> str:
        return self._arguments.get('footer', DEFAULT_FOOTER_FILE_NAME)

    def getTargetProjectName(self) -> str:
        return self._arguments.get('proj', MAIN_PROJECT_NAME)

    def getCopyOutput(self) -> str:
        return self._arguments.get('copy_output')

    def getBuildAction(self) -> str:
        return self._arguments.get('build_action', BUILD_ACTION_NONE)

    def getItemGroupLabel(self) -> str:
        return self._arguments.get('item_group')

    def getDestinationDirectory(self) -> str:
        return self._arguments.get('dir')

    def getFileName(self) -> str:
        return self._arguments.get('file', 'README-DB.md')
