# $Id$

DEBUGFLAGS = -g -define:DEBUG -define:TRACE

REFS = 

INTERFACE_DLL = tamjb.Interfaces.dll

all: $(INTERFACE_DLL)

INTERFACE_SRCS = AssemblyInfo.cs \
        Credentials.cs \
        EngineState.cs \
        Mood.cs \
        PlayableData.cs \
        IEngine.cs \
        IPlaylistCriterion.cs \
        ITrackInfo.cs \
        ICredentials.cs \
        IMood.cs

# 
# The interfaces (IEngine et al)
#
$(INTERFACE_DLL): $(INTERFACE_SRCS) Makefile
	mcs $(DEBUGFLAGS) $(LIBPATH) -target:library -out:$@ $(INTERFACE_SRCS)

