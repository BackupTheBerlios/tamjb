# 
# Tremendously sparse makefile to build jayrock using gnumake under mono.
#


# Please, somebody explain why people use nant to build the most trivial
# projects? It's not like C# has any sort of complicated dependencies.

DEFINES = -debug -define:TRACE -define:NET_2_0

REFERENCES = -r:System.Data -r:System.Web

SHARED_SOURCES = \
	Common/Mask.cs \
	Common/TamjbJrAssemblyInfo.cs

JAYROCK_DLL = bin/Jayrock.dll

JAYROCK_JSON_DLL = bin/Jayrock.Json.dll

# This is sort of a hack. No more than using "-recursive", I guess.
JAYROCK_SOURCES = $(shell find Jayrock -name '*.cs') \
	$(SHARED_SOURCES)

JAYROCK_RES = -resource:Jayrock/json.js,Jayrock.json.js

JAYROCK_JSON_SOURCES = $(shell find Jayrock.Json -name '*.cs') \
	$(SHARED_SOURCES)


all: $(JAYROCK_DLL) $(JAYROCK_JSON_DLL)

#
# Helper to install stuff into the tamjb test web dir.
#
install_tamjb: all
	cp $(JAYROCK_JSON_DLL) ../../tamjb/webgui/bin/.
	cp $(JAYROCK_JSON_DLL).mdb ../../tamjb/webgui/bin/.
	cp $(JAYROCK_DLL) ../../tamjb/webgui/bin/.
	cp $(JAYROCK_DLL).mdb ../../tamjb/webgui/bin/.
	cp -f ../www/json.js ../../tamjb/webgui/js/.

$(JAYROCK_DLL): $(JAYROCK_JSON_DLL) $(JAYROCK_SOURCES) Makefile
	gmcs $(DEFINES) $(REFERENCES) -r:$(JAYROCK_JSON_DLL) \
		$(JAYROCK_SOURCES) \
		$(JAYROCK_RES) \
		-target:library -out:$@

$(JAYROCK_JSON_DLL): $(JAYROCK_JSON_SOURCES) Makefile
	gmcs $(DEFINES) $(REFERENCES) \
		$(JAYROCK_JSON_SOURCES) -target:library -out:$@
