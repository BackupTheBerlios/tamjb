#
# Tries to get the latest version of dojo from the official
# site. If it doesn't exist, you'll have to find it yourself!
#

# If/when the version gets updated, this will all break, of course!
DOJO_HTTP = http://download.dojotoolkit.org/current-stable/
DOJO_FILE = dojo-release-1.1.1.tar.gz  
DOJO_CONTENTS = dojo-release-1.1.1

DOJO_DIR = ../../webgui/dojo

all: $(DOJO_DIR)


# Yes, requires wget AND gnu tar. I'm gnu-centric!
$(DOJO_DIR): $(DOJO_FILE)
	tar xzvf $(DOJO_FILE)
	mv $(DOJO_CONTENTS) ../../webgui/dojo


$(DOJO_FILE):
	wget $(DOJO_HTTP)/$(DOJO_FILE)
