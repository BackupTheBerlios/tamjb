# $Id$
# A simple unix makefile for mp3sharp. Whee.

MP3SHARPDIR = ../../../mp3sharp/mp3sharp

TARGET = ../Mp3Sharp.dll

# Should the engine throw an exception when it loses sync?
#   -define:THROW_ON_SYNC_LOSS

DEBUGFLAGS = -g -define:DEBUG -define:TRACE -define:THROW_ON_SYNC_LOSS

# Note: LayerIIDecoder.cs won't compile with mcs 0.29.99

# Mp3Stream has changed enough that I am maintaining it as a local
# package unless it gets merged in. Mp3Sharp still needs patches.

SOURCES_LOCAL = AssemblyInfo.cs

SOURCES_EXTERN = Mp3Stream.cs \
 converter/RiffFile.cs\
 converter/WaveFile.cs\
 converter/WaveFileObuffer.cs\
 decoder/BackStream.cs\
 decoder/BitReserve.cs\
 decoder/Bitstream.cs\
 decoder/BitstreamErrors.cs\
 decoder/BitstreamException.cs\
 decoder/Control.cs\
 decoder/Crc16.cs\
 decoder/Decoder.cs\
 decoder/DecoderErrors.cs\
 decoder/DecoderException.cs\
 decoder/Equalizer.cs\
 decoder/FrameDecoder.cs\
 decoder/Header.cs\
 decoder/huffcodetab.cs\
 decoder/InputStreamSource.cs\
 decoder/JavaLayerError.cs\
 decoder/JavaLayerErrors.cs\
 decoder/JavaLayerException.cs\
 decoder/JavaLayerHook.cs\
 decoder/JavaLayerUtils.cs\
 decoder/LayerIDecoder.cs\
 decoder/LayerIIDecoder.cs\
 decoder/LayerIIIDecoder.cs\
 decoder/Manager.cs\
 decoder/Obuffer.cs\
 decoder/OutputChannels.cs\
 decoder/SampleBuffer.cs\
 decoder/Source.cs\
 decoder/SynthesisFilter.cs\
 Support/SupportClass.cs

REFERENCES = -r System \
        -r System.Data \
        -r System.Management

SOURCES_REAL = $(addprefix $(MP3SHARPDIR)/, $(SOURCES_EXTERN)) \
        $(SOURCES_LOCAL)

all: $(TARGET)

$(TARGET): $(SOURCES_REAL) Makefile
	mcs $(DEBUGFLAGS) $(REFERENCES) -target:library -out:$@ $(SOURCES_REAL)

