# $Id$

LIBDIR = ../lib

LIBPATH = -L $(LIBDIR)

DEBUGFLAGS = -g -define:DEBUG

REFS = -r Mono.Data.SqliteClient.dll \
        -r tam.Interfaces \
        -r tam.LocalFileDatabase \
        -r tam.SimpleMp3Player \
        -r System.Data.dll

ENGINE_SRCS = FileSelector.cs \
        Engine.cs \
        EngineState.cs

# libs for dependency checking during dev:
DEPLIBS = $(LIBDIR)/tam.LocalFileDatabase.dll \
        $(LIBDIR)/tam.SimpleMp3Player.dll \
        $(LIBDIR)/mp3IdkonvertUtil.dll

BINARY_TARGETS = $(LIBDIR)/tam.Engine.dll

all: dll_subdirs $(BINARY_TARGETS) exe_subdirs doc

dll_subdirs:
	$(MAKE) --directory=Interfaces
	$(MAKE) --directory=LocalFileDatabase
	$(MAKE) --directory=SimpleMp3Player

exe_subdirs:
	$(MAKE) --directory=Server
	$(MAKE) --directory=GtkPlayer


#
# The player backend Engine object
# 
$(LIBDIR)/tam.Engine.dll: $(ENGINE_SRCS)
	mcs $(DEBUGFLAGS) $(LIBPATH) $(REFS) -target:library \
                -out:$@ $(ENGINE_SRCS)

#
# A command-line program, mainly for development and testing (obviously
# the real file scanner will be a lib and linked into the main program
# and stuff). Hmm.
#

FileScan.exe: FileScan.cs $(DEPLIBS)
	mcs $(DEBUGFLAGS) $(LIBPATH) $(REFS) -out:$@ FileScan.cs 


doc: .build_docs

.build_docs: $(DEPLIBS) $(BINARY_TARGETS)
	doxygen ./doxygen.conf
	touch .build_docs
