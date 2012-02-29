//-------------------------------------------------------------------------------
// mkversion
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
//
// USAGE:
//
//	MKVERSION [-clean] [-ini:inifile] [-dev] outfolder [product] [maj.min[.build[.revision]]]
//
//		-clean			- Clean the output folder
//		-rebuild		- Clean the output folder then build
//		-ini:inifile	- Use inifile if it exists to regenerate
//		-dev			- Developer flag. See below.
//		outfolder		- Output folder to place the version files
//		product			- Value to appear in the PRODUCT section of the VERSIONINFO
//		maj.min			- Major and minor build versions (e.g. 17.9, 7.3, 18.1)
//		.build.revision	- Optional overrides for BUILD and REVISION versions
//
//	DEVELOPER FLAG: If no command line arguments are specified, a dialog box will appear
//	to allow the user to enter this information.  If the -dev flag has been included on
//	the command line, the tool will first check to see if existing files are in place
//	in the output folder and will NOT generate new ones or prompt if that is the case.
//  This is done to prevent unnecessary rebuilds in Visual Studio, it's not intended
//  for automated builds from a command line
//---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace zuki.build.tools
{
    static class main
	{
		#region Win32 API
		private class Win32
		{
			[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
			public static extern uint GetPrivateProfileString(
			   string lpAppName,
			   string lpKeyName,
			   string lpDefault,
			   StringBuilder lpReturnedString,
			   uint nSize,
			   string lpFileName);
		};
		#endregion
		
		/// <summary>
		/// Main application entry point
		/// </summary>
		/// <param name="arguments">Array of command line arguments</param>
		[STAThread]
        static void Main(string[] arguments)
        {
			string			outfolder = String.Empty;		// Output folder name
			string			product = String.Empty;			// Product (baseline) name
			ushort			major = 0;						// Version MAJ
			ushort			minor = 0;						// Version MIN
			ushort			build = 0;						// Version BUILD
			ushort			revision = 0;					// Version REVISION
			Version			version;						// Full MAJ.MIN.BUILD.REVISION
			List<string>	args;							// Command line arguments
			List<string>	switches = new List<string>();	// Command line switches
			bool			devMode = false;				// Developer mode
			string			iniFile = String.Empty;			// INI file name
			bool			iniMode = false;				// INI file mode
			bool			rebuild = false;				// Flag to clean first
			bool			clean = false;					// Flag to clean only

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

			args = new List<String>(arguments);				// Move into a List<>

			// Remove all switches from the argument list for separate processing
			int index = 0;
			while(index < args.Count)
			{
				if (args[index].StartsWith("/") || args[index].StartsWith("-"))
				{
					if(args[index].Length > 1) switches.Add(args[index].Substring(1));
					args.RemoveAt(index);
				}
				else index++;
			}

			// Process command line switches
			foreach(string switcharg in switches)
			{
				string sw = switcharg.ToLower();

				// dev - Set developer mode
				if (sw == "dev") devMode = true;

				// ini - try to read default values from an ini file
				else if (sw.StartsWith("ini") && (sw.Contains(":")))
				{
					string filename = sw.Substring(sw.IndexOf(':') + 1);
					if (!String.IsNullOrEmpty(filename))
					{
						iniFile = Path.GetFullPath(filename);
						if (File.Exists(iniFile)) iniMode = true;
					}
				}

				// rebuild - Clean the output folder before generating code
				if (sw == "rebuild") rebuild = true;

				// clean - Just clean the output folder and exit
				if (sw == "clean") clean = true;
			}

			// Verify that the output folder exists
			if (args.Count >= 1)
			{
				if (!Directory.Exists(args[0]))
				{
					try { Directory.CreateDirectory(args[0]); }
					catch
					{
						MessageBox.Show("Invalid command line arguments. Specified output folder [" +
							args[0] + "] does not exist.", "MKVERSION", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
				}
			}

			// In CLEAN or REBUILD mode, delete the output files first
			if (clean || rebuild)
			{
				string cleanFile = String.Empty;

				cleanFile = Path.Combine(args[0], "version.cpp");
				if (File.Exists(cleanFile)) { File.SetAttributes(cleanFile, FileAttributes.Normal); File.Delete(cleanFile); }
				cleanFile = Path.Combine(args[0], "version.cs");
				if (File.Exists(cleanFile)) { File.SetAttributes(cleanFile, FileAttributes.Normal); File.Delete(cleanFile); }
				cleanFile = Path.Combine(args[0], "version.rc");
				if (File.Exists(cleanFile)) { File.SetAttributes(cleanFile, FileAttributes.Normal); File.Delete(cleanFile); }
				cleanFile = Path.Combine(args[0], "version.vb");
				if (File.Exists(cleanFile)) { File.SetAttributes(cleanFile, FileAttributes.Normal); File.Delete(cleanFile); }
				cleanFile = Path.Combine(args[0], "version.ini");
				if (File.Exists(cleanFile)) { File.SetAttributes(cleanFile, FileAttributes.Normal); File.Delete(cleanFile); }
				cleanFile = Path.Combine(args[0], "version.txt");
				if (File.Exists(cleanFile)) { File.SetAttributes(cleanFile, FileAttributes.Normal); File.Delete(cleanFile); }
				cleanFile = Path.Combine(args[0], "version.wxi");
				if (File.Exists(cleanFile)) { File.SetAttributes(cleanFile, FileAttributes.Normal); File.Delete(cleanFile); }

				if (clean) return;				// <--- Clean only, exit now
			}

			// In DEV MODE, if *all* of the output files already exist, don't do anything at all
			// to prevent unnecessary rebuilds
			if (devMode)
			{
				if(File.Exists(Path.Combine(args[0], "version.cpp")) &&
					File.Exists(Path.Combine(args[0], "version.cs")) &&
					File.Exists(Path.Combine(args[0], "version.rc")) &&
					File.Exists(Path.Combine(args[0], "version.vb")) &&
					File.Exists(Path.Combine(args[0], "version.ini")) &&
					File.Exists(Path.Combine(args[0], "version.txt")) &&
					File.Exists(Path.Combine(args[0], "version.wxi"))) return;
			}

			// INI file mode - read what we need from there instead of the command line
			if (iniMode)
			{
				StringBuilder iniString = new StringBuilder(255);

				Win32.GetPrivateProfileString("Version", "Baseline", "", iniString, 255, iniFile);
				product = iniString.ToString();
				if (String.IsNullOrEmpty(product))
				{
					MessageBox.Show("Invalid .INI file contents.  Baseline must be set", "MKVERSION", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				Win32.GetPrivateProfileString("Version", "Version", "", iniString, 255, iniFile);
				if (!TrySplitVersion(iniString.ToString(), out major, out minor, out build, out revision))
				{
					MessageBox.Show("Invalid .INI file contents. Version number must be specified in MAJ.MIN[.PATCH[.BUILD]] format.",
						"MKVERSION", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
			}

			// If at least three command line arguments were passed, parse them out as the
			// product (baseline) and MAJ.MIN portions of the version number
			else if (args.Count >= 3)
			{
				product = args[1];
				if (!TrySplitVersion(args[2], out major, out minor, out build, out revision))
				{
					MessageBox.Show("Invalid command line arguments. Version number must be specified in MAJ.MIN[.PATCH[.BUILD]] format.", 
						"MKVERSION", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
			}
			
			// Otherwise, if just the output folder was provided
			// form-based interactive method of getting the infromation we need
			else if (args.Count == 1)
			{
				using (MainForm form = new MainForm())
				{
					Application.Run(form);
					if (form.DialogResult != DialogResult.OK) return;

					// Copy the properties from the form locally
					product = form.Product;
					major = form.VersionMajor;
					minor = form.VersionMinor;
					build = form.VersionBuild;
					revision = form.VersionRevision;
				}
			}
			
			// The number of command line arguments was invalid
			else
			{
				MessageBox.Show("Invalid command line arguments.  Please see documentation.", "MKVERSION",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// If the specified REVISION number was not specified, generate it
			if (revision == 0) revision = (ushort)DateTime.Today.Subtract(DateTime.Parse("1/1/2000")).Days;
			version = new Version(major, minor, build, revision);

			// Output the files to the destination folder
			GenerateOutputFromResource(Path.Combine(args[0], "version"), "version.txt", product, version);
			GenerateOutputFromResource(Path.Combine(args[0], "version.cpp"), "version_cpp.txt", product, version);
			GenerateOutputFromResource(Path.Combine(args[0], "version.cs"), "version_cs.txt", product, version);
			GenerateOutputFromResource(Path.Combine(args[0], "version.rc"), "version_rc.txt", product, version);
			GenerateOutputFromResource(Path.Combine(args[0], "version.vb"), "version_vb.txt", product, version);
			GenerateOutputFromResource(Path.Combine(args[0], "version.ini"), "version_ini.txt", product, version);
			GenerateOutputFromResource(Path.Combine(args[0], "version.txt"), "version_txt.txt", product, version);
			GenerateOutputFromResource(Path.Combine(args[0], "version.wxi"), "version_wxi.txt", product, version);
		}

		/// <summary>
		/// Generates an output version file
		/// </summary>
		/// <param name="outfile">Output file name</param>
		/// <param name="resname">Input embedded resource file name</param>
		/// <param name="product">Input product name</param>
		/// <param name="version">Version</param>
		private static void GenerateOutputFromResource(string outfile, string resname, string product, Version version)
		{
			DateTime now = DateTime.Now;
			string filedata = String.Empty;

			// Make sure the destination file isn't read-only if it already exists
			if (File.Exists(outfile)) File.SetAttributes(outfile, FileAttributes.Normal);

			// Load the resoucre file into a string for manipulation
			using (StreamReader sr = new StreamReader(typeof(main).Assembly.GetManifestResourceStream(typeof(main), resname)))
			{
				filedata = sr.ReadToEnd();
			}

			//
			// REPLACEMENT STRINGS - ADD AS MANY HERE AS YOU WANT
			//

			filedata = filedata.Replace("%%PRODUCTNAME%%", product);
			filedata = filedata.Replace("%%VERSION%%", version.ToString(4));
			filedata = filedata.Replace("%%YYYY%%", now.ToString("yyyy"));
			filedata = filedata.Replace("%%VERSIONMAJOR%%", version.Major.ToString());
			filedata = filedata.Replace("%%VERSIONMINOR%%", version.Minor.ToString());
			filedata = filedata.Replace("%%VERSIONBUILD%%", version.Build.ToString());
			filedata = filedata.Replace("%%VERSIONREVISION%%", version.Revision.ToString());
			filedata = filedata.Replace("%%MMDDH%%", now.ToString("Mdd") + Math.Floor(now.Hour / 2.5).ToString());

			// Write the modified resoucre file out to disk as the new version file
			using(StreamWriter sw = new StreamWriter(File.Open(outfile, FileMode.Create, FileAccess.Write, FileShare.None)))
			{
				sw.Write(filedata);
				sw.Flush();
			}
		}

		/// <summary>
		/// Attempts to split a string-based MAJ.MIN string into component numbers
		/// </summary>
		/// <param name="version">Version string in MAJ.MIN format</param>
		/// <param name="major">On success, contains the MAJ portion</param>
		/// <param name="minor">On success, contains the MIN portion</param>
		/// <param name="build">On success, contains the BUILD portion or zero</param>
		/// <param name="revision">On success, contains the REVISION portion or zero</param>
		/// <returns>True if the format was valid, false otherwise</returns>
		private static bool TrySplitVersion(string version, out ushort major, out ushort minor,
			out ushort build, out ushort revision)
		{
			major = minor = 0;							// Initialize [out] variables
			build = revision = 0;						// Initialize [out] variables

			if (String.IsNullOrEmpty(version)) return false;
			if (!version.Contains(".")) return false;

			// This is fairly straightforward.  Split the version string on period chars
			// and assign the proper [out] variables based on how many parts there were

			string[] parts = version.Split(new char[] { '.' });
			if (parts.Length < 2) return false;

			if (!UInt16.TryParse(parts[0], out major)) return false;
			if (!UInt16.TryParse(parts[1], out minor)) { major = 0; return false; }

			if (parts.Length >= 3)
				if (!UInt16.TryParse(parts[2], out build)) { major = minor = 0; return false; }

			if (parts.Length >= 4)
				if (!UInt16.TryParse(parts[3], out revision)) { major = minor = build = 0; return false; }

			return true;
		}
	}
}
