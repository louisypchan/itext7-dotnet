/*
$Id$

This file is part of the iText (R) project.
Copyright (c) 1998-2016 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/
using System;
using System.Reflection;

namespace iTextSharp.Kernel
{
	/// <summary>This class contains version information about iText.</summary>
	/// <remarks>
	/// This class contains version information about iText.
	/// DO NOT CHANGE THE VERSION INFORMATION WITHOUT PERMISSION OF THE COPYRIGHT HOLDERS OF ITEXT.
	/// Changing the version makes it extremely difficult to debug an application.
	/// Also, the nature of open source software is that you honor the copyright of the original creators of the software.
	/// </remarks>
	public sealed class Version
	{
		/// <summary>String that will indicate if the AGPL version is used.</summary>
		private static String AGPL = " (AGPL-version)";

		/// <summary>The iText version instance.</summary>
		private static Version version = null;

		/// <summary>This String contains the name of the product.</summary>
		/// <remarks>
		/// This String contains the name of the product.
		/// iText is a registered trademark by iText Group NV.
		/// Please don't change this constant.
		/// </remarks>
		private static String iText = "iText\u00ae";

		/// <summary>This String contains the version number of this iText release.</summary>
		/// <remarks>
		/// This String contains the version number of this iText release.
		/// For debugging purposes, we request you NOT to change this constant.
		/// </remarks>
		private static String release = "7.0.1-SNAPSHOT";

		/// <summary>This String contains the iText version as shown in the producer line.</summary>
		/// <remarks>
		/// This String contains the iText version as shown in the producer line.
		/// iText is a product developed by iText Group NV.
		/// iText Group requests that you retain the iText producer line
		/// in every PDF that is created or manipulated using iText.
		/// </remarks>
		private String iTextVersion = iText + " " + release + " \u00a92000-2016 iText Group NV";

		/// <summary>The license key.</summary>
		private String key = null;

		/// <summary>Gets an instance of the iText version that is currently used.</summary>
		/// <remarks>
		/// Gets an instance of the iText version that is currently used.
		/// Note that iText Group requests that you retain the iText producer line
		/// in every PDF that is created or manipulated using iText.
		/// </remarks>
		public static Version GetInstance()
		{
			if (version == null)
			{
				version = new Version();
				lock (version)
				{
					try
					{
						Type klass = System.Type.GetType("com.itextpdf.licensekey.LicenseKey");
						MethodInfo m = klass.GetMethod("getLicenseeInfo");
						String[] info = (String[])m.Invoke(System.Activator.CreateInstance(klass), null);
						if (info[3] != null && info[3].Trim().Length > 0)
						{
							version.key = info[3];
						}
						else
						{
							version.key = "Trial version";
							if (info[5] == null)
							{
								version.key += "unauthorised";
							}
							else
							{
								version.key += info[5];
							}
						}
						if (info[4] != null && info[4].Trim().Length > 0)
						{
							version.iTextVersion = info[4];
						}
						else
						{
							if (info[2] != null && info[2].Trim().Length > 0)
							{
								version.iTextVersion += " (" + info[2];
								if (!version.key.ToLower().StartsWith("trial"))
								{
									version.iTextVersion += "; licensed version)";
								}
								else
								{
									version.iTextVersion += "; " + version.key + ")";
								}
							}
							else
							{
								if (info[0] != null && info[0].Trim().Length > 0)
								{
									// fall back to contact name, if company name is unavailable
									version.iTextVersion += " (" + info[0];
									if (!version.key.ToLower().StartsWith("trial"))
									{
										// we shouldn't have a licensed version without company name,
										// but let's account for it anyway
										version.iTextVersion += "; licensed version)";
									}
									else
									{
										version.iTextVersion += "; " + version.key + ")";
									}
								}
								else
								{
									throw new Exception();
								}
							}
						}
					}
					catch (Exception)
					{
						version.iTextVersion += AGPL;
					}
				}
			}
			return version;
		}

		/// <summary>Checks if the AGPL version is used.</summary>
		/// <returns>returns true if the AGPL version is used.</returns>
		public static bool IsAGPLVersion()
		{
			return GetInstance().GetVersion().IndexOf(AGPL) > 0;
		}

		/// <summary>Gets the product name.</summary>
		/// <remarks>
		/// Gets the product name.
		/// iText Group NV requests that you retain the iText producer line
		/// in every PDF that is created or manipulated using iText.
		/// </remarks>
		/// <returns>the product name</returns>
		public String GetProduct()
		{
			return iText;
		}

		/// <summary>Gets the release number.</summary>
		/// <remarks>
		/// Gets the release number.
		/// iText Group NV requests that you retain the iText producer line
		/// in every PDF that is created or manipulated using iText.
		/// </remarks>
		/// <returns>the release number</returns>
		public String GetRelease()
		{
			return release;
		}

		/// <summary>Returns the iText version as shown in the producer line.</summary>
		/// <remarks>
		/// Returns the iText version as shown in the producer line.
		/// iText is a product developed by iText Group NV.
		/// iText Group requests that you retain the iText producer line
		/// in every PDF that is created or manipulated using iText.
		/// </remarks>
		/// <returns>iText version</returns>
		public String GetVersion()
		{
			return iTextVersion;
		}

		/// <summary>Returns a license key if one was provided, or null if not.</summary>
		/// <returns>a license key.</returns>
		public String GetKey()
		{
			return key;
		}
	}
}