#!/bin/sh

EVERYTHING="package"

make rel-binary || exit 1
scp -r $EVERYTHING tekhedd@audio600:/var/mp3/jukebox/.

# ssh tekhedd@audio600 "cd /var/mp3/jukebox/package && sudo ./install.sh"
ssh player@medtom "cd /var/mp3/jukebox/package && sudo ./install.sh"

