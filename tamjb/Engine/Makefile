# $Id$
#

MCS=gmcs

TARGET = ../bin/tamjb.Engine.dll

SOURCES = AssemblyInfo.cs \
        Backend.cs \
	Bessel.cs \
        Compressor.cs \
	Denormal.cs \
	DualFIR.cs \
        Engine.cs \
        FirstOrderLowpassFilter.cs \
        FirstOrderHighpassFilter.cs \
        HighpassFilter.cs \
        IAudioProcessor.cs \
        IMonoFilter.cs \
	IMultiBandCompressor.cs \
	KaiserWindow.cs \
	Limiter.cs \
        LowpassFilter.cs \
	MathApproximation.cs \
        MyRandom.cs \
	PoorCompressor.cs \
        RecursiveScanner.cs \
        SampleFifo.cs \
        SoftClipper.cs \
        SoftKneeLimiter.cs \
	SoftKneeDevice.cs \
        StatusDatabase.cs \
	StereoCrossover.cs \
        MultiBandCompressor.cs

#
# Choose your database
#

# -- SQLite
DBFLAGS = -define:USE_SQLITE
DBREFS = -r:Mono.Data.SqliteClient

# -- MySql (4.1 or better for nested queries)
# DBFLAGS = -define:USE_MYSQL
# DBREFS = -r:ByteFX.Data

# -- PostgreSQL
# DBFLAGS = -define:USE_POSTGRESQL
# DBREFS = -r:Npgsql

# Note: WATCH_DENORMALS is pretty cpu-expensive

# DEBUGFLAGS = -debug -define:DEBUG -define:TRACE -define:WATCH_DENORMALS
DEBUGFLAGS = -debug -define:DEBUG -define:TRACE

# libs for dependency checking during dev:
DEPLIBS = ../bin/byteheaven.id3.dll \
        ../bin/Mp3Sharp.dll \
        ../bin/esd-sharp.dll \
        ../bin/tamjb.Interfaces.dll \
        ../bin/tamjb.SimpleMp3Player.dll


REFS = -r:System.Data \
        $(DBREFS) \
        $(addprefix -r:, $(DEPLIBS))

all: $(TARGET)

$(TARGET): $(DEPLIBS) $(SOURCES) $(REF_LIBS) Makefile
	$(MCS) $(DBFLAGS) $(DEBUGFLAGS) $(LIBPATH) $(REFS) $(SOURCES) \
                -target:library -out:$@


