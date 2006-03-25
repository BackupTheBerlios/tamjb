# $Id$

#
# Note: be sure EsdAssemblyInfo's version number matches the 
#       installed version!
#
ESDSHARPDIR = ../../../esd-sharp

TARGET = ../../bin/esd-sharp.dll

SRCS = esd-sharp.cs

SRCS_REAL = $(addprefix $(ESDSHARPDIR)/, $(SRCS)) \
        AssemblyInfo.cs

all: $(TARGET)

$(TARGET): $(SRCS_REAL) Makefile
	mcs -target:library -out:$@ $(SRCS_REAL)

clean:
	rm -f $(TARGET)

