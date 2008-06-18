# 
# Tremendously sparse makefile to build jayrock using gnumake under mono.
#


# Please, somebody explain why people use nant to build the most trivial
# projects? It's not like C# has any sort of complicated dependencies.


REFERENCES = -r:System.Data -r:System.Web

SHARED_SOURCES = \
	Common/AssemblyInfo.cs \
	Common/Mask.cs

JAYROCK_DLL = bin/Jayrock.dll

JAYROCK_JSON_DLL = bin/Jayrock.Json.dll


all: $(JAYROCK_DLL) $(JAYROCK_JSON_DLL)


$(JAYROCK_DLL): $(JAYROCK_JSON_DLL)
	gmcs $(REFERENCES) -r:$(JAYROCK_JSON_DLL) -recurse:'Jayrock/*.cs' \
		-target:library -out:$@

$(JAYROCK_JSON_DLL): 
	gmcs $(REFERENCES) \
		$(SHARED_SOURCES) -recurse:'Jayrock.Json/*.cs' \
		-target:library -out:$@
