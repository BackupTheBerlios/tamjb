#
# $Id$
#
LIBDIR = ../../lib

LIBPATH = -L $(LIBDIR)

TARGET = $(LIBDIR)/tam.SimpleMp3Player.dll

SOURCES = Player.cs \
        TrackFinishedInfo.cs

DEBUGFLAGS = -g -define:DEBUG -define:TRACE

#        -r gtk-sharp 
#        -r glib-sharp 
# -r tam.LocalFileDatabase
        
REFS = -r Mp3Sharp \
        -r EsdSharp

all: $(TARGET) CmdPlayer.exe

$(TARGET): $(SOURCES)
	mcs $(DEBUGFLAGS) $(LIBPATH) $(REFS) -target:library -out:$@ $(SOURCES)

#
# esd-sharp requires unsafe (for the things I'm doing anyway). Sorry.
#
CmdPlayer.exe: $(SOURCES) CmdLine.cs
	mcs $(DEBUGFLAGS) $(LIBPATH) $(REFS) -out:$@ \
	        -r tam.SimpleMp3Player \
	        CmdLine.cs 
