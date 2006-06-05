# $Id$

DEBUGFLAGS = -debug -define:DEBUG -define:TRACE

# You can get the anthem sources from sourceforge, or just build
# a dll elsewhere (and hack this code)
ANTHEM_ROOT = ../../anthem
ANTHEM = bin/Anthem.dll

SRCS = tamjb.master.cs \
	index.aspx.cs \
	WebPageBase.cs \
	AssemblyInfo.cs

DEPLIBS = ../bin/tamjb.Interfaces.dll \
	$(ANTHEM)

REFS = -r:System.Configuration \
	-r:System.Runtime.Remoting \
	-r:System.Web \
        $(addprefix -r:, $(DEPLIBS)) 

TARGET = bin/webgui.dll

all: anthem $(TARGET)

#
# OK, the copying of required dll's is sort of lame here.
#
$(TARGET): $(SRCS) $(DEPLIBS) Makefile
	cp ../bin/tamjb.Interfaces.dll bin/.
	gmcs $(DEBUGFLAGS) $(REFS) -target:library -out:$@ $(SRCS)


#
# We don't need to build anthem every time, and I don't feel like messing with
# dependencies, so just work around it for now, OK?
#
anthem:
	echo "Note: to compile anthem, run make anthem_real"

anthem_real:
	gmcs -debug -define:DEBUG -r:System -r:System.Data -r:System.Web \
		$(ANTHEM_ROOT)/Anthem/*.cs \
		AnthemAssemblyInfo.cs \
		-resource:$(ANTHEM_ROOT)/Anthem/Anthem.js,Anthem.Anthem.js \
		-target:library \
		-out:$(ANTHEM)
