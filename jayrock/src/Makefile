# 
# Tremendously sparse makefile to build jayrock using gnumake under mono.
#


# Please, somebody explain why people use nant to build the most trivial
# projects? It's not like C# has any sort of complicated dependencies.

DEFINES = -define:NET_2_0

REFERENCES = -r:System.Data -r:System.Web

SHARED_SOURCES = \
	Common/Mask.cs \
	Common/TamjbJrAssemblyInfo.cs

JAYROCK_DLL = bin/Jayrock.dll

JAYROCK_JSON_DLL = bin/Jayrock.Json.dll

# This is sort of a hack. No more than using "-recursive", I guess.
JAYROCK_SOURCES = $(shell find Jayrock -name '*.cs') \
	$(SHARED_SOURCES)

JAYROCK_JSON_SOURCES = $(shell find Jayrock.Json -name '*.cs') \
	$(SHARED_SOURCES)

all: $(JAYROCK_DLL) $(JAYROCK_JSON_DLL)


$(JAYROCK_DLL): $(JAYROCK_JSON_DLL) $(JAYROCK_SOURCES) Makefile
	gmcs $(DEFINES) $(REFERENCES) -r:$(JAYROCK_JSON_DLL) \
		 $(JAYROCK_SOURCES) \
		-target:library -out:$@

$(JAYROCK_JSON_DLL): $(JAYROCK_JSON_SOURCES) Makefile
	gmcs $(DEFINES) $(REFERENCES) \
		$(JAYROCK_JSON_SOURCES) -target:library -out:$@
