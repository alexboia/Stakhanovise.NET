from .support.named_spec_with_args import NamedSpecWithArgs
from .support.args_list_parser import ArgsListParser
from .support.definition_with_properties import DefinitionWithProperties
from .support.definition_with_properties_parser import DefinitionWithPropertiesParser
from ..model.db_mapping import DbMapping
from ..model.db_constraint import DbConstraint

class DbConstraintParser:
    _mapping: DbMapping

    def __init__(self, mapping: DbMapping):
        self._mapping = mapping

    def parse(self, constraintContents: str) -> DbConstraint:
        constraintContents = constraintContents or ''
        if (len(constraintContents) > 0):
            constraintDefinition = self._readRawConstraintDefinition(constraintContents)
            if (constraintDefinition is not None):
                constraintName = constraintDefinition.getName()
                constraintColumns = self._readConstraintColumns(constraintDefinition.getArgsContents())

                constraintType = constraintDefinition.getProperty("type", None)
                if (constraintType is None):
                    raise ValueError('Constraint type is mandatory')

                if (not DbConstraint.isValidConstraintType(constraintType)):
                    raise ValueError('Constraint type is of invalid type: <'  + constraintType + '>')

                return DbConstraint(constraintName, constraintColumns, constraintType)
            else:
                return None
        else:
            return None

    def _readRawConstraintDefinition(self, constraintContents: str) -> DefinitionWithProperties:
       parser = DefinitionWithPropertiesParser(self._mapping)
       return parser.parse(constraintContents)

    def _readConstraintColumns(self, argsContents: str) -> list[str]:
        parser = ArgsListParser(',')
        return parser.parse(argsContents)