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
				try { stream.Connect(ref embedMetadata); }
				catch (Exception)
				{
					response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
					response.End();
					return;
				}

				// Write content type, metadata flag and any other original stream headers to the client
				foreach (KeyValuePair<string, string> header in stream.Headers)
					if (!String.IsNullOrEmpty(header.Value)) response.AppendHeader(header.Key, header.Value);
				response.ContentType = stream.ContentType;
				if (embedMetadata) response.AppendHeader("Icy-Metaint", stream.MetadataInterval.ToString());

				// Continue to read from the source stream and send it to the client
				while ((response.IsClientConnected) && (stream.IsConnected))
				{
					int written = stream.WriteTo(response.OutputStream, 4096);
					if (written == 0) Thread.Sleep(100);
				}
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
		// Member Variables
		//---------------------------------------------------------------------

		private ProcessRequestHandler			m_delegate;			// Async handler
	}
}

//-----------------------------------------------------------------------------