
all:
	mcs -target:library -out:esd-sharp.dll esd-sharp.cs
	mcs -r:esd-sharp.dll -out:esd-play.exe esd-play.cs

clean:
	rm -f esd-sharp.dll esd-play.exe

