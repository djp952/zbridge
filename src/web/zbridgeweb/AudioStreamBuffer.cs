//-----------------------------------------------------------------------------
// AudioStreamBuffer
//
// zBridge - ZukiSoft SoundBridge Streaming Audio Server
//
// The use and distribution terms for this software are covered by the
// Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
// which can be found in the file CPL.TXT at the root of this distribution.
// By using this software in any fashion, you are agreeing to be bound by
// the terms of this license. You must not remove this notice, or any other,
// from this software.
//
// Contributor(s):
//	alexreg (original author)
//	Michael G. Brehm
//-----------------------------------------------------------------------------

using System;
using System.Text;
using System.Threading;

namespace zuki.web.zbridgeweb
{
	public class AudioStreamBuffer
	{
		/// <summary>
		/// Interval in which metadata is inserted into the audio stream
		/// </summary>
		private const int METADATA_INTERVAL = 8192;

		/// <summary>
		/// Instance Constructor
		/// </summary>
		/// <param name="capacity">Size of the stream buffer</param>
		/// <param name="embedMetadata">Flag to include metadata or not when buffering</param>
		public AudioStreamBuffer(int capacity, bool embedMetadata)
		{
			// Initialize member variables
			m_buffer = new CircularBuffer(capacity);
			m_embedMetadata = embedMetadata;
			m_metadataChanged = 0;
			m_metadataSync = new object();
			m_metadata = new byte[0];
			m_metacount = METADATA_INTERVAL;
		}

		//---------------------------------------------------------------------
		// Member Functions
		//---------------------------------------------------------------------

		/// <summary>
		/// Reads a block of data from the audio stream buffer
		/// </summary>
		/// <param name="buffer">Destination buffer</param>
		/// <param name="offset">Offset into the destination buffer to read</param>
		/// <param name="count">Number of bytes to read into the destination buffer</param>
		/// <returns></returns>
		public int Read(byte[] buffer, int offset, int count)
		{
			// No special processing is required here, just read as much
			// of the requested amount of data as possible from the buffer.
			lock (m_buffer)
			{
				return m_buffer.Get(buffer, offset, count);
			}
		}

		/// <summary>
		/// Sets the string that will be inserted into the next metadata slot
		/// </summary>
		/// <param name="metadata"></param>
		public void SetMetadata(string metadata)
		{
			// Convert the string metadata into an aligned byte[] buffer
			byte[] encoded = new byte[AlignMetadataBufferSize(metadata.Length, 16)];

			// If anything goes wrong with the conversion, just bail out
			try { Encoding.ASCII.GetBytes(metadata, 0, metadata.Length, encoded, 0); }
			catch { return; }

			// Replace the existing metadata buffer and set the change flag
			lock (m_metadataSync)
			{
				m_metadata = encoded;
				Interlocked.Exchange(ref m_metadataChanged, 1);
			}
		}

		/// <summary>
		/// Writes a block of raw audio stream data to the stream buffer
		/// </summary>
		/// <param name="buffer">Source byte array to read from</param>
		/// <param name="offset">Offset into the source buffer to read</param>
		/// <param name="count">Number of bytes to write from the source buffer</param>
		/// <returns></returns>
		public int Write(byte[] buffer, int offset, int count)
		{
			// Call into the appropriate handler based on if metadata should be
			// embedded into the stream or not
			return (m_embedMetadata) ? WriteMetadataStream(buffer, offset, count) : 
				WriteRawStream(buffer, offset, count);
		}

		//---------------------------------------------------------------------
		// Properties
		//---------------------------------------------------------------------

		/// <summary>
		/// Gets the interval of metadata in the raw output stream
		/// </summary>
		public int MetadataInterval
		{
			get { return (m_embedMetadata) ? METADATA_INTERVAL : 0; }
		}

		//---------------------------------------------------------------------
		// Private Member Functions
		//---------------------------------------------------------------------

		/// <summary>
		/// Aligns a metadata buffer size value
		/// </summary>
		/// <param name="size">Metadata buffer size to be aligned</param>
		/// <param name="alignment">Alignment</param>
		/// <returns>The aligned metadata buffer size</returns>
		private static int AlignMetadataBufferSize(int size, int alignment)
		{
			if (size == 0) return alignment;
			return size + alignment - (size % alignment);
		}

		/// <summary>
		/// Writes raw audio stream data into the buffer
		/// </summary>
		/// <param name="buffer">Source byte array</param>
		/// <param name="offset">Offset into the source byte array</param>
		/// <param name="count">Number of bytes to read from the array</param>
		/// <returns>Number of bytes successfully written to the stream</returns>
		private int WriteRawStream(byte[] buffer, int offset, int count)
		{
			// Streams that do not require metadata are simply buffered as-is
			lock (m_buffer)
			{
				return m_buffer.Put(buffer, offset, count);
			}
		}

		/// <summary>
		/// Writes audio data into the buffer and includes metadata
		/// </summary>
		/// <param name="buffer">Source byte array</param>
		/// <param name="offset">Offset into the source byte array</param>
		/// <param name="count">Number of bytes to read from the array</param>
		/// <returns>Number of bytes successfully written to the stream</returns>
		private int WriteMetadataStream(byte[] buffer, int offset, int count)
		{
			// Determine how much buffer space is actually available.  Note that
			// this doesn't need to be synchronized as it's safe to assume only
			// calls to .Read() that would make the buffer larger would be happening
			// on a thread other than this one for this particular application
			int available = m_buffer.Capacity - m_buffer.Size;

			// Determine the number of bytes to write into the buffer
			count = Math.Min(Math.Min(count, available), m_metacount);

			lock (m_metadataSync)
			{
				// Determine if metadata will need to be written and ensure there
				// will be enough space in the buffer to do so before continuing
				if ((m_metacount - count) == 0)
				{
					if ((count + m_metadata.Length + 1) > available) return 0;	// +1 = size byte
				}

				lock (m_buffer)
				{
					// Write the calculated number of raw bytes into the buffer
					if(m_buffer.Put(buffer, offset, count) != count) 
						throw new Exception("AudioStreamBuffer: Internal buffer was not of the expected length");

					// Check to see if the buffer has been aligned for metadata and write it
					m_metacount -= count;
					if (m_metacount == 0)
					{
						// If the metadata has not changed since the last write, just insert a single zero
						if (m_metadataChanged == 0) m_buffer.Put(0);
						else
						{
							// Otherwise, a byte containing (metadata length / 16) must be written into the 
							// buffer followed by the actual aligned metadata information buffer
							m_buffer.Put((byte)(m_metadata.Length / 16));
							if(m_buffer.Put(m_metadata, 0, m_metadata.Length) != m_metadata.Length)
								throw new Exception("AudioStreamBuffer: Internal buffer was not of the expected length");

							Interlocked.Exchange(ref m_metadataChanged, 0);		// Reset metadata change flag
						}

						m_metacount = METADATA_INTERVAL;						// Reset metadata counter value
					}
				}
			}

			return count;		// Return actual raw bytes written, excludes metadata
		}

		//---------------------------------------------------------------------
		// Member Variables
		//---------------------------------------------------------------------

		CircularBuffer			m_buffer;				// Circular data buffer
		bool					m_embedMetadata;		// Flag to embed metadata
		int						m_metadataChanged;		// Flag if metadata changed
		object					m_metadataSync;			// Synchronization object
		byte[]					m_metadata;				// Next block of metadata
		int						m_metacount;			// Count to next metadata write
	}
}
