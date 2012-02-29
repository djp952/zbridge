//-----------------------------------------------------------------------------
// BridgeService.cs
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
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using zuki.web.server;

namespace zuki.web.zbridge
{
	partial class BridgeService : ServiceBase
	{
		private const string ZBRIDGE_APPID = "zBridge";

		public BridgeService()
		{
			InitializeComponent();					// Initialize the designer-based code

			// Set up sinks for all possible Zucchini activity events
			WebServer.ApplicationActivity += new WebApplicationActivityHandler(OnWebApplicationActivity);
			WebServer.ApplicationRestartFailure += new WebApplicationExceptionHandler(OnWebApplicationRestartFailure);
			WebServer.ApplicationStarted += new WebApplicationEventHandler(OnWebApplicationStarted);
			WebServer.ApplicationStopped += new WebApplicationEventHandler(OnWebApplicationStopped);
		}

		protected override void OnStart(string[] args)
		{
			try
			{
				WebApplicationConfiguration config = WebApplicationConfiguration.FromXml(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), 
					"zbridge.vweb.config"));
				WebServer.Applications.Create("zBridge", config);
			}
			catch (Exception ex)
			{
				EventLog.WriteEntry("zBridge: " + ex.Message);
			}

		}
			
		protected override void OnStop()
		{
			try
			{
				WebApplication app = WebServer.Applications["zBridge"];
				if (app != null) app.Shutdown();
			}
			catch (Exception) { }
		}

		//---------------------------------------------------------------------
		// Event Handlers
		//---------------------------------------------------------------------

		void OnWebApplicationActivity(string appid, string activity)
		{
			if (appid == ZBRIDGE_APPID) EventLog.WriteEntry(activity);
		}

		void OnWebApplicationRestartFailure(string appid, Exception exception)
		{
			if (appid == ZBRIDGE_APPID) EventLog.WriteEntry(exception.Message, EventLogEntryType.Error);
		}

		void OnWebApplicationStarted(string appid, WebApplicationEventArgs args)
		{
			if (appid == ZBRIDGE_APPID) EventLog.WriteEntry("Web application started");
		}

		void OnWebApplicationStopped(string appid, WebApplicationEventArgs args)
		{
			if (appid == ZBRIDGE_APPID) EventLog.WriteEntry("Web application stopped");
		}

		//---------------------------------------------------------------------
		// Member Variables
		//---------------------------------------------------------------------
	}
}
