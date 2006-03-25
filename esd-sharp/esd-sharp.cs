
//
// esd-sharp.cs 0.1.3
//
// (c) Malte Hildingson 2003
//

namespace EsdSharp 
{
	using System;
	using System.IO;
	using System.Diagnostics;
	using System.Runtime.InteropServices;

	public enum EsdBits
	{
		Eight = 0x0000,
		Sixteen = 0x0001
	}

	public enum EsdChannels
	{
		Mono = 0x0010,
		Stereo = 0x0020
	}

	public enum EsdMode
	{
		Stream = 0x0000,
		Sample = 0x0100,
		AdPcm = 0x0200
	}

	public enum EsdFunc
	{
		Play = 0x1000,
		Monitor = 0x0000,
		Record = 0x2000,
		Stop = 0x0000,
		Loop = 0x2000
	}

	public enum EsdStandbyMode
	{
		Error,
		OnStandby,
		OnAutoStandby,
		Running
	}



	public class EsdSample
	{
		private Esd esd;
		private bool cached;
		internal int id;
		internal string name;
		internal int rate;
		internal int length;

		public int Id
		{
			get { return id; }
		}

		public string Name
		{
			get { return name; }
		}

		public bool Cached
		{
			get { return cached; }
		}

		internal EsdSample(Esd esd)
		{
			this.esd = esd;
			cached = true;
		}

		~EsdSample()
		{
			if (cached)
				Free();
		}

		public void Free()
		{
			Esd.esd_sample_free(esd.socket, id);
			cached = false;
		}

		public void Play()
		{
			if (!cached)
				throw new EsdCacheException("Sample not cached", this);

			Esd.esd_sample_play(esd.socket, id);
		}

		public void Loop()
		{
			if (!cached)
				throw new EsdCacheException("Sample not cached", this);

			Esd.esd_sample_loop(esd.socket, id);
		}

		public void Stop()
		{
			if (!cached)
				throw new EsdCacheException("Sample not cached", this);

			Esd.esd_sample_stop(esd.socket, id);
		}

		[DllImport("esd")]
		internal extern static int esd_set_default_sample_pan(int esd, int sample_id,
									int left, int right);

		public void SetDefaultPan(int left, int right)
		{
			Debug.Assert(esd.IsOpen);

			esd_set_default_sample_pan(esd.socket, id, left, right);
		}

	}



	public class EsdStream
	{
		private Esd esd;
		internal int id;
		internal string name;
		internal EsdFunc func;
		private IntPtr rmem, wmem;

		public int Id
		{
			get { return id; }
		}

		public string Name
		{
			get { return name; }
		}

		public bool IsOpen
		{
			get { return id != -1; }
		}

		public EsdFunc Func
		{
			get { return func; }
		}

		internal EsdStream(Esd esd)
		{
			this.esd = esd;
			id = -1;
		}

		~EsdStream()
		{
			if (IsOpen)
				Close();

			if (rmem != IntPtr.Zero) FreeReadBuffer();
			if (wmem != IntPtr.Zero) FreeWriteBuffer();
		}

		public void Close()
		{
			Esd.esd_close(id);
			id = -1;
		}

		public void AllocReadBuffer(int size)
		{
			rmem = Marshal.AllocHGlobal(size);
		}

		public void FreeReadBuffer()
		{
			Marshal.FreeHGlobal(rmem);
			rmem = IntPtr.Zero;
		}

		public int Read(byte[] buffer, int offset, int length)
		{
			if (func != EsdFunc.Record || func != EsdFunc.Monitor)
				throw new EsdException("Stream is not readable");

			Debug.Assert(rmem != IntPtr.Zero);
			length = Esd.read(id, rmem, length);
			Marshal.Copy(rmem, buffer, offset, length);
			return length;
		}

		public void AllocWriteBuffer(int size)
		{
			wmem = Marshal.AllocHGlobal(size);
		}

		public void FreeWriteBuffer()
		{
			Marshal.FreeHGlobal(wmem);
			wmem = IntPtr.Zero;
		}

		public int Write(byte[] buffer, int offset, int length)
		{
			if (func != EsdFunc.Play)
				throw new EsdException("Stream is not writable");

			Debug.Assert(wmem != IntPtr.Zero);
			Marshal.Copy(buffer, offset, wmem, length);
			length = Esd.write(id, wmem, length);
			return length;
		}

		[DllImport("esd")]
		internal extern static int esd_set_stream_pan(int esd, int stream_id,
								int left, int right);

		public void SetPan(int left, int right)
		{
			Debug.Assert(esd.IsOpen);

			esd_set_stream_pan(esd.socket, id, left, right);
		}
	}



	public class Esd
	{
		public static readonly int BufferSize = 4 * 1024;
		public static readonly int DefaultRate = 44100;
		public static readonly int VolumeBase = 256;

		internal int socket;
		private string host; 

		public int Socket
		{
			get { return socket; }
		}

		public string Host
		{
			get { return host; }
		}

		public bool IsOpen
		{
			get { return socket != -1; }
		}

		[DllImport("esd")]
		internal extern static int esd_get_latency(int esd);

		public int Latency
		{
			get { return esd_get_latency(socket); }
		}

		[DllImport("esd")]
		internal extern static int esd_get_standby_mode(int esd);

		public EsdStandbyMode StandbyMode
		{
			get { return (EsdStandbyMode) esd_get_standby_mode(socket); }
		}

		[DllImport("esd")]
		internal extern static IntPtr esd_audio_devices();

		public static string Devices
		{
			get {
				return Marshal.PtrToStringAuto(
						esd_audio_devices());
			}
		}

		[DllImport("esd")]
		internal extern static IntPtr esd_get_socket_dirname();

		public static string DirName
		{
			get {
				return Marshal.PtrToStringAuto(
						esd_get_socket_dirname());
			}
		}

		[DllImport("esd")]
		internal extern static IntPtr esd_get_socket_name();

		public static string SocketName
		{
			get {
				return Marshal.PtrToStringAuto(
						esd_get_socket_name());
			}
		}

		public Esd() : this("localhost")
		{
		}

		public Esd(string host)
		{
			this.host = host;
			socket = -1;
		}

		~Esd()
		{
			if (IsOpen)
				Close();
		}

		[DllImport("esd")]
		internal extern static int esd_open_sound(string host);

		public void Open()
		{
			socket = esd_open_sound(host);
			if (socket < 0)
				throw new EsdException("Could not connect to host");
		}

		[DllImport("esd")]
		internal extern static int esd_send_auth(int esd);

		public void Auth()
		{
			if (esd_send_auth(socket) < 0)
				throw new EsdException("Authentication failed");
		}

		[DllImport("esd")]
		internal extern static int esd_close(int esd);

		public void Close()
		{
			esd_close(socket);
			socket = -1;
		}

		[DllImport("esd")]
		internal extern static int esd_lock(int esd);

		public void Lock()
		{
			if (esd_lock(socket) < 0)
				throw new EsdException("Lock failed");
		}

		[DllImport("esd")]
		internal extern static int esd_unlock(int esd);

		public void Unlock()
		{
			if (esd_unlock(socket) < 0)
				throw new EsdException("Unlock failed");
		}

		[DllImport("esd")]
		internal extern static int esd_standby(int esd);

		public void Standby()
		{
			if (esd_standby(socket) < 0)
				throw new EsdException("Standby failed");
		}

		[DllImport("esd")]
		internal extern static int esd_resume(int esd);

		public void Resume()
		{
			if (esd_resume(socket) < 0)
				throw new EsdException("Resume failed");
		}

		[DllImport("esd")]
		internal extern static int esd_play_file(string prefix, string filename, int fallback);

		public static void PlayFile(string filename)
		{
			if (esd_play_file(String.Empty, filename, 0) < 0)
				throw new EsdException("Could not play file");
		}

		[DllImport("esd")]
		internal extern static int esd_file_cache(int esd, string prefix,
								string filename);

		public EsdSample CacheFile(string filename)
		{
			EsdSample sample;
			int id;

			id = esd_file_cache(socket, String.Empty, filename);
			if (id < 0)
				throw new EsdCacheException("Could not cache file", null);

			sample = new EsdSample(this);
			sample.id = id;
			sample.name = filename;
			return sample;
		}

		[DllImport("libc")]
		internal extern static int read(int fd, IntPtr ptr, int len);

		[DllImport("libc")]
		internal extern static int write(int fd, IntPtr ptr, int len);

		[DllImport("esd")]
		internal extern static int esd_sample_cache(int esd, int format, int rate,
								int length, string name);

		[DllImport("esd")]
		internal extern static int esd_confirm_sample_cache(int esd);

		public EsdSample CacheSample(string name, Stream stream, EsdChannels channels,
						int rate, EsdBits bits)
		{
			byte[] buffer = new byte[BufferSize];
			int len, id, format;
			IntPtr ptr;
			EsdSample sample;

			format = (int) bits | (int) channels
					| (int) EsdMode.Stream
					| (int) EsdFunc.Play;

			id = esd_sample_cache(socket, format, rate,
						(int) stream.Length, name);
			if (id < 0)
				throw new EsdCacheException("Could not cache sample", null);

			ptr = Marshal.AllocHGlobal(BufferSize);

			while ((len = stream.Read(buffer, 0, buffer.Length)) > 0)
			{
				Marshal.Copy(buffer, 0, ptr, len);
				write(socket, ptr, len);
			}

			Marshal.FreeHGlobal(ptr);

			if (id != esd_confirm_sample_cache(socket))
				throw new EsdCacheException("Could not cache sample", null);

			sample = new EsdSample(this);
			sample.id = id;
			sample.name = name;
			return sample;
		}

		[DllImport("esd")]
		internal extern static int esd_sample_free(int esd, int sample);

		[DllImport("esd")]
		internal extern static int esd_sample_play(int esd, int sample);

		[DllImport("esd")]
		internal extern static int esd_sample_loop(int esd, int sample);

		[DllImport("esd")]
		internal extern static int esd_sample_stop(int esd, int sample);

		[DllImport("esd")]
		internal extern static int esd_sample_getid(int esd, string name);

		public int GetSampleId(string name)
		{
			int id = esd_sample_getid(socket, name);
			if (id < 0)
				throw new EsdException("Sample id lookup failed");

			return id;
		}

		[DllImport("esd")]
		internal extern static int esd_play_stream(int format, int rate,
								string host, string name);

		public EsdStream PlayStream(string name, EsdChannels channels,
						int rate, EsdBits bits)
		{
			EsdStream stream;
			int id, format;

			format = Format(bits, channels, EsdMode.Stream, EsdFunc.Play);
			id = esd_play_stream(format, rate, host, name);
			if (id < 0)
				throw new EsdException("Could not open play stream");

			return CreateStream(id, name, EsdFunc.Play);
		}

		[DllImport("esd")]
		internal extern static int esd_monitor_stream(int format, int rate,
								string host, string name);

		public EsdStream MonitorStream(string name, EsdChannels channels,
						int rate, EsdBits bits)
		{
			EsdStream stream;
			int id, format;
	
			format = Format(bits, channels, EsdMode.Stream, EsdFunc.Monitor);
			id = esd_monitor_stream(format, rate, host, name);
			if (id < 0)
				throw new EsdException("Could not open monitor stream");

			return CreateStream(id, name, EsdFunc.Monitor);
		}

		[DllImport("esd")]
		internal extern static int esd_record_stream(int format, int rate,
								string host, string name);

		public EsdStream RecordStream(string name, EsdChannels channels,
						int rate, EsdBits bits)
		{
			EsdStream stream;
			int id, format;

			format = Format(bits, channels, EsdMode.Stream, EsdFunc.Record);
			id = esd_record_stream(format, rate, host, name);
			if (id < 0)
				throw new EsdException("Could not open record stream");

			return CreateStream(id, name, EsdFunc.Record);
		}

		internal int Format(EsdBits bits, EsdChannels channels,
					EsdMode mode, EsdFunc func)
		{
			return (int) bits | (int) channels | (int) mode | (int) func;
		}

		internal EsdStream CreateStream(int id, string name, EsdFunc func)
		{
			EsdStream stream = new EsdStream(this);
			stream.id = id;
			stream.name = name;
			stream.func = func;
			return stream;
		}
	}



	public class EsdException : Exception
	{
		public EsdException()
		{
		}

		public EsdException(string message) : base (message)
		{
		}
	}

	public class EsdCacheException : EsdException
	{
		internal EsdSample sample;

		public EsdSample Sample
		{
			get { return sample; }
		}

		public EsdCacheException()
		{
		}

		public EsdCacheException(string message, EsdSample sample) : base(message)
		{
			this.sample = sample;
		}
	}
}

