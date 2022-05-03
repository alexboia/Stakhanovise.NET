import os
from engine.compiler import Compiler

def compileDb(sourceDirectory: str, solutionRootDirectory: str):
    compiler = Compiler(sourceDirectory, solutionRootDirectory)
    compiler.compile()

if __name__ == '__main__':
    sourceDirectory = './src'
    solutionRootDirectory = '../'
    compileDb(sourceDirectory, solutionRootDirectory)