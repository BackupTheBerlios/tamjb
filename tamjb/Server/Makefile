# $Id$

MCS=gmcs

DEBUGFLAGS = -debug -define:DEBUG -define:TRACE

SERVER = ../bin/tamjb.Server.exe

SRCS = Server.cs

#
# libs that are part of this project (or, anyway, not system files)
#
DEPLIBS = ../bin/Mp3Sharp.dll \
        ../bin/esd-sharp.dll \
        ../bin/byteheaven.id3.dll \
        ../bin/tamjb.Interfaces.dll \
        ../bin/tamjb.SimpleMp3Player.dll \
        ../bin/tamjb.Engine.dll

SERVER_REFS = \
	-r:System.Configuration \
	-r:System.Runtime.Remoting \
        $(addprefix -r:, $(DEPLIBS))

all: $(SERVER)

$(SERVER): $(SRCS) $(DEPLIBS)
	$(MCS) $(DEBUGFLAGS) $(LIBPATH) $(SERVER_REFS) -out:$@ $(SRCS)

