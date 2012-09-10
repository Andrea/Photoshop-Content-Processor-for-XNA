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

using System;
using System.IO;

namespace PSDImporter.PSDFile.ImageResources
{
	/// <summary>
	/// Summary description for ResolutionInfo.
	/// </summary>
	public class ResolutionInfo : ImageResource
	{
		public short HRes { get; set; }

		public short VRes { get; set; }

		/// <summary>
		/// 1=pixels per inch, 2=pixels per centimeter
		/// </summary>
		public enum ResUnit
		{
			PxPerInch = 1,
			PxPerCent = 2
		}

		public ResUnit HResUnit { get; set; }

		public ResUnit VResUnit { get; set; }

		/// <summary>
		/// 1=in, 2=cm, 3=pt, 4=picas, 5=columns
		/// </summary>
		public enum Unit
		{
			In = 1,
			Cm = 2,
			Pt = 3,
			Picas = 4,
			Columns = 5
		}

		public Unit WidthUnit { get; set; }

		public Unit HeightUnit { get; set; }

		public ResolutionInfo()
		{
			ID = (short)ResourceIDs.ResolutionInfo;
		}
		public ResolutionInfo(ImageResource imgRes)
			: base(imgRes)
		{
			BinaryReverseReader reader = imgRes.DataReader;

			HRes = reader.ReadInt16();
			HResUnit = (ResUnit)reader.ReadInt32();
			WidthUnit = (Unit)reader.ReadInt16();

			VRes = reader.ReadInt16();
			VResUnit = (ResUnit)reader.ReadInt32();
			HeightUnit = (Unit)reader.ReadInt16();

			reader.Close();
		}

		protected override void StoreData()
		{
			var stream = new MemoryStream();
			var writer = new BinaryReverseWriter(stream);

			writer.Write(HRes);
			writer.Write((Int32)HResUnit);
			writer.Write((Int16)WidthUnit);

			writer.Write(VRes);
			writer.Write((Int32)VResUnit);
			writer.Write((Int16)HeightUnit);

			writer.Close();
			stream.Close();

			Data = stream.ToArray();
		}

	}
}
