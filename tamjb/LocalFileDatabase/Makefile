# $Id$
# Yah, makefiles with dot net. Cheesy, huh?
#

LIBDIR = ../../lib

LIBPATH = -L $(LIBDIR)

TARGET = $(LIBDIR)/tam.LocalFileDatabase.dll

SOURCES = PlaylistCriteria.cs \
        PlayableData.cs \
        LocalFileDatabase.cs

DEBUGFLAGS = -g -define:DEBUG -define:TRACE


REFS = -r Mono.Data.SqliteClient.dll \
        -r System.Data.dll \
        -r tam.Interfaces

REF_LIBS = $(LIBDIR)/tam.Interfaces.dll

all: $(TARGET)

$(TARGET): $(SOURCES) $(REF_LIBS)
	mcs $(DEBUGFLAGS) $(LIBPATH) $(REFS) $(SOURCES) -target:library -out:$@

