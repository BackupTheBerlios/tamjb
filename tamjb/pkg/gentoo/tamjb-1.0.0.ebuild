# Copyright 1999-2004 Gentoo Foundation
# Distributed under the terms of the GNU General Public License v2
# $Header$

# (Yeah, I guess gentoo foundation can have this little text file.)
# (It's the least I can do.)

# ebuild for TamJukebox

LICENSE="GPL-2"
HOMEPAGE="http://tamjb.sourceforge.net/"

# There can be only one. Well, actually there can be thousands, but
# there _is_ only one.
SLOT="0"

# This should build/run on any platform with .NET. No, seriously.
KEYWORDS="~x86"

# This is intended to be temporary
MY_P="tamjb-2004-08-03-src"

# Note: could dynamically switch between sqlite/postgres if that
# support were added to the makefile.
IUSE=""

# Note: should work with .NET-style compiles/runtimes
# Note: gtk-sharp could be optional if there were another GUI.
# ditto glade-sharp
DEPEND=">=dev-dotnet/mono-1.0.4
	>=dev-dotnet/gtk-sharp-1.0.4
	>=dev-dotnet/glade-sharp-1.0.4
	>=dev-db/sqlite-2.8.11
	media-sound/esound"

SRC_URI="mirror://sourceforge/tamjb/${MY_P}.zip"

src_compile() {
	make
}

src_install() {
	exit 1
}

