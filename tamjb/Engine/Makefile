# $Id$
#

TARGET = tamjb.Engine.dll

SOURCES = AssemblyInfo.cs \
        Backend.cs \
        Compressor.cs \
        Engine.cs \
        LocalFileDatabase.cs \
        MyRandom.cs \
        RecursiveScanner.cs

#
# Choose your database
#

# -- SQLite
# DBFLAGS = -define:USE_SQLITE
# DBREFS = -r Mono.Data.SqliteClient 

# -- MySql (4.1 or better for nested queries)
# DBFLAGS = -define:USE_MYSQL
# DBREFS = -r ByteFX.Data

# -- PostgreSQL
DBFLAGS = -define:USE_POSTGRESQL
DBREFS = -r Npgsql


DEBUGFLAGS = -g -define:DEBUG -define:TRACE

# libs for dependency checking during dev:
DEPLIBS = ../id3/byteheaven.id3.dll \
        ../thirdparty/Mp3Sharp.dll \
        ../thirdparty/esd-sharp.dll \
        ../Interfaces/tamjb.Interfaces.dll \
        ../SimpleMp3Player/tamjb.SimpleMp3Player.dll


REFS = -r System.Data \
        $(DBREFS) \
        $(addprefix -r:, $(DEPLIBS))

all: $(TARGET)

$(TARGET): $(DEPLIBS) $(SOURCES) $(REF_LIBS) Makefile
	mcs $(DBFLAGS) $(DEBUGFLAGS) $(LIBPATH) $(REFS) $(SOURCES) \
                -target:library -out:$@


