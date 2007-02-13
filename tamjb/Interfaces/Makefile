# $Id$

DEBUGFLAGS = -debug -define:DEBUG -define:TRACE

REFS = 

INTERFACE_DLL = ../bin/tamjb.Interfaces.dll

all: $(INTERFACE_DLL)

INTERFACE_SRCS = AssemblyInfo.cs \
        Credentials.cs \
        EngineState.cs \
        Mood.cs \
        PlayableData.cs \
        IBackend.cs \
        IEngine.cs \
        IPlaylistCriterion.cs \
        IRecursiveScanner.cs \
        ITrackInfo.cs

# 
# The interfaces (IEngine et al)
#
$(INTERFACE_DLL): $(INTERFACE_SRCS) Makefile
	mcs $(DEBUGFLAGS) $(LIBPATH) -target:library -out:$@ $(INTERFACE_SRCS)

