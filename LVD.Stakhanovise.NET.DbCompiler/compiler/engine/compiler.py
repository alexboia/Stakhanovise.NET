import os

from .compiler_asset_provider import CompilerAssetProvider
from .helper.vs_project_facade import VsProjectFacade

from .model.db import Db
from .model.makefile_info import MakefileInfo
from .model.db_mapping import DbMapping
from .model.db_object import DbObject
from .model.compiler_output_info import CompilerOutputInfo

from .parser.makefile_parser import MakefileParser
from .parser.db_mapping_parser import DbMappingParser
from .parser.db_object_parser import DbObjectParser
from .parser.db_object_parser_registry import DbObjectParserRegistry
from .parser.source_file_reader import SourceFileReader
from .parser.source_definition_file_type_sniffer import SourceDefinitionFileTypeSniffer

from .output.output_provider_registry import OutputProviderRegistry
from .output.output_provider import OutputProvider

class Compiler:
    _parserRegistry: DbObjectParserRegistry = None
    _outputProviderRegistry: OutputProviderRegistry = None
    _compilerAssetProvider: CompilerAssetProvider = None
    _sourceDefinitionFileTypeSniffer: SourceDefinitionFileTypeSniffer

    def __init__(self, sourceDirectory: str, solutionRootDirectory: str):
        vsProjectFacade = VsProjectFacade(solutionRootDirectory)
        compilerAssetProvider = CompilerAssetProvider(sourceDirectory)

        self._compilerAssetProvider = compilerAssetProvider
        self._outputProviderRegistry = OutputProviderRegistry(vsProjectFacade, compilerAssetProvider)
        self._sourceDefinitionFileTypeSniffer = SourceDefinitionFileTypeSniffer()
        self._parserRegistry = DbObjectParserRegistry()

    def compile(self, makefileName: str = 'makefile') -> None:
        db = self._parse(makefileName)
        self._output(db)

    def _parse(self, makefileName: str) -> Db:
        #1 read makefile
        makefileInfo = self._readMakefile(makefileName)
        if not makefileInfo:
            raise ValueError('No contents found in makefile!')
        
        #2 read mapping file
        mapping = self._readDbMapping(makefileInfo)
        if not mapping:
            raise ValueError('No contents found in mapping file!')

        #3 discover source files
        sourceDefinitionFiles = self._discoverSourceDefinitionFiles(makefileInfo)
        if not sourceDefinitionFiles:
            raise ValueError('No source definition files found!')

        #4 read and parse source files
        objects = self._readDefinitionObjects(sourceDefinitionFiles, mapping)

        return Db(makefileInfo, 
            mapping, 
            objects)

    def _readMakefile(self, makefileName: str) -> MakefileInfo:
        makefileParser = MakefileParser()
        makefilePath = self._getSourceFilePath(makefileName)
        return makefileParser.parse(makefilePath)

    def _getSourceFilePath(self, fileName: str) -> str:
        return self._compilerAssetProvider.getSourceFilePath(fileName)

    def _readDbMapping(self, makefileInfo: MakefileInfo) -> DbMapping:
        mappingFileName = makefileInfo.getMappingFileName()
        if not mappingFileName:
            raise ValueError('Mapping file expected but not specified!')

        mappingParser = DbMappingParser()
        mappingFilePath = self._getSourceFilePath(mappingFileName)
        return mappingParser.parse(mappingFilePath)

    def _discoverSourceDefinitionFiles(self, makefileInfo: MakefileInfo) -> list[str]:
        pattern = makefileInfo.getDefinitionFilesGlob()
        if not pattern:
            raise ValueError('No definition files specified!')

        return self._compilerAssetProvider.discoverFilesByPattern(pattern)

    def _readDefinitionObjects(self, sourceDefinitionFiles: list[str], mapping: DbMapping) -> list[DbObject]:
        objects = []

        for sourceDefinitionFile in sourceDefinitionFiles:
            sourceDefinitionFilePath = self._getSourceFilePath(sourceDefinitionFile)
            
            objectType = self._readDefinitionFileObjectType(sourceDefinitionFilePath)
            if not sourceDefinitionFilePath:
                raise ValueError('No object type found in definition file <' + sourceDefinitionFile + '>')

            objectParser = self._getParser(objectType, mapping)
            if not objectParser:
                raise ValueError('Invalid object type <' + objectType + '> found in definition file <' + sourceDefinitionFile + '>')

            obj = objectParser.parseFromFile(sourceDefinitionFilePath)
            if not obj:
                raise ValueError('Failed to parse definition file <' + sourceDefinitionFile + '>')

            objects.append(obj)

        return objects

    def _readDefinitionFileObjectType(self, sourceDefinitionFilePath: str) -> str:
       return self._sourceDefinitionFileTypeSniffer.readType(sourceDefinitionFilePath)

    def _getParser(self, objectType: str, mapping: DbMapping) -> DbObjectParser:
        return self._parserRegistry.createParser(objectType, mapping)

    def _output(self, db: Db) -> None:
        outputs = db.getOutputs()
        if not outputs:
            raise ValueError('No output types provided!')

        for outputInfo in outputs:
            outputProvider = self._getOutputProvider(outputInfo)
            if not outputProvider:
                raise ValueError('Invalid output type <' + outputInfo.getName() + '>')

            outputProvider.export(db)

    def _getOutputProvider(self, outputInfo: CompilerOutputInfo) -> OutputProvider:
        return self._outputProviderRegistry.createOutputProvider(outputInfo)