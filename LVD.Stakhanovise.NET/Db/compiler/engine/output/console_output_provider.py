from prettytable import PrettyTable
from rich import print
from rich.text import Text
from rich.console import Console
from rich.panel import Panel

from ..helper.string import sprintf
from ..helper.string import bool_to_yesno
from ..model.compiler_output_info import CompilerOutputInfo
from ..model.db_object_prop import DbObjectProp
from ..model.db_function import DbFunction
from ..model.db_sequence import DbSequence
from ..model.db_table import DbTable
from ..model.db_column import DbColumn
from ..model.db_constraint import DbConstraint
from ..model.db_index import DbIndex
from .output_provider import OutputProvider

class ConsoleOutputProvider(OutputProvider):
    _console: Console = None

    def __init__(self, outputInfo: CompilerOutputInfo) -> None:
        super().__init__(outputInfo)
        self._console = Console()

    def writeTable(self, dbTable: DbTable) -> None:
        self._writeObjectTitle("Table", dbTable.getName())
        self._writeObjectProperties(dbTable.getProperties())
        self._writeTableColumns(dbTable.getColumns())
        self._writePrimaryKey(dbTable.getPrimaryKey())

    def _writeObjectTitle(self, prefix: str, name: str) -> None:
        title = self._getObjectTitlePanel(prefix, name)
        self._console.print(title)

    def _getObjectTitlePanel(self, prefix: str, name: str) -> Panel:
        titleString = sprintf("%s: %s" % (prefix, name))
        titleText = Text(titleString)
        titleText.stylize("bold blue")
        return Panel(titleText)

    def _writeObjectProperties(self, properties: dict[str, DbObjectProp]) -> None:
        propsTitle = self._getObjectSectionTitle("Properties:")
        self._console.print(propsTitle)

        propsDescription = self._getObjectPropertiesDescription(properties)
        self._console.print(propsDescription)

        self._writeSectionSpacer()

    def _getObjectSectionTitle(self, titleString: str) -> Text:
        titleText = Text(titleString)
        titleText.stylize("underline")
        return titleText

    def _writeSectionSpacer(self) -> None:
        self._console.print('')

    def _getObjectPropertiesDescription(self, properties: dict[str, DbObjectProp]) -> str:
        propsTable = PrettyTable()
        propsTable.field_names = ['Name', 'Value']
        
        for propKey in properties:
            propsTable.add_row([properties[propKey].name, properties[propKey].value])

        return propsTable.get_string()

    def _writeTableColumns(self, columns: list[DbColumn]) -> None:
        colsTitle = self._getObjectSectionTitle("Columns:")
        self._console.print(colsTitle)

        if len(columns) > 0:
            colsDescription = self._getTableColumnsDescription(columns)
            self._console.print(colsDescription)
        else:
            colsMissing = self._getObjectMissingMessage('columns')
            self._console.print(colsMissing)
        
        self._writeSectionSpacer()

    def _getTableColumnsDescription(self, columns: list[DbColumn]) -> str:
        colsTable = PrettyTable()
        colsTable.field_names = ['Name', 'Description', 'Type', 'Not Null', 'Default Value']
        
        for col in columns:
            colsTable.add_row([col.getName(), 
                col.getDescritption() or '[No description]', 
                col.getType(), 
                bool_to_yesno(col.isNotNull()), 
                col.getDefaultValue() or '[No default]'])

        return colsTable.get_string()

    def _writePrimaryKey(self, primaryKey: DbConstraint) -> None:
        pkTitle = self._getObjectSectionTitle('Primary Key:')
        self._console.print(pkTitle)

        if primaryKey is not None:
            pkDescription = self._getTableConstraintsDescription([primaryKey])
            self._console.print(pkDescription)
        else:
            pkMissing = self._getObjectMissingMessage('primary key')
            self._console.print(pkMissing)

        self._writeSectionSpacer()

    def _getTableConstraintsDescription(self, constraints: list[DbConstraint]) -> str:
        constraintTable = PrettyTable()
        constraintTable.field_names = ['Name', 'Columns']

        for constraint in constraints:
            constraintTable.add_row([constraint.getName(), ','.join(constraint.getColumnNames())])

        return constraintTable.get_string()

    def _getObjectMissingMessage(self, objectDescription: str) -> Text:
        warningText = Text(sprintf('Missing %s!' % (objectDescription)))
        warningText.stylize("red")
        return warningText

    def writeSequence(self, dbSequence: DbSequence) -> None:
        pass

    def writeFunction(self, dbFunction: DbFunction) -> None:
        pass