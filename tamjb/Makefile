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
# Contacts:
#   Tom Surace <tekhedd@byteheaven.net>

RELEASE_TEXT_FILES = \
  COPYING \
  CHANGELOG \
  README \
  todo.txt \
  scripts/tamjb.init \
  scripts/tamjb.conf \
  scripts/server_wrap.sh \
  install.sh \
  gtktamjb

RELEASE_BINARY_FILES = \
  bin/esd-sharp.dll \
  bin/Mp3Sharp.dll \
  bin/byteheaven.id3.dll \
  bin/id3helper.exe \
  bin/tamjb.SimpleMp3Player.dll \
  bin/tamjb.Interfaces.dll \
  bin/tamjb.Engine.dll \
  bin/tamjb.Server.exe \
  bin/tamjb.GtkPlayer.exe \

all: assykey.snk dll_subdirs exe_subdirs

rel-binary: all
	-mkdir package 2> /dev/null
	-mkdir package/bin 2> /dev/null
	cp $(RELEASE_TEXT_FILES) package/.
	cp $(RELEASE_BINARY_FILES) package/bin/.

install: rel-binary
	cd package && ./install.sh

#
# You must manually create a key. Don't check it in!
#
assykey.snk:
	echo "Please link your personal keyfile to assykey.snk!"
	exit 1

#
# Build the various dll's in order of dependency (basically, engine last)
#
dll_subdirs:
	$(MAKE) --directory=thirdparty
	$(MAKE) --directory=id3
	$(MAKE) --directory=SimpleMp3Player
	$(MAKE) --directory=Interfaces
	$(MAKE) --directory=Engine

exe_subdirs:
	$(MAKE) --directory=Server
	$(MAKE) --directory=GtkPlayer


doc: .build_docs

.build_docs: $(DEPLIBS) $(RELEASE_BINARY_FILES) doxygen.conf
	doxygen ./doxygen.conf
	touch .build_docs
