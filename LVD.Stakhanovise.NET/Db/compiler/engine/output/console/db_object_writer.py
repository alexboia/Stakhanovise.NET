from abc import abstractmethod
from typing import Generic, TypeVar
from prettytable import PrettyTable
from rich import print
from rich.text import Text
from rich.console import Console
from rich.panel import Panel

from ...helper.string import sprintf
from ...model.db_object_prop import DbObjectProp

TDbObject = TypeVar('TDbObject')

class DbObjectWriter(Generic[TDbObject]):
    def __init__(self, console: Console) -> None:
        self._console = console

    def _writeObjectTitle(self, prefix: str, name: str) -> None:
        title = self._getObjectTitlePanel(prefix, name)
        self._console.print(title)

    def _getObjectTitlePanel(self, prefix: str, name: str) -> Panel:
        titleString = sprintf("%s: %s" % (prefix, name))
        titleText = Text(titleString)
        titleText.stylize("bold blue")
        return Panel(titleText)

    def _writeObjectProperties(self, properties: dict[str, DbObjectProp]) -> None:
        self._writeObjectSectionTitle("Properties:")

        propsDescription = self._getObjectPropertiesDescription(properties)
        self._console.print(propsDescription)

        self._writeSectionSpacer()

    def _getObjectPropertiesDescription(self, properties: dict[str, DbObjectProp]) -> str:
        propsTable = PrettyTable()
        propsTable.field_names = ['Name', 'Value']
        
        for propKey in properties:
            propsTable.add_row([properties[propKey].name, properties[propKey].value])

        return propsTable.get_string()

    def _writeObjectSectionTitle(self, titleString: str) -> None:
        sectionTitle = self._getObjectSectionTitle(titleString)
        self._console.print(sectionTitle)

    def _getObjectSectionTitle(self, titleString: str) -> Text:
        titleText = Text(titleString)
        titleText.stylize("underline")
        return titleText

    def _writeSectionSpacer(self) -> None:
        self._console.print('')

    def _getObjectMissingMessage(self, objectDescription: str, error: bool = True) -> Text:
        warningText = Text(sprintf('Missing %s!' % (objectDescription)))
        if error:
            warningText.stylize("bright_red")
        else:
            warningText.stylize("bright_yellow")
        return warningText

    @abstractmethod
    def write(self, dbObject: TDbObject) -> None:
        pass