# $Id$

DEBUGFLAGS = -g -define:DEBUG -define:TRACE

REFS = 

INTERFACE_DLL = tamjb.Interfaces.dll

all: $(INTERFACE_DLL)

INTERFACE_SRCS = AssemblyInfo.cs \
        IEngine.cs \
        IPlaylistCriterion.cs \
        ITrackInfo.cs

# 
# The interfaces (IEngine et al)
#
$(INTERFACE_DLL): $(INTERFACE_SRCS)
	mcs $(DEBUGFLAGS) $(LIBPATH) -target:library -out:$@ $(INTERFACE_SRCS)

