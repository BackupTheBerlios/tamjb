#!/bin/sh

logfile=/tmp/tjblog

mono --debug bin/tamjb.Server.exe --trace --dir /var/mp3 2>&1 > $logfile
