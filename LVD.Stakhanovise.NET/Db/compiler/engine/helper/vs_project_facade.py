import os
from .string import sprintf
from .path_resolver import PathResolver
from .vs_project import VsProject

class VsProjectFacade:
    _pathResolver: PathResolver = None

    def __init__(self, solutionRoot: str) -> None:
        self._pathResolver = PathResolver(solutionRoot)

    def openProject(self, projectName: str) -> VsProject:
        filePath = self._determineProjectFilePath(projectName)
        
        if os.path.exists(filePath):
            project = VsProject(filePath)
            project.open()
            return project
        else:
            return None

    def _determineProjectFilePath(self, projectName: str) -> str:
        projectDir = self._pathResolver.resolveDirectory(projectName)
        projectFileName = sprintf('%s.csproj' % (projectName))
        return os.path.join(projectDir, projectFileName)
