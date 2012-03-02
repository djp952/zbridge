//-----------------------------------------------------------------------------
// CircularBuffer
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
using System.IO;

namespace zuki.web.zbridgeweb
{
	/// <summary>
	/// Circular byte array data buffer class, based on work presented by alexreg
	/// at http://circularbuffer.codeplex.com/.  Reduced in scope and performance
	/// of the main read/write functions have been improved.  Still not inherantly
	/// thread-safe and must be externally synchronized
	/// </summary>
	public class CircularBuffer
	{
		/// <summary>
		/// Instance Constructor
		/// </summary>
		/// <param name="capacity">Size of the buffer in bytes</param>
		public CircularBuffer(int capacity)
		{
			if (capacity < 0) throw new ArgumentOutOfRangeException();

			// Initialize member variables
			m_capacity = capacity;
			m_size = 0;
			m_head = 0;
			m_tail = 0;
			m_buffer = new byte[capacity];
		}

		//---------------------------------------------------------------------
		// Methods
		//---------------------------------------------------------------------

		/// <summary>
		/// Reads a single byte from the circular buffer
		/// </summary>
		/// <returns>Head byte from the circualr buffer</returns>
		public byte Get()
		{
			if (m_size == 0) throw new InvalidOperationException("Buffer is empty");

			// Grab the head byte from the buffer
			byte val = m_buffer[m_head];

			// Move the head pointer and set the new buffer size
			m_head++;
			if (m_head == m_capacity) m_head = 0;
			m_size--;

			return val;
		}
		
		/// <summary>
		/// Reads an array of bytes from the circular buffer
		/// </summary>
		/// <param name="dest">Destination byte array</param>
		/// <param name="offset">Offset into the destination byte array</param>
		/// <param name="count">Number of bytes to write into the array</param>
		/// <returns>Actual number of bytes written into the array</returns>
		public int Get(byte[] dest, int offset, int count)
		{
			int				read = 0;			// Number of bytes read
			int				cb;					// Byte counter

			// Calculate the actual amount of data to be read
			count = Math.Min(count, m_size);
			if (count == 0) return 0;

			// Determine how many bytes to read from the head to the end of
			// the buffer array and copy that data
			cb = Math.Min(count, m_capacity - m_head);
			Buffer.BlockCopy(m_buffer, m_head, dest, offset, cb);
			read += cb;
			m_head += cb;

			if (m_head == m_capacity)
			{
				m_head = 0;						// Reset head pointer

				// If more data remains to be read, read it from the top of the buffer
				// and adjust the head to reflect the new position
				if ((count - read) > 0)
				{
					Buffer.BlockCopy(m_buffer, 0, dest, offset + read, count - read);
					m_head = count - read;
				}
			}

			m_size -= count;
			return count;
		}

		/// <summary>
		/// Writes a single byte into the circular buffer
		/// </summary>
		/// <param name="val">Value to be written into the buffer</param>
		public void Put(byte val)
		{
			if (m_size == m_capacity) throw new InternalBufferOverflowException("Buffer is full");

			// Write the new value at the tail of the buffer
			m_buffer[m_tail] = val;

			// Move the tail pointer and set the new buffer size
			m_tail++;
			if (m_tail == m_capacity) m_tail = 0;
			m_size++;
		}

		/// <summary>
		/// Writes an array of bytes into the circular buffer
		/// </summary>
		/// <param name="source">Source byte array</param>
		/// <param name="offset">Offset into the source byte array</param>
		/// <param name="count">Number of bytes to write into the buffer</param>
		/// <returns>Actual number of bytes written into the buffer</returns> 
		public int Put(byte[] source, int offset, int count)
		{
			int					written = 0;		// Number of bytes written
			int					cb;					// Byte counter

			// Calculate the actual number of bytes to write into the buffer
			count = Math.Min(count, m_capacity - m_size);
			if (count == 0) return 0;

			// Determine how many bytes to write from the tail to the end of
			// the buffer array and copy that data
			cb = Math.Min(count, m_capacity - m_tail);
			Buffer.BlockCopy(source, offset, m_buffer, m_tail, cb);
			written += cb;
			m_tail += cb;

			if (m_tail == m_capacity)
			{
				m_tail = 0;						// Reset tail pointer

				// If more data remains to be written, write it from the top of the buffer
				// and adjust the tail to reflect the new position
				if ((count - written) > 0)
				{
					Buffer.BlockCopy(source, offset + written, m_buffer, 0, count - written);
					m_tail = count - written;
				}
			}

			m_size += count;
			return count;
		}

		//---------------------------------------------------------------------
		// Properties
		//---------------------------------------------------------------------

		/// <summary>
		/// Gets the capacity of the circular buffer
		/// </summary>
		public int Capacity
		{
			get { return m_capacity; }
		}

		/// <summary>
		/// Gets the size of the data contained in the circular buffer
		/// </summary>
		public int Size
		{
			get { return m_size; }
		}

		//---------------------------------------------------------------------
		// Member Variables
		//---------------------------------------------------------------------

		private int				m_capacity;			// Buffer capacity
		private int				m_size;				// Current buffer size
		private int				m_head;				// Head (read) pointer
		private int				m_tail;				// Tail (write) pointer
		private byte[]			m_buffer;			// Contained byte buffer
	}
}
