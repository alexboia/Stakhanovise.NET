import os

class PathResolver:
    _rootDirectory: str = None

    def __init__(self, rootDirectory: str) -> None:
        self._rootDirectory = os.path.realpath(rootDirectory)

    def resolvePath(self, relativePath: str) -> str:
        relativePath = relativePath.replace('/', os.path.sep)
        return os.path.join(self._rootDirectory, relativePath)

    def getRootDirectory(self) -> str:
        return self._rootDirectory