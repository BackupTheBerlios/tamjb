# $Id$

# For debugging
# -define:VERBOSE_DUMP=1 

DEBUGFLAGS = -g -define:DEBUG -define:TRACE


SRCS = ID3v2.cs \
        ID3v2Header.cs \
        ID3v2FrameHeader.cs \
        ID3v2_4FrameHeader.cs \
        ID3v2_3FrameHeader.cs \
        ID3v2_2FrameHeader.cs \
        ID3v2TagNotFoundException.cs \
        Mp3StreamReader.cs \
        AssemblyInfo.cs

ASSEMBLYNAME = byteheaven.id3
TARGET = ../bin/$(ASSEMBLYNAME).dll

CMDLINE = ../bin/id3helper.exe

all: $(TARGET) $(CMDLINE)

$(TARGET): $(SRCS) Makefile
	mcs $(DEBUGFLAGS) -target:library -out:$@ $(SRCS)


$(CMDLINE): $(TARGET) main.cs
	mcs $(DEBUGFLAGS) -r $(TARGET) -out:$@ main.cs 