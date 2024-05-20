#nullable enable
using AutoCAD_Align_Structure.Models;
using AutoCAD_Align_Structure.Views;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

[assembly: CommandClass(typeof(AutoCAD_Align_Structure.AlignStructureCommand))]
namespace AutoCAD_Align_Structure
{
	public class AlignStructureCommand
	{
		private const string UPPER_BLOCK_NAME = "A$C743d0799";
		private const string LOWER_BLOCK_NAME = "A$C5ad3868f";

		[CommandMethod("StructureGroup", "AutoAlign", "AutoAlign", CommandFlags.Modal | CommandFlags.UsePickSet)]
		public void AutoAlign()
		{
			var document = Application.DocumentManager.MdiActiveDocument;
			var editor = document.Editor;
			var database = document.Database;

			if (ShowInputDialog() is not { } result) return;

			using var transaction = database.TransactionManager.StartTransaction();

			if (FindEntityByName(transaction, document, UPPER_BLOCK_NAME) is not { } upperBlock) return;
			if (FindEntityByName(transaction, document, LOWER_BLOCK_NAME) is not { } lowerBlock) return;

			if (GetLine(transaction, editor) is not { } line) return;

			var startPoint = line.StartPoint.X > line.EndPoint.X ? line.EndPoint : line.StartPoint;
			startPoint += new Vector3d(100, 0, 0);

			// Create
			if (transaction.GetObject(database.CurrentSpaceId, OpenMode.ForWrite) is not BlockTableRecord currentSpace) return;

			var upperPosition = startPoint;
			var lowerPosition = upperPosition;
			var width = 0d;
			var height = 0d;
			for (var i = 0; i <= result.SpanCount; i++)
			{
				if (i < result.SpanCount)
				{
					var upperBlockRef = CreateBlockReference(transaction, currentSpace, upperBlock, upperPosition);

					(width, height) = GetExtents(transaction, upperBlockRef);

					var upperProps = GetProperties(upperBlockRef.DynamicBlockReferencePropertyCollection.OfType<DynamicBlockReferenceProperty>());

					width = upperProps.ContainsKey("距離1") ? Convert.ToSingle(upperProps["距離1"].Value) : width;
				}

				lowerPosition = upperPosition.Add(new(0, -height, 0));
				upperPosition = upperPosition.Add(new(width, 0, 0));

				var lowerBlockRef = CreateBlockReference(transaction, currentSpace, lowerBlock, lowerPosition);
				var lowerProps = GetProperties(lowerBlockRef.DynamicBlockReferencePropertyCollection.OfType<DynamicBlockReferenceProperty>());

				if (lowerProps.ContainsKey("距離1")) lowerProps["距離1"].Value = result.ProtrusionLength + result.EmbedmentLength;
			}

			DuplicateLine(transaction, currentSpace, line, new Vector3d(0, -result.ProtrusionLength - height, 0));

			transaction.Commit();
			return;

			static (double width, double height) GetExtents(Transaction transaction, BlockReference blockRef)
			{
				var width = 0d;
				var height = 0d;
				foreach (var (entity, matrix) in GetManyEntity(transaction, blockRef))
				{
					var extents = entity.GeometricExtents;
					extents.TransformBy(matrix);

					var deltaX = extents.MaxPoint.X - extents.MinPoint.X;
					var deltaY = extents.MaxPoint.Y - extents.MinPoint.Y;

					width = width < deltaX ? deltaX : width;
					height = height < deltaY ? deltaY : height;
				}

				return (width, height);
			}

			static IEnumerable<(Entity, Matrix3d)> GetManyEntity(Transaction transaction, BlockReference blockRef)
			{
				if (transaction.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) is not BlockTableRecord blockTableRec) yield break;

				foreach (ObjectId entId in blockTableRec)
				{
					if (transaction.GetObject(entId, OpenMode.ForRead) is not Entity entity || false == entity.Visible) continue;
					if (entity is BlockReference nestedBlockRef)
					{
						if (GetManyEntity(transaction, nestedBlockRef) is not { } entities) continue;
						foreach (var (nestedEntity, transform) in entities)
						{
							yield return (nestedEntity, blockRef.BlockTransform * transform);
						}
					}
					else
					{
						yield return (entity, blockRef.BlockTransform);
					}
				}
			}

			static Line DuplicateLine(Transaction transaction, BlockTableRecord currentSpace, Line selectedLine, Vector3d offset)
			{
				Line newLine = new Line(selectedLine.StartPoint.Add(offset), selectedLine.EndPoint.Add(offset))
				{
					Color = selectedLine.Color,
					Linetype = selectedLine.Linetype,
					LinetypeScale = selectedLine.LinetypeScale,
					LineWeight = selectedLine.LineWeight,
					Layer = selectedLine.Layer
				};

				currentSpace.AppendEntity(newLine);
				transaction.AddNewlyCreatedDBObject(newLine, true);
				return newLine;
			}

			static InputModel? ShowInputDialog()
			{
				var inputControl = new InputWindow();
				var hwnd = Autodesk.AutoCAD.ApplicationServices.Core.Application.MainWindow.Handle;
				Application.ShowModalWindow(hwnd, inputControl);
				return inputControl.DialogResult == true ? inputControl.ViewModel.GetResult() : null;
			}

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
