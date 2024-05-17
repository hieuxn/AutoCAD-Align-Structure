#nullable enable
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Interop.Common;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

[assembly: CommandClass(typeof(AutoCAD_Align_Structure.MyCommands))]
namespace AutoCAD_Align_Structure
{
	public class MyCommands
	{
		private const string UPPER_BLOCK_NAME = "A$C743d0799";
		private const string LOWER_BLOCK_NAME = "A$C5ad3868f";

		[CommandMethod("StructureGroup", "AutoAlign", "AutoAlign", CommandFlags.Modal | CommandFlags.UsePickSet)]
		public void AutoAlign()
		{
			var document = Application.DocumentManager.MdiActiveDocument;
			var editor = document.Editor;
			var database = document.Database;
			const int upperBlockCount = 5;
			const double pileLength = 10_000; //m

			using var transaction = database.TransactionManager.StartTransaction();
			if (FindEntityByName(transaction, document, UPPER_BLOCK_NAME) is not { } upperBlock) return;
			if (FindEntityByName(transaction, document, LOWER_BLOCK_NAME) is not { } lowerBlock) return;

			if (GetLine(transaction, editor) is not { } line) return;
			var startPoint = line.StartPoint;
			var endPoint = line.EndPoint;
			var length = line.Length;

			// Create
			if (transaction.GetObject(database.CurrentSpaceId, OpenMode.ForWrite) is not BlockTableRecord currentSpace) return;

			var upperPosition = startPoint;
			var lowerPosition = upperPosition;
			var width = 0d;
			var height = 0d;
			for (var i = 0; i <= upperBlockCount; i++)
			{
				if (i < upperBlockCount)
				{
					var upperBlockRef = CreateBlockReference(transaction, currentSpace, upperBlock, upperPosition);
					var extents = upperBlockRef.GeometricExtents;
					width = extents.MaxPoint.X - extents.MinPoint.X;
					height = extents.MaxPoint.Y - extents.MinPoint.Y;
					var upperProps = GetProperties(upperBlockRef.DynamicBlockReferencePropertyCollection.OfType<DynamicBlockReferenceProperty>());

					width = upperProps.ContainsKey("距離1") ? Convert.ToSingle(upperProps["距離1"].Value) : width;
					height = height / 2; // props.ContainsKey("距離2") ? Convert.ToSingle(props["距離2"].Value) : height;
				}

				lowerPosition = upperPosition.Add(new(0, -height, 0));
				upperPosition = upperPosition.Add(new(width, 0, 0));

				var lowerBlockRef = CreateBlockReference(transaction, currentSpace, lowerBlock, lowerPosition);
				var lowerProps = GetProperties(lowerBlockRef.DynamicBlockReferencePropertyCollection.OfType<DynamicBlockReferenceProperty>());
				if (lowerProps.ContainsKey("距離1")) lowerProps["距離1"].Value = pileLength;
			}

			transaction.Commit();
			return;

			static BlockReference CreateBlockReference(Transaction transaction, BlockTableRecord currentSpace, BlockTableRecord blockTableRec, Point3d position)
			{
				var blockReference = new BlockReference(position, blockTableRec.ObjectId);
				currentSpace.AppendEntity(blockReference);
				transaction.AddNewlyCreatedDBObject(blockReference, true);

				return blockReference;
			}

			static Dictionary<string, DynamicBlockReferenceProperty> GetProperties(IEnumerable<DynamicBlockReferenceProperty> list)
			{
				var props = new Dictionary<string, DynamicBlockReferenceProperty>();
				foreach (var item in list)
				{
					if (props.ContainsKey(item.PropertyName)) continue;
					props.Add(item.PropertyName, item);
				}
				return props;
			}

			static Line? GetLine(Transaction transaction, Editor editor)
			{
				var prompt = new PromptEntityOptions("\nSelect a line to get its information: ");
				prompt.SetRejectMessage("\nSelected entity is not a line.");
				prompt.AddAllowedClass(typeof(Line), false);

				var result = editor.GetEntity(prompt);
				if (result.Status != PromptStatus.OK) return null;

				var line = transaction.GetObject(result.ObjectId, OpenMode.ForRead) as Line;
				return line;
			}

			static BlockTableRecord? FindEntityByName(Transaction transaction, Document document, string name)
			{
				if (transaction.GetObject(document.Database.BlockTableId, OpenMode.ForRead) is not BlockTable blockTable) return null;
				if (blockTable.Has(name))
				{
					var blockTableRecord = (BlockTableRecord)transaction.GetObject(blockTable[name], OpenMode.ForRead);
					return blockTableRecord;
				}

				return null;
			}
		}
	}
}
