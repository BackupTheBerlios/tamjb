# $Id$

LIBDIR = ../../lib

LIBPATH = -L $(LIBDIR)

DEBUGFLAGS = -g -define:DEBUG -define:TRACE

REFS = 

INTERFACE_DLL = $(LIBDIR)/tam.Interfaces.dll

all: $(INTERFACE_DLL)

INTERFACE_SRCS =  IEngine.cs \
        IPlaylistCriterion.cs \
        ITrackInfo.cs

# 
# The interfaces (IEngine et al)
#
$(INTERFACE_DLL): $(INTERFACE_SRCS)
	mcs $(DEBUGFLAGS) $(LIBPATH) -target:library -out:$@ $(INTERFACE_SRCS)

