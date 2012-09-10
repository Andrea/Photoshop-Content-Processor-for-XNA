using System;

namespace PSDImporter.PSDFile
{
	class LengthWriter : IDisposable
	{
		private long _lengthPosition = long.MinValue;
		private readonly long _startPosition;
		private readonly BinaryReverseWriter _writer;

		public LengthWriter(BinaryReverseWriter writer)
		{
			_writer = writer;

			// we will write the correct length later, so remember 
			// the position
			_lengthPosition = _writer.BaseStream.Position;
			_writer.Write((uint)0xFEEDFEED);

			// remember the start  position for calculation Image 
			// resources length
			_startPosition = _writer.BaseStream.Position;
		}

		public void Write()
		{
			if (_lengthPosition != long.MinValue)
			{
				long endPosition = _writer.BaseStream.Position;

				_writer.BaseStream.Position = _lengthPosition;
				long length = endPosition - _startPosition;
				_writer.Write((uint)length);
				_writer.BaseStream.Position = endPosition;

				_lengthPosition = long.MinValue;
			}
		}

		public void Dispose()
		{
			Write();
		}
	}
}