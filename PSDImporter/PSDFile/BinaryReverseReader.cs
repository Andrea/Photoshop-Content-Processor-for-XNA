/////////////////////////////////////////////////////////////////////////////////
// Copyright (C) 2006, Frank Blumenberg
// 
// See License.txt for complete licensing and attribution information.
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
// THE SOFTWARE.
// 
/////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////
//
// This code is adapted from code in the Endogine sprite engine by Jonas Beckeman.
// http://www.endogine.com/CS/
//
/////////////////////////////////////////////////////////////////////////////////

using System.IO;

namespace PSDImporter.PSDFile
{
	/// <summary>
	/// Reads primitive data types as binary values in in big-endian format
	/// </summary>
	public class BinaryReverseReader : BinaryReader
	{
		public BinaryReverseReader(Stream a_stream)
			: base(a_stream)
		{
		}

		public override short ReadInt16()
		{
			short val = base.ReadInt16();
			unsafe
			{
				SwapBytes((byte*)&val, 2);
			}
			return val;
		}
		public override int ReadInt32()
		{
			int val = base.ReadInt32();
			unsafe
			{
				SwapBytes((byte*)&val, 4);
			}
			return val;
		}
		public override long ReadInt64()
		{
			long val = base.ReadInt64();
			unsafe
			{
				SwapBytes((byte*)&val, 8);
			}
			return val;
		}

		public override ushort ReadUInt16()
		{
			ushort val = base.ReadUInt16();
			unsafe
			{
				SwapBytes((byte*)&val, 2);
			}
			return val;
		}

		public override uint ReadUInt32()
		{
			uint val = base.ReadUInt32();
			unsafe
			{
				SwapBytes((byte*)&val, 4);
			}
			return val;
		}

		public override ulong ReadUInt64()
		{
			ulong val = base.ReadUInt64();
			unsafe
			{
				this.SwapBytes((byte*)&val, 8);
			}
			return val;
		}

		//////////////////////////////////////////////////////////////////

		public string ReadPascalString()
		{
			byte stringLength = base.ReadByte();

			char[] c = base.ReadChars(stringLength);

			if ((stringLength % 2) == 0)
				base.ReadByte();

			return new string(c);
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

