# $Id$

DEBUGFLAGS = -g -define:DEBUG -define:TRACE

SERVER = tamjb.Server.exe

SRCS = Server.cs

#
# libs that are part of this project (or, anyway, not system files)
#
DEPLIBS = ../thirdparty/Mp3Sharp.dll \
        ../thirdparty/esd-sharp.dll \
        ../id3/byteheaven.id3.dll \
        ../Interfaces/tamjb.Interfaces.dll \
        ../SimpleMp3Player/tamjb.SimpleMp3Player.dll \
        ../Engine/tamjb.Engine.dll

SERVER_REFS = -r System.Runtime.Remoting \
        $(addprefix -r:, $(DEPLIBS))

all: $(SERVER)

$(SERVER): $(SRCS) $(DEPLIBS)
	mcs $(DEBUGFLAGS) $(LIBPATH) $(SERVER_REFS) -out:$@ $(SRCS)

