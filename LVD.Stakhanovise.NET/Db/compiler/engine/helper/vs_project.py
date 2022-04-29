import os
from xml.etree.ElementTree import parse
from xml.etree.ElementTree import indent
from xml.etree.ElementTree import ElementTree
from xml.etree.ElementTree import Element

class VsProject:
    _filePath: str = None
    _projectTree: ElementTree = None
    _projectRoot: Element = None

    def __init__(self, filePath: str) -> None:
        self._filePath = filePath

    def open(self) -> None:
        if self._projectTree is None:
            self._projectTree = parse(self._filePath)
            self._projectRoot = self._projectTree.getroot()

    def write(self) -> None:
        if self._projectTree is not None:
            indent(self._projectRoot, space = '\t', level = 0)
            self._projectTree.write(self._filePath, 'utf-8')

    def close(self) -> None:
        if self._projectTree is not None:
            self._projectTree.write(self._filePath, 'utf-8')
            self._projectTree = None
            self._projectRoot = None

    def isOpen(self) -> bool:
        return self._projectRoot is not None

    def includeFilesToItemGroup(self, itemGroup: str, filePaths: list[str], buildAction: str) -> None:
        if not self.isOpen():
            self.open()

        itemGroupElement = self._findItemGroupByLabel(itemGroup)
        if itemGroupElement is None:
            itemGroupElement = Element('ItemGroup')
            itemGroupElement.attrib['Label'] = itemGroup
            self._projectRoot.append(itemGroupElement)

        for filePath in filePaths:
            filePath = self._prepareFilePath(filePath)
            self._removeFileItemInGroup(itemGroupElement, filePath)
            self._addFileItemInGroup(itemGroupElement, filePath, buildAction)

    def _findItemGroupByLabel(self, itemGroup: str) -> Element:
        foundElement = None
        itemGroupElements = self._findAllItemGroups()

        for itemGroupElement in itemGroupElements:
            label = itemGroupElement.attrib.get('Label', '')
            if label == itemGroup:
                foundElement = itemGroupElement
                break

        return foundElement

    def _removeFileItemInGroup(self, itemGroupElement: Element, filePath: str) -> None:
        removeElements = []
        for element in itemGroupElement.findall('*'):
            includeOrRemove = element.attrib.get('Include', None)
            if includeOrRemove is None:
                includeOrRemove = element.attrib.get('Remove', None)

            if includeOrRemove is not None and includeOrRemove.lower() == filePath.lower():
                removeElements.append(element)

        for removeElement in removeElements:
            itemGroupElement.remove(removeElement)

        removeElements = []

    def _addFileItemInGroup(self, itemGroupElement: Element, filePath: str, buildAction: str) -> None:
        fileItem = Element(buildAction)
        fileItem.attrib['Include'] = filePath
        itemGroupElement.append(fileItem)

    def _prepareFilePath(self, filePath: str) -> str:
        return filePath.replace('/', os.path.sep)

    def _findAllItemGroups(self) -> list[Element]:
        return self._projectRoot.findall('ItemGroup') or []

    def removeItemGroup(self, itemGroup: str) -> None:
        if not self.isOpen():
            self.open()

        itemGroupElement = self._findItemGroupByLabel(itemGroup)
        if itemGroupElement is not None:
            self._projectRoot.remove(itemGroupElement)