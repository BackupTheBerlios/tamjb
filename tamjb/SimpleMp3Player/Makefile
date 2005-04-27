#
# $Id$
#

DEBUGFLAGS = -g -define:DEBUG -define:TRACE

TARGET = ../bin/tamjb.SimpleMp3Player.dll

SOURCES = AssemblyInfo.cs \
        Player.cs \
        TrackFinishedInfo.cs

REF_LIBS = ../bin/Mp3Sharp.dll \
        ../bin/esd-sharp.dll
        
REFS = $(addprefix -r:, $(REF_LIBS))

all: $(TARGET) CmdPlayer.exe

$(TARGET): $(SOURCES) $(REF_LIBS) Makefile
	mcs $(DEBUGFLAGS) $(REFS) -target:library -out:$@ $(SOURCES)

#
# esd-sharp requires unsafe (for the things I'm doing anyway). Sorry.
#
CmdPlayer.exe: $(SOURCES) CmdLine.cs
	mcs $(DEBUGFLAGS) $(REFS) -r $(TARGET) -out:$@ CmdLine.cs 
