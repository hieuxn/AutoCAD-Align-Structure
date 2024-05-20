using System.IO;
using System.Linq;
using System.Reflection;

namespace AutoCAD_Align_Structure.Utils
{
	public static class FilePathUtils
	{
		public static string GetFilePath(params string[] fileName)
		{
			var assemblyPath = Assembly.GetExecutingAssembly().Location;
			var assemblyDir = Path.GetDirectoryName(assemblyPath);
			fileName = fileName.Prepend(assemblyDir).ToArray();
			var filePath = Path.Combine(fileName);
			return filePath;
		}
	}
}
