//-------------------------------------------------------------------------------
// MainForm.cs
//
// (C) COPYRIGHT Lockheed Martin Corp. 2010 - Unpublished - All rights reserved.
// This software was developed under contract with the Internal Revenue
// Service (IRS), Contract No. TIRNO-94-D-00028.
//-------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace zuki.build.tools
{
    public partial class MainForm : Form
    {
		public MainForm()
		{
			InitializeComponent();
			this.DialogResult = System.Windows.Forms.DialogResult.OK;
		}

		//---------------------------------------------------------------------------
		// Properties
		//---------------------------------------------------------------------------

		/// <summary>
		/// Gets the specified product name
		/// </summary>
		public string Product
		{
			get { return m_product; }
		}

		/// <summary>
		/// Gets the specified build version number
		/// </summary>
		public ushort VersionBuild
		{
			get { return m_build; }
		}

		/// <summary>
		/// Gets the specified efix version number
		/// </summary>
		public ushort EFix
		{
			get { return m_efix; }
		}

		/// <summary>
		/// Gets the specified major version number
		/// </summary>
		public ushort VersionMajor
		{
			get { return m_maj; }
		}

		/// <summary>
		/// Gets the specified minor version number
		/// </summary>
		public ushort VersionMinor
		{
			get { return m_min; }
		}

		/// <summary>
		/// Gets the specified revision version number
		/// </summary>
		public ushort VersionRevision
		{
			get { return m_revision; }
		}

		//---------------------------------------------------------------------------
		// Event Handlers
		//---------------------------------------------------------------------------

		/// <summary>
		/// Invoked when the user clicks the CANCEL button
		/// </summary>
		/// <param name="sender">Object raising this event</param>
		/// <param name="args">Standard event arguments</param>
		private void OnCancel(object sender, EventArgs args)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		/// <summary>
		/// Invoked when the text in one of the controls changes
		/// </summary>
		/// <param name="sender">Object raising this event</param>
		/// <param name="args">Standard event arguments</param>
		private void OnItemTextChanged(object sender, EventArgs args)
		{
			ushort major, minor;				// Temporary variables
			ushort build, revision;				// Temporary variables

			bool enable = true;
			
			// A product (baseline) name must be set and version must be MAJ.MIN
			if (String.IsNullOrEmpty(m_productText.Text)) enable = false;
			if (!TrySplitVersion(m_versionText.Text, out major, out minor, out build, out revision)) enable = false;

			m_ok.Enabled = enable;
		}

		/// <summary>
		/// Invoked when the user clicks the OK button
		/// </summary>
		/// <param name="sender">Object raising this event</param>
		/// <param name="args">Standard event arguments</param>
		private void OnOK(object sender, EventArgs args)
		{
			m_product = m_productText.Text;
			TrySplitVersion(m_versionText.Text, out m_maj, out m_min, out m_build, out m_revision);

			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		//---------------------------------------------------------------------------
		// Member Functions
		//---------------------------------------------------------------------------

		/// <summary>
		/// Attempts to split a string-based MAJ.MIN string into component numbers
		/// </summary>
		/// <param name="version">Version string in MAJ.MIN format</param>
		/// <param name="major">On success, contains the MAJ portion</param>
		/// <param name="minor">On success, contains the MIN portion</param>
		/// <param name="build">On success, contains the BUILD portion or zero</param>
		/// <param name="revision">On success, contains the REVISION portion or zero</param>
		/// <returns>True if the format was valid, false otherwise</returns>
		private bool TrySplitVersion(string version, out ushort major, out ushort minor,
			out ushort build, out ushort revision)
		{
			major = minor = 0;							// Initialize [out] variables
			build = revision = 0;						// Initialize [out] variables

			if (String.IsNullOrEmpty(version)) return false;
			if (!version.Contains(".")) return false;

			string[] parts = version.Split(new char[] { '.' });
			if (parts.Length < 2) return false;

			if (!UInt16.TryParse(parts[0], out major)) return false;
			if (!UInt16.TryParse(parts[1], out minor)) { major = 0; return false; }

			if(parts.Length >= 3)
				if (!UInt16.TryParse(parts[2], out build)) { major = minor = 0;  return false; }

			if(parts.Length >= 4)
				if (!UInt16.TryParse(parts[3], out revision)) { major = minor = build = 0;  return false; }

			return true;
		}

		//---------------------------------------------------------------------------
		// Member Variables
		//---------------------------------------------------------------------------

		private string			m_product = String.Empty;		// Product string
		private ushort			m_maj = 0;						// Major version
		private ushort			m_min = 0;						// Minor version
		private ushort			m_build = 0;					// Build version
		private ushort			m_revision = 0;					// Revision number
		private ushort			m_efix = 0;						// EFix number
	}
}
