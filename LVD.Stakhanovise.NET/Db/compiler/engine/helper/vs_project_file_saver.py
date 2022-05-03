import os
from .path_resolver import PathResolver
from .vs_project_facade import VsProjectFacade
from .vs_project import VsProject

class VsProjectFileSaver:
    _projectFacade: VsProjectFacade = None
    _projectName: str = None
    _savedFiles: list[str] = None

    def __init__(self, projectFacade: VsProjectFacade, projectName: str) -> None:
        self._projectFacade = projectFacade
        self._projectName = projectName
        self._savedFiles = []

    def saveFile(self, fileName: str, fileContents: str) -> None:
        filePath = self._determineFilePath(fileName)
        
        self._ensureParentDirectoryExists(filePath)
        self._writeFileContents(filePath, fileContents)

        self._savedFiles.append(fileName)

    def _determineFilePath(self, fileName: str) -> str:
        return self._projectFacade.determineAbsoluteProjectFilePath(self._projectName, fileName)

    def _ensureParentDirectoryExists(self, filePath: str) -> None:
        dirPath = os.path.dirname(filePath)
        if not os.path.isdir(dirPath):
            os.mkdir(dirPath)

    def _writeFileContents(self, filePath: str, fileContents: str) -> None:
        filePointer = open(filePath, 'w', encoding='utf-8')
        filePointer.write(fileContents)
        filePointer.close()

    def commit(self, itemGroup: str, buildAction: str, options: dict[str, str] = None) -> None:
        if len(self._savedFiles) > 0:
            options = options or {}
            project = self._openProject()
            project.includeFilesToItemGroup(itemGroup, self._savedFiles, buildAction, options)
            project.close()

    def _openProject(self) -> VsProject:
        project = self._projectFacade.openProject(self._projectName)
        return project