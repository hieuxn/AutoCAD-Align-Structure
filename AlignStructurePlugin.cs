// (C) Copyright 2024 by  
//
using AutoCAD_Align_Structure.Models;
using AutoCAD_Align_Structure.Utils;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;
using System.IO;
using System.Linq;
using System.Reflection;

[assembly: ExtensionApplication(typeof(AutoCAD_Align_Structure.AlignStructurePlugin))]

namespace AutoCAD_Align_Structure
{
	public class AlignStructurePlugin : IExtensionApplication
	{

		void IExtensionApplication.Initialize()
		{
		}

		void IExtensionApplication.Terminate()
		{
		}
	}
}
