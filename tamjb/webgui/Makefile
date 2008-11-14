# $Id$

DEBUGFLAGS = -debug -define:DEBUG -define:TRACE

SRCS = tamjb.master.cs \
	login.aspx.cs \
	moodselect.aspx.cs \
	DojoStoreRequest.cs \
	WebPageBase.cs \
	AssemblyInfo.cs

DEPLIBS = ../bin/tamjb.Interfaces.dll 

REFS = -r:System.Configuration \
	-r:System.Data \
	-r:System.Runtime.Remoting \
	-r:System.Web \
        $(addprefix -r:, $(DEPLIBS)) 

TARGET = bin/webgui.dll

all: $(TARGET)

#
# OK, the copying of required dll's is sort of lame here.
#
$(TARGET): $(SRCS) $(DEPLIBS) Makefile
	cp ../bin/tamjb.Interfaces.dll bin/.
	gmcs $(DEBUGFLAGS) $(REFS) -target:library -out:$@ $(SRCS)


