using System.IO;

namespace PSDImporter.PSDFile
{
	/// <summary>
	/// Writes primitive data types as binary values in in big-endian format
	/// </summary>
	public class BinaryReverseWriter : BinaryWriter
	{
		public BinaryReverseWriter(Stream a_stream)
			: base(a_stream)
		{
		}

		public bool AutoFlush;

		public void WritePascalString(string s)
		{
			char[] c;
			if (s.Length > 255)
				c = s.Substring(0, 255).ToCharArray();
			else
				c = s.ToCharArray();

			base.Write((byte)c.Length);
			base.Write(c);

			int realLength = c.Length + 1;

			if ((realLength % 2) == 0)
				return;

			for (int i = 0; i < (2 - (realLength % 2)); i++)
				base.Write((byte)0);

			if (AutoFlush)
				Flush();
		}

		public override void Write(short val)
		{
			unsafe
			{
				this.SwapBytes((byte*)&val, 2);
			}
			base.Write(val);

			if (AutoFlush)
				Flush();
		}
		public override void Write(int val)
		{
			unsafe
			{
				this.SwapBytes((byte*)&val, 4);
			}
			base.Write(val);

			if (AutoFlush)
				Flush();
		}
		public override void Write(long val)
		{
			unsafe
			{
				this.SwapBytes((byte*)&val, 8);
			}
			base.Write(val);

			if (AutoFlush)
				Flush();
		}

		public override void Write(ushort val)
		{
			unsafe
			{
				this.SwapBytes((byte*)&val, 2);
			}
			base.Write(val);

			if (AutoFlush)
				Flush();
		}

		public override void Write(uint val)
		{
			unsafe
			{
				this.SwapBytes((byte*)&val, 4);
			}
			base.Write(val);

			if (AutoFlush)
				Flush();
		}

		public override void Write(ulong val)
		{
			unsafe
			{
				this.SwapBytes((byte*)&val, 8);
			}
			base.Write(val);

			if (AutoFlush)
				Flush();
		}

		//////////////////////////////////////////////////////////////////

		unsafe protected void SwapBytes(byte* ptr, int nLength)
		{
			for (long i = 0; i < nLength / 2; ++i)
			{
				byte t = *(ptr + i);
				*(ptr + i) = *(ptr + nLength - i - 1);
				*(ptr + nLength - i - 1) = t;
			}
		}
	}
}