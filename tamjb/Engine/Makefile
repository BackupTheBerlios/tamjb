# $Id$
#

TARGET = tamjb.Engine.dll

SOURCES = AssemblyInfo.cs \
        PlaylistCriteria.cs \
        PlayableData.cs \
        LocalFileDatabase.cs \
        FileSelector.cs \
        Engine.cs \
        EngineState.cs

DEBUGFLAGS = -g -define:DEBUG -define:TRACE

# libs for dependency checking during dev:
DEPLIBS = ../thirdparty/Mp3Sharp.dll \
        ../thirdparty/esd-sharp.dll \
        ../Interfaces/tamjb.Interfaces.dll \
        ../SimpleMp3Player/tamjb.SimpleMp3Player.dll


REFS = -r System.Data \
        -r Mono.Data.SqliteClient \
        $(addprefix -r:, $(DEPLIBS))

all: $(TARGET)

$(TARGET): $(SOURCES) $(REF_LIBS) Makefile
	mcs $(DEBUGFLAGS) $(LIBPATH) $(REFS) $(SOURCES) -target:library -out:$@


