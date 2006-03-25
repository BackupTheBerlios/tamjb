
using System;
using System.IO;
using EsdSharp;

public class EsdPlay
{
	public static void Main (string [] args)
	{
		FileStream fstream;
		EsdStream estream;
		Esd esd = new Esd();
		byte[] buffer = new byte[Esd.BufferSize];
		int len, leno, offs;

		if (args.Length != 1) {
			Console.WriteLine ("Usage: esd-play <wav-file>");
			return;
		}

		fstream = new FileStream(args[0], FileMode.Open);
		estream = esd.PlayStream(args[0], EsdChannels.Stereo,
						Esd.DefaultRate, EsdBits.Sixteen);
		estream.AllocWriteBuffer(Esd.BufferSize);

		offs = 0;
		while ((len = fstream.Read(buffer, offs, buffer.Length - offs)) > 0) {
			leno = len + offs;
			offs = estream.Write(buffer, 0, leno);
			//Console.WriteLine("offs=" + offs + " len=" + len + " leno-offs=" + (leno - offs));
			if (offs < 0) break;
			if (offs < leno) Array.Copy(buffer, offs, buffer, 0, leno - offs);
			offs = leno - offs;
		}

		estream.FreeWriteBuffer();
		estream.Close();
		fstream.Close();
	}
}

