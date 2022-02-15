using System;
using Newtonsoft.Json.Linq;

namespace ExpressionUtility
{
	internal readonly struct GitPackage : IComparable<GitPackage>, IEquatable<GitPackage>
	{
		public Version Version { get; }
		public string ZipURL { get; }

		public static bool TryCreate(JToken token, out GitPackage package)
		{
			package = new GitPackage();
			try
			{
				var name = token["name"]?.ToString();
				var url = token["zipball_url"]?.ToString();
				
				if (Version.TryParse(name ?? string.Empty, out Version version) && !string.IsNullOrEmpty(url))
				{
					package = new GitPackage(version, url);
					return true;
				}
			}
			catch
			{
				return false;
			}
			
			return false;
		}

		public int CompareTo(GitPackage other)
		{
			return Version.CompareTo(other.Version);
		}

		public bool Equals(GitPackage other)
		{
			return Version.Equals(other.Version) && ZipURL == other.ZipURL;
		}

		public override bool Equals(object obj)
		{
			return obj is GitPackage other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (Version.GetHashCode() * 397) ^ ZipURL.GetHashCode();
			}
		}

		private GitPackage(Version version, string zipURL)
		{
			Version = version;
			ZipURL = zipURL;
		}
	}
}