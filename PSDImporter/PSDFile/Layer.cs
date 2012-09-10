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
// This code contains code from the Endogine sprite engine by Jonas Beckeman.
// http://www.endogine.com/CS/
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Diagnostics;

namespace PSDImporter.PSDFile
{
	public class Layer
	{
		///////////////////////////////////////////////////////////////////////////

		public class Channel
		{
			private Layer _layer;
			/// <summary>
			/// The layer to which this channel belongs
			/// </summary>
			public Layer Layer
			{
				get { return _layer; }
			}


			/// <summary>
			/// 0 = red, 1 = green, etc.
			/// �1 = transparency mask
			/// �2 = user supplied layer mask
			/// </summary>
			public short Id { get; set; }

			/// <summary>
			/// The length of the compressed channel data.
			/// </summary>
			public int Length;

			/// <summary>
			/// The compressed raw channel data
			/// </summary>
			public byte[] Data { get; set; }

			public byte[] ImageData { get; set; }

			public ImageCompression ImageCompression { get; set; }

			//////////////////////////////////////////////////////////////////

			internal Channel(short id, Layer layer)
			{
				Id = id;
				_layer = layer;
				_layer.Channels.Add(this);
				_layer.SortedChannels.Add(this.Id, this);
			}

			internal Channel(BinaryReverseReader reader, Layer layer)
			{
				Debug.WriteLine("Channel started at " + reader.BaseStream.Position);

				Id = reader.ReadInt16();
				Length = reader.ReadInt32();

				_layer = layer;
			}

			internal void Save(BinaryReverseWriter writer)
			{
				Debug.WriteLine("Channel Save started at " + writer.BaseStream.Position.ToString());

				writer.Write(Id);

				CompressImageData();

				writer.Write(Data.Length + 2); // 2 bytes for the image compression
			}

			//////////////////////////////////////////////////////////////////

			internal void LoadPixelData(BinaryReverseReader reader)
			{
				Debug.WriteLine("Channel.LoadPixelData started at " + reader.BaseStream.Position.ToString());

				Data = reader.ReadBytes(Length);

				using (var readerImg = DataReader)
				{
					ImageCompression = (ImageCompression)readerImg.ReadInt16();

					var bytesPerRow = 0;

					switch (_layer.PsdFile.Depth)
					{
						case 1:
							bytesPerRow = _layer.Rect.Width;//NOT Shure
							break;
						case 8:
							bytesPerRow = _layer.Rect.Width;
							break;
						case 16:
							bytesPerRow = _layer.Rect.Width * 2;
							break;
					}

					ImageData = new byte[_layer.Rect.Height * bytesPerRow];

					switch (ImageCompression)
					{
						case ImageCompression.Raw:
							readerImg.Read(ImageData, 0, ImageData.Length);
							break;
						case ImageCompression.Rle:
							{
								int[] rowLenghtList = new int[_layer.Rect.Height];
								for (int i = 0; i < rowLenghtList.Length; i++)
									rowLenghtList[i] = readerImg.ReadInt16();

								for (int i = 0; i < _layer.Rect.Height; i++)
								{
									int rowIndex = i * _layer.Rect.Width;
									RleHelper.DecodedRow(readerImg.BaseStream, ImageData, rowIndex, bytesPerRow);

									//if (rowLenghtList[i] % 2 == 1)
									//  readerImg.ReadByte();
								}
							}
							break;
						default:
							break;
					}
				}
			}

			private void CompressImageData()
			{
				if (ImageCompression == ImageCompression.Rle)
				{
					MemoryStream dataStream = new MemoryStream();
					BinaryReverseWriter writer = new BinaryReverseWriter(dataStream);

					// we will write the correct lengths later, so remember 
					// the position
					long lengthPosition = writer.BaseStream.Position;

					int[] rleRowLenghs = new int[_layer.Rect.Height];

					if (ImageCompression == ImageCompression.Rle)
					{
						for (int i = 0; i < rleRowLenghs.Length; i++)
						{
							writer.Write((short)0x1234);
						}
					}

					//---------------------------------------------------------------

					int bytesPerRow = 0;

					switch (_layer.PsdFile.Depth)
					{
						case 1:
							bytesPerRow = _layer.Rect.Width;//NOT Shure
							break;
						case 8:
							bytesPerRow = _layer.Rect.Width;
							break;
						case 16:
							bytesPerRow = _layer.Rect.Width * 2;
							break;
					}

					//---------------------------------------------------------------

					for (int row = 0; row < _layer.Rect.Height; row++)
					{
						int rowIndex = row * _layer.Rect.Width;
						rleRowLenghs[row] = RleHelper.EncodedRow(writer.BaseStream, ImageData, rowIndex, bytesPerRow);
					}

					//---------------------------------------------------------------

					long endPosition = writer.BaseStream.Position;

					writer.BaseStream.Position = lengthPosition;

					for (int i = 0; i < rleRowLenghs.Length; i++)
					{
						writer.Write((short)rleRowLenghs[i]);
					}

					writer.BaseStream.Position = endPosition;

					dataStream.Close();

					Data = dataStream.ToArray();

					dataStream.Dispose();

				}
				else
				{
					Data = (byte[])ImageData.Clone();
				}
			}

			internal void SavePixelData(BinaryReverseWriter writer)
			{
				Debug.WriteLine("Channel SavePixelData started at " + writer.BaseStream.Position);

				writer.Write((short)ImageCompression);
				writer.Write(ImageData);
			}

			//////////////////////////////////////////////////////////////////

			public BinaryReverseReader DataReader
			{
				get
				{
					if (Data == null)
						return null;

					return new BinaryReverseReader(new System.IO.MemoryStream(this.Data));
				}
			}
		}

		///////////////////////////////////////////////////////////////////////////

		public class Mask
		{
			private Layer m_layer;
			/// <summary>
			/// The layer to which this mask belongs
			/// </summary>
			public Layer Layer
			{
				get { return m_layer; }
			}

			private Rectangle m_rect = Rectangle.Empty;
			/// <summary>
			/// The rectangle enclosing the mask.
			/// </summary>
			public Rectangle Rect
			{
				get { return m_rect; }
				set { m_rect = value; }
			}

			private byte m_defaultColor;
			public byte DefaultColor
			{
				get { return m_defaultColor; }
				set { m_defaultColor = value; }
			}


			private static int m_positionIsRelativeBit = BitVector32.CreateMask();
			private static int m_disabledBit = BitVector32.CreateMask(m_positionIsRelativeBit);
			private static int m_invertOnBlendBit = BitVector32.CreateMask(m_disabledBit);

			private BitVector32 m_flags = new BitVector32();
			/// <summary>
			/// If true, the position of the mask is relative to the layer.
			/// </summary>
			public bool PositionIsRelative
			{
				get
				{
					return m_flags[m_positionIsRelativeBit];
				}
				set
				{
					m_flags[m_positionIsRelativeBit] = value;
				}
			}

			public bool Disabled
			{
				get { return m_flags[m_disabledBit]; }
				set { m_flags[m_disabledBit] = value; }
			}

			/// <summary>
			/// if true, invert the mask when blending.
			/// </summary>
			public bool InvertOnBlendBit
			{
				get { return m_flags[m_invertOnBlendBit]; }
				set { m_flags[m_invertOnBlendBit] = value; }
			}

			///////////////////////////////////////////////////////////////////////////

			internal Mask(Layer layer)
			{
				m_layer = layer;
				m_layer.MaskData = this;
			}

			///////////////////////////////////////////////////////////////////////////

			internal Mask(BinaryReverseReader reader, Layer layer)
			{
				Debug.WriteLine("Mask started at " + reader.BaseStream.Position.ToString());

				m_layer = layer;

				uint maskLength = reader.ReadUInt32();

				if (maskLength <= 0)
					return;

				long startPosition = reader.BaseStream.Position;

				//-----------------------------------------------------------------------

				m_rect = new Rectangle();
				m_rect.Y = reader.ReadInt32();
				m_rect.X = reader.ReadInt32();
				m_rect.Height = reader.ReadInt32() - m_rect.Y;
				m_rect.Width = reader.ReadInt32() - m_rect.X;

				m_defaultColor = reader.ReadByte();

				//-----------------------------------------------------------------------

				byte flags = reader.ReadByte();
				m_flags = new BitVector32(flags);

				//-----------------------------------------------------------------------

				if (maskLength == 36)
				{
					BitVector32 realFlags = new BitVector32(reader.ReadByte());

					byte realUserMaskBackground = reader.ReadByte();

					Rectangle rect = new Rectangle();
					rect.Y = reader.ReadInt32();
					rect.X = reader.ReadInt32();
					rect.Height = reader.ReadInt32() - m_rect.Y;
					rect.Width = reader.ReadInt32() - m_rect.X;
				}


				// there is other stuff following, but we will ignore this.
				reader.BaseStream.Position = startPosition + maskLength;
			}

			///////////////////////////////////////////////////////////////////////////

			public void Save(BinaryReverseWriter writer)
			{
				Debug.WriteLine("Mask Save started at " + writer.BaseStream.Position.ToString());

				if (m_rect.IsEmpty)
				{
					writer.Write((uint)0);
					return;
				}

				using (new LengthWriter(writer))
				{
					writer.Write(m_rect.Top);
					writer.Write(m_rect.Left);
					writer.Write(m_rect.Bottom);
					writer.Write(m_rect.Right);

					writer.Write(m_defaultColor);

					writer.Write((byte)m_flags.Data);

					// padding 2 bytes so that size is 20
					writer.Write((int)0);
				}
			}

			//////////////////////////////////////////////////////////////////

			/// <summary>
			/// The raw image data from the channel.
			/// </summary>
			public byte[] m_imageData;

			public byte[] ImageData
			{
				get { return m_imageData; }
				set { m_imageData = value; }
			}

			internal void LoadPixelData(BinaryReverseReader reader)
			{
				Debug.WriteLine("Mask.LoadPixelData started at " + reader.BaseStream.Position.ToString());

				if (m_rect.IsEmpty || m_layer.SortedChannels.ContainsKey(-2) == false)
					return;

				Channel maskChannel = m_layer.SortedChannels[-2];


				maskChannel.Data = reader.ReadBytes((int)maskChannel.Length);


				using (BinaryReverseReader readerImg = maskChannel.DataReader)
				{
					maskChannel.ImageCompression = (ImageCompression)readerImg.ReadInt16();

					int bytesPerRow = 0;

					switch (m_layer.PsdFile.Depth)
					{
						case 1:
							bytesPerRow = m_rect.Width;//NOT Shure
							break;
						case 8:
							bytesPerRow = m_rect.Width;
							break;
						case 16:
							bytesPerRow = m_rect.Width * 2;
							break;
					}

					maskChannel.ImageData = new byte[m_rect.Height * bytesPerRow];
					// Fill Array
					for (int i = 0; i < maskChannel.ImageData.Length; i++)
					{
						maskChannel.ImageData[i] = 0xAB;
					}

					m_imageData = (byte[])maskChannel.ImageData.Clone();

					switch (maskChannel.ImageCompression)
					{
						case ImageCompression.Raw:
							readerImg.Read(maskChannel.ImageData, 0, maskChannel.ImageData.Length);
							break;
						case ImageCompression.Rle:
							{
								int[] rowLenghtList = new int[m_rect.Height];

								for (int i = 0; i < rowLenghtList.Length; i++)
									rowLenghtList[i] = readerImg.ReadInt16();

								for (int i = 0; i < m_rect.Height; i++)
								{
									int rowIndex = i * m_rect.Width;
									RleHelper.DecodedRow(readerImg.BaseStream, maskChannel.ImageData, rowIndex, bytesPerRow);
								}
							}
							break;
						default:
							break;
					}

					m_imageData = (byte[])maskChannel.ImageData.Clone();

				}
			}

			internal void SavePixelData(BinaryReverseWriter writer)
			{
				//writer.Write(m_data);
			}


			///////////////////////////////////////////////////////////////////////////

		}


		///////////////////////////////////////////////////////////////////////////

		public class BlendingRanges
		{
			private Layer m_layer;
			/// <summary>
			/// The layer to which this channel belongs
			/// </summary>
			public Layer Layer
			{
				get { return m_layer; }
			}

			private byte[] m_data = new byte[0];

			public byte[] Data
			{
				get { return m_data; }
				set { m_data = value; }
			}

			///////////////////////////////////////////////////////////////////////////

			public BlendingRanges(Layer layer)
			{
				m_layer = layer;
				m_layer.BlendingRangesData = this;
			}

			///////////////////////////////////////////////////////////////////////////

			public BlendingRanges(BinaryReverseReader reader, Layer layer)
			{
				Debug.WriteLine("BlendingRanges started at " + reader.BaseStream.Position.ToString());

				m_layer = layer;
				int dataLength = reader.ReadInt32();
				if (dataLength <= 0)
					return;

				m_data = reader.ReadBytes(dataLength);
			}

			///////////////////////////////////////////////////////////////////////////

			public void Save(BinaryReverseWriter writer)
			{
				Debug.WriteLine("BlendingRanges Save started at " + writer.BaseStream.Position.ToString());

				writer.Write((uint)m_data.Length);
				writer.Write(m_data);
			}
		}

		///////////////////////////////////////////////////////////////////////////

		public class AdjusmentLayerInfo
		{
			private Layer m_layer;
			/// <summary>
			/// The layer to which this info belongs
			/// </summary>
			internal Layer Layer
			{
				get { return m_layer; }
			}

			private string m_key;
			public string Key
			{
				get { return m_key; }
				set { m_key = value; }
			}

			private byte[] m_data;
			public byte[] Data
			{
				get { return m_data; }
				set { m_data = value; }
			}

			public AdjusmentLayerInfo(string key, Layer layer)
			{
				m_key = key;
				m_layer = layer;
				m_layer.AdjustmentInfo.Add(this);
			}

			public AdjusmentLayerInfo(BinaryReverseReader reader, Layer layer)
			{
				Debug.WriteLine("AdjusmentLayerInfo started at " + reader.BaseStream.Position.ToString());

				m_layer = layer;

				string signature = new string(reader.ReadChars(4));
				if (signature != "8BIM")
				{
					throw new IOException("Could not read an image resource");
				}

				m_key = new string(reader.ReadChars(4));

				uint dataLength = reader.ReadUInt32();
				m_data = reader.ReadBytes((int)dataLength);
			}

			public void Save(BinaryReverseWriter writer)
			{
				Debug.WriteLine("AdjusmentLayerInfo Save started at " + writer.BaseStream.Position.ToString());

				string signature = "8BIM";

				writer.Write(signature.ToCharArray());
				writer.Write(m_key.ToCharArray());
				writer.Write((uint)m_data.Length);
				writer.Write(m_data);
			}

			//////////////////////////////////////////////////////////////////

			public BinaryReverseReader DataReader
			{
				get
				{
					return new BinaryReverseReader(new System.IO.MemoryStream(this.m_data));
				}
			}
		}


		///////////////////////////////////////////////////////////////////////////

		internal PsdFile PsdFile { get; private set; }

		/// <summary>
		/// The rectangle containing the contents of the layer.
		/// </summary>
		public Rectangle Rect { get; set; }


		/// <summary>
		/// Channel information.
		/// </summary>
		private readonly List<Channel> _channels = new List<Channel>();

		public List<Channel> Channels
		{
			get { return _channels; }
		}

		private readonly SortedList<short, Channel> _sortedChannels = new SortedList<short, Channel>();
		public SortedList<short, Channel> SortedChannels
		{
			get
			{
				return _sortedChannels;
			}
		}

		private string _blendModeKey = "norm";
		/// <summary>
		/// The blend mode key for the layer
		/// </summary>
		/// <remarks>
		/// <list type="table">
		/// </item>
		/// <term>norm</term><description>normal</description>
		/// <term>dark</term><description>darken</description>
		/// <term>lite</term><description>lighten</description>
		/// <term>hue </term><description>hue</description>
		/// <term>sat </term><description>saturation</description>
		/// <term>colr</term><description>color</description>
		/// <term>lum </term><description>luminosity</description>
		/// <term>mul </term><description>multiply</description>
		/// <term>scrn</term><description>screen</description>
		/// <term>diss</term><description>dissolve</description>
		/// <term>over</term><description>overlay</description>
		/// <term>hLit</term><description>hard light</description>
		/// <term>sLit</term><description>soft light</description>
		/// <term>diff</term><description>difference</description>
		/// <term>smud</term><description>exlusion</description>
		/// <term>div </term><description>color dodge</description>
		/// <term>idiv</term><description>color burn</description>
		/// </list>
		/// </remarks>
		public string BlendModeKey
		{
			get { return _blendModeKey; }
			set
			{
				if (value.Length != 4) throw new ArgumentException("Key length must be 4");
			}
		}


		/// <summary>
		/// 0 = transparent ... 255 = opaque
		/// </summary>
		public byte Opacity { get; set; }


		/// <summary>
		/// false = base, true = non�base
		/// </summary>
		public bool Clipping { get; set; }

		private static readonly int _protectTransBit = BitVector32.CreateMask();
		private static readonly int _visibleBit = BitVector32.CreateMask(_protectTransBit);

		private BitVector32 _flags = new BitVector32();

		/// <summary>
		/// If true, the layer is visible.
		/// </summary>
		public bool Visible
		{
			get { return !_flags[_visibleBit]; }
			set { _flags[_visibleBit] = !value; }
		}


		/// <summary>
		/// Protect the transparency
		/// </summary>
		public bool ProtectTrans
		{
			get { return _flags[_protectTransBit]; }
			set { _flags[_protectTransBit] = value; }
		}


		/// <summary>
		/// The descriptive layer name
		/// </summary>
		public string Name { get; set; }

		private BlendingRanges m_blendingRangesData;
		public BlendingRanges BlendingRangesData
		{
			get { return m_blendingRangesData; }
			set { m_blendingRangesData = value; }
		}

		private Mask m_maskData;
		public Layer.Mask MaskData
		{
			get { return m_maskData; }
			set { m_maskData = value; }
		}

		private List<AdjusmentLayerInfo> m_adjustmentInfo = new List<AdjusmentLayerInfo>();
		public List<Layer.AdjusmentLayerInfo> AdjustmentInfo
		{
			get { return m_adjustmentInfo; }
			set { m_adjustmentInfo = value; }
		}

		///////////////////////////////////////////////////////////////////////////

		public Layer(PsdFile psdFile)
		{
			Rect = Rectangle.Empty;
			PsdFile = psdFile;
			PsdFile.Layers.Add(this);
		}

		public Layer(BinaryReverseReader reader, PsdFile psdFile)
		{
			Debug.WriteLine("Layer started at " + reader.BaseStream.Position.ToString());

			PsdFile = psdFile;
			Rect = new Rectangle
				       {
					       Y = reader.ReadInt32(),
					       X = reader.ReadInt32(),
					       Height = reader.ReadInt32() - Rect.Y,
					       Width = reader.ReadInt32() - Rect.X
				       };

			//-----------------------------------------------------------------------

			int numberOfChannels = reader.ReadUInt16();
			_channels.Clear();
			for (var channel = 0; channel < numberOfChannels; channel++)
			{
				var ch = new Channel(reader, this);
				_channels.Add(ch);
				_sortedChannels.Add(ch.Id, ch);
			}

			//-----------------------------------------------------------------------

			var signature = new string(reader.ReadChars(4));
			if (signature != "8BIM")
				throw (new IOException("Layer Channelheader error!"));

			_blendModeKey = new string(reader.ReadChars(4));
			Opacity = reader.ReadByte();

			Clipping = reader.ReadByte() > 0;

			//-----------------------------------------------------------------------

			byte flags = reader.ReadByte();
			_flags = new BitVector32(flags);

			//-----------------------------------------------------------------------

			reader.ReadByte(); //padding

			//-----------------------------------------------------------------------

			Debug.WriteLine("Layer extraDataSize started at " + reader.BaseStream.Position.ToString());

			// this is the total size of the MaskData, the BlendingRangesData, the 
			// Name and the AdjustmenLayerInfo
			uint extraDataSize = reader.ReadUInt32();



			// remember the start position for calculation of the 
			// AdjustmenLayerInfo size
			long extraDataStartPosition = reader.BaseStream.Position;

			m_maskData = new Mask(reader, this);
			m_blendingRangesData = new BlendingRanges(reader, this);

			//-----------------------------------------------------------------------

			long namePosition = reader.BaseStream.Position;

			Name = reader.ReadPascalString();

			int paddingBytes = (int)((reader.BaseStream.Position - namePosition) % 4);

			Debug.Print("Layer {0} padding bytes after name", paddingBytes);
			reader.ReadBytes(paddingBytes);

			//-----------------------------------------------------------------------

			m_adjustmentInfo.Clear();

			long adjustmenLayerEndPos = extraDataStartPosition + extraDataSize;
			while (reader.BaseStream.Position < adjustmenLayerEndPos)
			{
				try
				{
					m_adjustmentInfo.Add(new AdjusmentLayerInfo(reader, this));
				}
				catch
				{
					reader.BaseStream.Position = adjustmenLayerEndPos;
				}
			}


			//-----------------------------------------------------------------------
			// make shure we are not on a wrong offset, so set the stream position 
			// manually
			reader.BaseStream.Position = adjustmenLayerEndPos;
		}

		///////////////////////////////////////////////////////////////////////////

		public void Save(BinaryReverseWriter writer)
		{
			Debug.WriteLine("Layer Save started at " + writer.BaseStream.Position.ToString());

			writer.Write(Rect.Top);
			writer.Write(Rect.Left);
			writer.Write(Rect.Bottom);
			writer.Write(Rect.Right);

			//-----------------------------------------------------------------------

			writer.Write((short)_channels.Count);
			foreach (Channel ch in _channels)
				ch.Save(writer);

			//-----------------------------------------------------------------------

			string signature = "8BIM";
			writer.Write(signature.ToCharArray());
			writer.Write(_blendModeKey.ToCharArray());
			writer.Write(Opacity);
			writer.Write((byte)(Clipping ? 1 : 0));

			writer.Write((byte)_flags.Data);

			//-----------------------------------------------------------------------

			writer.Write((byte)0);

			//-----------------------------------------------------------------------

			using (new LengthWriter(writer))
			{
				m_maskData.Save(writer);
				m_blendingRangesData.Save(writer);

				long namePosition = writer.BaseStream.Position;

				writer.WritePascalString(Name);

				int paddingBytes = (int)((writer.BaseStream.Position - namePosition) % 4);
				Debug.Print("Layer {0} write padding bytes after name", paddingBytes);

				for (int i = 0; i < paddingBytes; i++)
					writer.Write((byte)0);

				foreach (AdjusmentLayerInfo info in m_adjustmentInfo)
				{
					info.Save(writer);
				}
			}
		}

	}
}
