import os

class PathResolver:
    _rootDirectory: str = None

    def __init__(self, rootDirectory: str) -> None:
        self._rootDirectory = os.path.realpath(rootDirectory)

    def resolveDirectory(self, relativeDirectory: str) -> str:
        relativeDirectory = relativeDirectory.replace('/', os.path.sep)
        return os.path.join(self._rootDirectory, relativeDirectory)

    def getRootDirectory(self) -> str:
        return self._rootDirectory