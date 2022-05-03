from ...helper.string_builder import StringBuilder
from ...model.db_sequence import DbSequence
from .sql_db_object_writer import SqlDbObjectWriter

class SqlDbSequenceWriter(SqlDbObjectWriter[DbSequence]):
    def __init__(self, sqlStringBuilder: StringBuilder) -> None:
        super().__init__(sqlStringBuilder)

    def write(self, dbSequence: DbSequence) -> None:
        self._sqlStringBuilder.appendLine('CREATE SEQUENCE IF NOT EXISTS public.' + dbSequence.getName())

        startValue = dbSequence.getStartValue()
        if startValue is not None:
            self._sqlStringBuilder.appendLineIndented('START WITH ' + startValue)

        incrementByValue = dbSequence.getIncrementValue()
        if incrementByValue is not None:
            self._sqlStringBuilder.appendLineIndented('INCREMENT BY ' + incrementByValue)

        minValue = dbSequence.getMinValue()
        if minValue is not None:
            self._sqlStringBuilder.appendLineIndented('MINVALUE ' + minValue)
        else:
            self._sqlStringBuilder.appendLineIndented('NO MINVALUE')

        maxValue = dbSequence.getMaxValue()
        if maxValue is not None:
            self._sqlStringBuilder.appendLineIndented('MAXVALUE ' + maxValue)
        else:
            self._sqlStringBuilder.appendLineIndented('NO MAXVALUE')

        cacheAmount = dbSequence.getCacheAmount()
        if cacheAmount is not None:
            self._sqlStringBuilder.appendLineIndented('CACHE ' + cacheAmount)

        if dbSequence.shouldCycle():
            self._sqlStringBuilder.appendLineIndented('CYCLE;')
        else:
            self._sqlStringBuilder.appendLineIndented('NO CYCLE;')

        self._sqlStringBuilder.appendEmptyLine()