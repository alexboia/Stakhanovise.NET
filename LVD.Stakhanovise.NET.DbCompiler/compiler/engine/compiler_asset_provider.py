﻿import os
import glob
from .helper.path_resolver import PathResolver

class CompilerAssetProvider:
    _pathResolver: PathResolver = None
    
    def __init__(self, sourceDirectory: str) -> None:
        self._pathResolver = PathResolver(sourceDirectory)

    def sourceFileExists(self, relativeFilePath: str) -> bool:
        absoluteFilePath = self.getSourceFilePath(relativeFilePath)
        return os.path.exists(absoluteFilePath)

    def getSourceFileContents(self, relativeFilePath: str) -> str:
        absoluteFilePath = self.getSourceFilePath(relativeFilePath)

        if os.path.exists(absoluteFilePath):
            filePointer = open(absoluteFilePath, 'r', encoding = 'utf_8_sig')
            fileContents = filePointer.read()
            filePointer.close()
        else:
            fileContents = None

        return fileContents

    def getSourceFilePath(self, relativeFilePath: str) -> str:
        return self._pathResolver.resolvePath(relativeFilePath)

    def discoverFilesByPattern(self, pattern: str) -> list[str]:
        fileNames = []
        searchPath = self._pathResolver.resolvePath(pattern)
        foundFiles = glob.glob(searchPath)
        fileNames = map(lambda foundFile: os.path.basename(foundFile), foundFiles)
        return list(fileNames)