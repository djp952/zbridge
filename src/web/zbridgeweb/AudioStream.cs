//-----------------------------------------------------------------------------
// AudioStream.cs
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
//	Michael G. Brehm (original author)
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace zuki.web.zbridgeweb
{
	class AudioStream : IDisposable
	{
		//---------------------------------------------------------------------
		// Constants
		//---------------------------------------------------------------------

		/// <summary>
		/// The size of the network stream buffer
		/// </summary>
		private const int BUFFER_SIZE = 256 * 1024;		// 256 KiB

		//---------------------------------------------------------------------
		// Constructors / Finalizer
		//---------------------------------------------------------------------

		/// <summary>
		/// Instance Constructor
		/// </summary>
		/// <param name="url">URL to connect to</param>
		public AudioStream(string url)
		{
			// Initialize network stream members
			m_uri = new Uri(url);
			m_tcpClient = new TcpClient();
			m_headers = new Dictionary<string, string>();
			m_metadataInterval = -1;

			// Initialize buffer members
			m_buffer = null;
			m_stop = new ManualResetEvent(false);
		}

		/// <summary>
		/// Finalizer
		/// </summary>
		~AudioStream() { Dispose(false); }

		//---------------------------------------------------------------------
		// IDisposable Implementation
		//---------------------------------------------------------------------
		
		/// <summary>
		/// Invoked to manually dispose of this object and its resources
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Internal disposal method; disposes of managed and unmanaged resources
		/// </summary>
		/// <param name="disposing">True if not being called from finalizer</param>
		protected virtual void Dispose(bool disposing)
		{
			if (m_disposed) return;			// Object alread disposed

			// Disposing - Invoked manually, dispose managed objects
			if (disposing)
			{
				// Signal the buffer thread to stop and wait for it to do so
				if (m_bufferThread != null)
				{
					m_stop.Set();
					m_bufferThread.Join();
				}
			}

			// Dispose of unmanaged resources here as necessary

			m_disposed = true;				// Object is now disposed
		}
	
		//---------------------------------------------------------------------
		// Member Functions
		//---------------------------------------------------------------------

		/// <summary>
		/// Connects to the stream, processes headers, and begins buffering
		/// </summary>
		/// <param name="embedMetadata">Flag to include metadata if possible, check on return</param>
		public void Connect(ref bool embedMetadata)
		{
			string					header;					// A single HTTP header string

			if (m_disposed) throw new ObjectDisposedException(typeof(AudioStream).Name);
			if (m_connected) throw new InvalidOperationException("Audio stream has already been connected");

			// Attempt to to connect to the provided host and port
			m_tcpClient.Connect(m_uri.Host, m_uri.Port);

			// Set up the request HTTP headers to pass to the server
			string requestHeaders = (embedMetadata) ? 
				String.Format("GET {0} HTTP/1.0\r\nIcy-Metadata:1\r\n\r\n", m_uri.AbsolutePath) :
				String.Format("GET {0} HTTP/1.0\r\n\r\n", m_uri.AbsolutePath);
			byte[] requestHeaderBits = Encoding.UTF8.GetBytes(requestHeaders);
			m_tcpClient.GetStream().Write(requestHeaderBits, 0, requestHeaderBits.Length);

			// Read the HTTP response headers from the connection
			StreamReader sr = new StreamReader(m_tcpClient.GetStream());

			// The first header must end with "200 OK".  This allows for both HTTP and ICY responses.
			header = sr.ReadLine();
			if (!header.EndsWith("200 OK")) throw new Exception("Invalid response [" + header + "] received from server");

			// Read and process the remaining response headers, stopping when the blank one is found.
			// This should position the TCP stream at the head of the actual data stream
			header = sr.ReadLine();
			while (!String.IsNullOrEmpty(header))
			{
				// Split the header into KEY:VALUE components
				int colon = header.IndexOf(':');
				if ((colon > 0) && (header.Length > colon + 1))
				{
					string key = header.Substring(0, colon);
					string value = header.Substring(colon + 1);

					// SPECIAL HEADER: ICY-METAINT
					if (String.Compare(key, "Icy-Metaint", true) == 0) m_metadataInterval = Int32.Parse(value.Trim());

					// SPECIAL HEADER: CONTENT-TYPE
					else if (String.Compare(key, "content-type", true) == 0) m_contentType = value.Trim();

					// EVERYTHING ELSE
					else if(!m_headers.ContainsKey(key)) m_headers.Add(key, value);
				}

				header = sr.ReadLine();						// Next header
			}

			// Set the return value for embedded metadata and create the audio stream buffer
			embedMetadata = (m_metadataInterval > 0);
			m_buffer = new AudioStreamBuffer(BUFFER_SIZE, embedMetadata);
			
			// Launch the background worker thread to begin buffering the audio data
			m_bufferThread = new Thread(new ThreadStart(BufferThreadProc));
			m_bufferThread.IsBackground = true;
			m_bufferThread.Priority = ThreadPriority.AboveNormal;
			m_bufferThread.Start();

			m_connected = true;								// Stream is now connected
		}

		/// <summary>
		/// Reads a block of bytes from the audio stream buffer
		/// </summary>
		/// <param name="buffer">Output byte array</param>
		/// <param name="offset">Offset into the output byte array</param>
		/// <param name="length">Number of bytes to read</param>
		/// <returns>Number of bytes actually read; may be zero</returns>
		public int Read(byte[] buffer, int offset, int length)
		{
			if (m_disposed) throw new ObjectDisposedException(typeof(AudioStream).Name);
			if (!m_connected) throw new InvalidOperationException("Audio stream has not been connected");

			// AudioStreamBuffer is thread-safe, just read directly from it
			return m_buffer.Read(buffer, offset, length);
		}

		/// <summary>
		/// Writes a specified amount of buffered data to an output stream
		/// </summary>
		/// <param name="output">Output stream</param>
		/// <param name="length">Number of bytes to write to the output stream</param>
		/// <returns>Number of bytes written to the stream; may be zero</returns>
		public int WriteTo(Stream output, int length)
		{
			if (m_disposed) throw new ObjectDisposedException(typeof(AudioStream).Name);
			if (!m_connected) throw new InvalidOperationException("Audio stream has not been connected");

			// Make sure this is a writable stream
			if (!output.CanWrite) return 0;

			// Allocate a temporary buffer and read the data into it
			byte[] buffer = new byte[length];
			int read = Read(buffer, 0, length);

			// Write the data to the output stream
			if(read > 0) output.Write(buffer, 0, read);
			return read;
		}

		//---------------------------------------------------------------------
		// Properties
		//---------------------------------------------------------------------

		/// <summary>
		/// Returns the Content-Type header value for the network stream
		/// </summary>
		public String ContentType
		{
			get 
			{
				if (m_disposed) throw new ObjectDisposedException(typeof(AudioStream).Name);
				if (!m_connected) throw new InvalidOperationException("Audio stream has not been connected");
				return String.IsNullOrEmpty(m_contentType) ? "audio/mpeg" : m_contentType; 
			}
		}

		/// <summary>
		/// Indicates if metadata for the source stream is available
		/// </summary>
		public bool HasMetadata
		{
			get
			{
				if (m_disposed) throw new ObjectDisposedException(typeof(AudioStream).Name);
				if (!m_connected) throw new InvalidOperationException("Audio stream has not been connected");
				return (m_metadataInterval > 0);
			}
		}

		/// <summary>
		/// Returns a collection of original response headers that were not consumed by this class
		/// </summary>
		public Dictionary<string, string> Headers
		{
			get
			{
				// Return a copy of the headers collection, not the collection itself
				if (m_disposed) throw new ObjectDisposedException(typeof(AudioStream).Name);
				if (!m_connected) throw new InvalidOperationException("Audio stream has not been connected");
				return new Dictionary<string, string>(m_headers);
			}
		}

		/// <summary>
		/// Indicates if the source stream is still connected
		/// </summary>
		public bool IsConnected
		{
			get
			{
				if (m_disposed) throw new ObjectDisposedException(typeof(AudioStream).Name);
				if (!m_connected) throw new InvalidOperationException("Audio stream has not been connected");
				return m_tcpClient.Connected;
			}
		}

		/// <summary>
		/// Gets the interval of metadata in the raw output stream
		/// </summary>
		public int MetadataInterval
		{
			get 
			{
				if (m_disposed) throw new ObjectDisposedException(typeof(AudioStream).Name);
				if (!m_connected) throw new InvalidOperationException("Audio stream has not been connected");
				return m_buffer.MetadataInterval;
			}
		}

		//---------------------------------------------------------------------
		// Private Member Functions
		//---------------------------------------------------------------------

		/// <summary>
		/// Worker thread that loads the buffer with streamed network data
		/// </summary>
		private void BufferThreadProc()
		{
			try
			{
				// Call an appropriate handler based on the presence of metadata or not
				if (m_metadataInterval > 0) BufferMetadataStream(m_tcpClient.GetStream());
				else BufferRawStream(m_tcpClient.GetStream());
			}

			// For now, just allow the connection to close on a worker exception.  There
			// isn't any good way to let the client know a bad thing happened, although
			// some form of logging would certainly be a good idea
			//catch (Exception) { }

			// Always close the source connection regardless of how this function exits
			finally { m_tcpClient.Close(); }
		}

		/// <summary>
		/// Buffers a stream with embedded metadata
		/// </summary>
		/// <param name="stream">Data stream</param>
		private void BufferMetadataStream(NetworkStream stream)
		{
			byte[]		buffer = new byte[4096];			// 4 KiB read buffer
			int			metacount = m_metadataInterval;		// Count to next metadata

			// Continually process data until the thread is told to stop
			while (!m_stop.WaitOne(0))
			{
				// Read up to 4096 bytes of data, or whatever amount will align the
				// stream at the next metadata interval
				int read = stream.Read(buffer, 0, Math.Min(4096, metacount));

				// Write the data to the buffer.  If the entire block wasn't written,
				// the buffer is full so sleep a bit to try and allow it to free up
				int written = 0;
				while (written < read)
				{
					written += m_buffer.Write(buffer, written, read - written);
					if (written != read) Thread.Sleep(100);
					if (m_stop.WaitOne(0)) break;
				}

				// Check to see if we have reached the metadata interval, and if so
				// consume that data and reset the interval counter
				metacount -= read;
				if (metacount == 0)
				{
					ProcessMetadata(stream);
					metacount = m_metadataInterval;
				}
			}
		}

		/// <summary>
		/// Buffers a raw data stream
		/// </summary>
		/// <param name="stream">Data stream</param>
		private void BufferRawStream(NetworkStream stream)
		{
			byte[] buffer = new byte[4096];			// 4 KiB read buffer

			// Continually process data until the thread is told to stop
			while (!m_stop.WaitOne(0))
			{
				// Read up to 4096 bytes from the network stream
				int read = stream.Read(buffer, 0, 4096);

				// Write the data to the buffer.  If the entire block wasn't written,
				// the buffer is full so sleep a bit to try and allow it to free up
				int written = 0;
				while (written < read)
				{
					written += m_buffer.Write(buffer, written, read - written);
					if (written != read) Thread.Sleep(100);
					if (m_stop.WaitOne(0)) break;
				}
			}
		}

		/// <summary>
		/// Processes metadata from an audio stream
		/// </summary>
		/// <param name="stream">Open stream with the metadata to process</param>
		private void ProcessMetadata(NetworkStream stream)
		{
			// Read the metadata length byte from the stream.  Zero indicates that
			// there is no change in the metadata and there is nothing more to do
			int metalength = stream.ReadByte();
			if (metalength == 0) return;

			// The length byte actually indicates how many 16-byte blocks are consumed
			metalength *= 16;
			byte[] metabuf = new byte[metalength];

			// Read all of the metadata from the stream
			int read = 0;
			do { read += stream.Read(metabuf, read, metalength - read); }
			while (read < metalength);

			// Set the new metadata as a string to the stream buffer class
			m_buffer.SetMetadata(Encoding.UTF8.GetString(metabuf).TrimEnd(new char[] { '\0' }));
		}

		//---------------------------------------------------------------------
		// Member Variables
		//---------------------------------------------------------------------

		private	bool			m_disposed = false;		// Object disposal flag
		private bool			m_connected = false;	// Stream connected flag

		// NETWORK STREAM
		private	Uri				m_uri;					// Stream URI
		private TcpClient		m_tcpClient;			// Connection object
		private int				m_metadataInterval;		// Stream metadata interval
		private string			m_contentType;			// Content-Type
		private Dictionary<string, string>	m_headers;	// Original headers

		// BUFFER
		private AudioStreamBuffer		m_buffer;			// Data buffer
		private Thread					m_bufferThread;		// Worker thread
		private ManualResetEvent		m_stop;				// Stop event
	}
}
