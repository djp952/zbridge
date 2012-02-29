//-----------------------------------------------------------------------------
// streamer.ashx
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
using System.Net;
using System.Text;
using System.Threading;
using System.Web;

namespace zuki.web.zbridgeweb
{
	class streamer : IHttpAsyncHandler
	{
		/// <summary>
		/// Delegate used to asynchronously invoke ProcessRequest()
		/// </summary>
		/// <param name="context"></param>
		protected delegate void ProcessRequestHandler(HttpContext context);

		//---------------------------------------------------------------------
		// Member Functions
		//---------------------------------------------------------------------

		/// <summary>
		/// Begins processing the request asynchronously
		/// </summary>
		/// <param name="context">ASP.NET context instance</param>
		/// <param name="callback">Async callback function</param>
		/// <param name="extraData">Async callback state object</param>
		/// <returns></returns>
		public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback callback, object extraData)
		{
			m_delegate = new ProcessRequestHandler(ProcessRequest);
			return m_delegate.BeginInvoke(context, callback, extraData);
		}

		/// <summary>
		/// Invoked when the asynchronous call to ProcessRequest() ends
		/// </summary>
		/// <param name="result">Asynchronous operation result object</param>
		public void EndProcessRequest(IAsyncResult result)
		{
			if (m_delegate != null) m_delegate.EndInvoke(result);
		}

		/// <summary>
		/// Synchronously processes the web request
		/// </summary>
		public void ProcessRequest(HttpContext context)
		{
			HttpRequest request = context.Request;			// For cleaner code
			HttpResponse response = context.Response;		// For cleaner code

			context.Response.BufferOutput = false;				// No response buffering

			// Get the source URL from the request query string
			string sourceurl = request.QueryString["streamurl"];
			if (String.IsNullOrEmpty(sourceurl))
			{
				response.StatusCode = (int)HttpStatusCode.BadRequest;
				response.End();
				return;
			}

			// If the request headers contain "Icy-Metadata:1", the client is expecting to
			// receive and process the audio stream metadata
			string icyMetadata = request.Headers["Icy-Metadata"];
			bool embedMetadata = (!String.IsNullOrEmpty(icyMetadata) && (String.Compare(icyMetadata, "1") == 0));

			using (AudioStream stream = new AudioStream(sourceurl))
			{
				// Attempt to connect to the source audio stream and return HTTP 503 if unable to
				try { stream.Connect(); }
				catch (Exception)
				{
					response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
					response.End();
					return;
				}

				// Hook up the event handlers
				stream.MetadataChanged += new AudioStream.MetadataChangedEventHandler(OnMetadataChanged);

				// If there is no metadata in the source audio stream, it cannot be re-embedded
				if (embedMetadata && !stream.HasMetadata) embedMetadata = false;

				// Write content type, metadata flag and any other original stream headers to the client
				foreach (KeyValuePair<string, string> header in stream.Headers)
					if (!String.IsNullOrEmpty(header.Value)) response.AppendHeader(header.Key, header.Value);
				response.ContentType = stream.ContentType;
				if (embedMetadata) response.AppendHeader("Icy-Metaint", "8192");

				// Stream the data to the client ....
				if (embedMetadata) StreamWithMetadata(response, stream);
				else StreamRaw(response, stream);
			}
		}

		//---------------------------------------------------------------------
		// Properties
		//---------------------------------------------------------------------

		/// <summary>
		/// Gets a value indicating whether another request can use the IHttpAsyncHandler
		/// </summary>
		public bool IsReusable
		{
			get { return false; }
		}

		//---------------------------------------------------------------------
		// Private Member Functions
		//---------------------------------------------------------------------

		/// <summary>
		/// Aligns a buffer size value
		/// </summary>
		/// <param name="val">Buffer size to be aligned</param>
		/// <param name="alignment">Alignment</param>
		/// <returns>The aligned buffer size</returns>
		private static int AlignBufferSize(int val, int alignment)
		{
			if (val == 0) return alignment;
			return val + alignment - (val % alignment);
		}

		/// <summary>
		/// Invoked when the stream metadata has changed.  Note that this call
		/// will come in from another thread and member vars must be protected
		/// </summary>
		/// <param name="metadata">New metadata</param>
		private void OnMetadataChanged(string metadata)
		{
			lock (m_metadata)
			{
				m_metadata = metadata;
				Interlocked.Exchange(ref m_metadataChanged, 1);
			}
		}

		/// <summary>
		/// Streams the raw audio data without embedding new metadata
		/// </summary>
		/// <param name="response">HttpResponse object instance</param>
		/// <param name="stream">Source AudioStream object instance</param>
		private void StreamRaw(HttpResponse response, AudioStream stream)
		{
			// Raw output is simple to process, just continue to read from
			// the source audio stream buffer and send it to the client
			while ((response.IsClientConnected) && (stream.IsConnected))
			{
				int read = stream.WriteTo(response.OutputStream, 4096);
				if (read == 0) Thread.Sleep(50);
			}
		}

		/// <summary>
		/// Streams the audio data with embedded metadata
		/// </summary>
		/// <param name="response">HttpResponse object instance</param>
		/// <param name="stream">Source AudioStream object instance</param>
		private void StreamWithMetadata(HttpResponse response, AudioStream stream)
		{
			int metacount = 8192;			// Count until metadata

			while ((response.IsClientConnected) && (stream.IsConnected))
			{
				// Write buffered audio data to the stream until 8192 bytes have been sent
				while (metacount > 0)
				{
					int bytesToWrite = Math.Min(4096, metacount);
					int written = stream.WriteTo(response.OutputStream, bytesToWrite);
					metacount -= written;
					if (written == 0) Thread.Sleep(50);
				}

				// Write the metadata block to the client or just a single zero
				// if the metadata hasn't changed since the last time it was sent
				if (m_metadataChanged == 0) response.OutputStream.WriteByte(0);
				else
				{
					lock (m_metadata)
					{
						byte[] encoded = new byte[AlignBufferSize(m_metadata.Length, 16)];
						Encoding.UTF8.GetBytes(m_metadata, 0, m_metadata.Length, encoded, 0);
						response.OutputStream.WriteByte((byte)(encoded.Length / 16));
						response.OutputStream.Write(encoded, 0, encoded.Length);
						Interlocked.Exchange(ref m_metadataChanged, 0);
					}
				}

				metacount = 8192;				// Reset metadata counter
			}
		}

		//---------------------------------------------------------------------
		// Member Variables
		//---------------------------------------------------------------------

		private ProcessRequestHandler	m_delegate;					// Async handler
		private int						m_metadataChanged;			// Metadata changed
		private string					m_metadata = String.Empty;	// Stream metadata
	}
}

//-----------------------------------------------------------------------------