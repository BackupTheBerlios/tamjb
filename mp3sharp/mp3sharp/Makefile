# $Id$
# A simple unix makefile for mp3sharp. Whee.

# Should the engine throw an exception when it loses sync?
#   -define:THROW_ON_SYNC_LOSS

DEBUGFLAGS = -g -define:DEBUG -define:TRACE -define:THROW_ON_SYNC_LOSS

# Note: LayerIIDecoder.cs won't compile with mcs 0.29.99

SOURCES= Mp3Stream.cs\
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
 Support/AssemblyInfo.cs\
 Support/SupportClass.cs

REFERENCES = -r System \
        -r System.Data \
        -r System.Management

TARGET = Mp3Sharp.dll

all: $(TARGET)

$(TARGET): $(SOURCES)
	mcs $(DEBUGFLAGS) $(REFERENCES) -target:library -out:$@ $(SOURCES) 

