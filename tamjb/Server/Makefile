# $Id$

LIBDIR = ../../lib

LIBPATH = -L $(LIBDIR)

DEBUGFLAGS = -g -define:DEBUG -define:TRACE

# libs for dependency checking during dev:
DEPLIBS = $(LIBDIR)/tam.LocalFileDatabase.dll \
        $(LIBDIR)/tam.SimpleMp3Player.dll \
        $(LIBDIR)/mp3IdkonvertUtil.dll

SRCS = Server.cs \
        RecursiveScanner.cs

SERVER = tam.Server.exe

all: $(SERVER)

SERVER_REFS = -r System.Runtime.Remoting \
        -r mp3IdkonvertUtil \
        -r tam.Engine \
        -r tam.Interfaces \
        -r tam.LocalFileDatabase \
        -r tam.SimpleMp3Player

$(SERVER): $(SRCS) $(DEPLIBS)
	mcs $(DEBUGFLAGS) $(LIBPATH) $(SERVER_REFS) -out:$@ $(SRCS)

