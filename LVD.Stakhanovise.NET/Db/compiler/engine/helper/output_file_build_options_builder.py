class OutputFileBuildOptionsBuilder:
    def getOutputFileBuildOptions(self) -> dict[str, str]:
        buildOptions = {}

        if hasattr(self, 'getCopyOutput'):
            copyOutput = self.getCopyOutput()
            if copyOutput is not None:
                buildOptions = { 'copy_output': copyOutput }

        return buildOptions