# $Id$
#

# This Makefile is part of the Tam Jukebox project too, OK?
#
# This program is free software; you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation; either version 2 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program; if not, write to the Free Software
# Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
#
# Feel free to track down and contact ALL project contributors to
# negotiate other terms. Bring a checkbook.
#
#   Tom Surace <tekhedd@byteheaven.net>


LIBDIR = ../lib

LIBPATH = -L $(LIBDIR)

DEBUGFLAGS = -g -define:DEBUG -define:TRACE

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

doc: .build_docs

.build_docs: $(DEPLIBS) $(BINARY_TARGETS)
	doxygen ./doxygen.conf
	touch .build_docs
