using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace AutoCAD_Align_Structure.Utils
{
	public class BlockTableRecordLoader
	{
		/// <summary>
		/// Returns name of inserted blocks
		/// </summary>
		/// <param name="dwgFilePath"></param>
		/// <param name="blockNames"></param>
		/// <returns></returns>
		public IEnumerable<string> InsertBlockTableRecordFromFile(string dwgFilePath, params string[] blockNames)
		{
			using var sourceDb = new Database(false, true);
			using var sourceTransaction = sourceDb.TransactionManager.StartTransaction();
			sourceDb.ReadDwgFile(dwgFilePath, FileOpenMode.OpenForReadAndAllShare, true, "");

			var targetDb = Application.DocumentManager.MdiActiveDocument.Database;
			using var targetTransaction = targetDb.TransactionManager.StartTransaction();
			if (targetTransaction.GetObject(targetDb.BlockTableId, OpenMode.ForWrite) is not BlockTable targetBlockTable) yield break;

			foreach (string blockName in blockNames)
			{
				if (targetBlockTable.Has(blockName)) continue;
				if (LoadBlockTableRecordFromFile(sourceDb, sourceTransaction, targetTransaction, targetBlockTable, blockName) is not { }) continue;
				yield return blockName;
			}

			targetTransaction.Commit();
		}

		private BlockTableRecord LoadBlockTableRecordFromFile(Database sourceDb, Transaction sourceTransaction, Transaction targetTransaction, BlockTable targetBlockTable, string blockName)
		{
			var clonedBlockTableRecord = default(BlockTableRecord?);

			try
			{
				if (sourceTransaction.GetObject(sourceDb.BlockTableId, OpenMode.ForRead) is not BlockTable blockTable) return null;
				if (false == blockTable.Has(blockName)) return null;
				if (sourceTransaction.GetObject(blockTable[blockName], OpenMode.ForRead) is not BlockTableRecord blockTableRecord) return null;

				return CloneBlockTableRecord(blockTableRecord, sourceTransaction, targetBlockTable, targetTransaction);
			}
			catch (Autodesk.AutoCAD.Runtime.Exception ex)
			{
				Application.ShowAlertDialog($"Error: {ex.Message}");
			}

			return clonedBlockTableRecord;
		}


		private Dictionary<string, ObjectId> blockIdMap = new Dictionary<string, ObjectId>();

		private BlockTableRecord CloneBlockTableRecord(BlockTableRecord sourceRecord, Transaction sourceTransaction, BlockTable targetTable, Transaction targetTransaction)
		{
			if (blockIdMap.ContainsKey(sourceRecord.Name))
			{
				var blockTableRecord = (BlockTableRecord)targetTransaction.GetObject(targetTable[sourceRecord.Name], OpenMode.ForRead);
				return blockTableRecord;
			}

			BlockTableRecord clonedRecord = new BlockTableRecord
			{
				Name = sourceRecord.Name
			};

			targetTable.Add(clonedRecord);
			targetTransaction.AddNewlyCreatedDBObject(clonedRecord, true);

			blockIdMap[sourceRecord.Name] = clonedRecord.ObjectId;

			foreach (ObjectId id in sourceRecord)
			{
				if (sourceTransaction.GetObject(id, OpenMode.ForRead) is not Entity entity) continue;
				if (entity is BlockReference blockRef)
				{
					BlockTableRecord nestedBtr = (BlockTableRecord)sourceTransaction.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead);
					CloneBlockTableRecord(nestedBtr, sourceTransaction, targetTable, targetTransaction);

					var clonedBlockRef = new BlockReference(blockRef.Position, blockIdMap[nestedBtr.Name]);
					clonedRecord.AppendEntity(clonedBlockRef);
					targetTransaction.AddNewlyCreatedDBObject(clonedBlockRef, true);
				}
				else
				{
					var clonedEntity = entity.Clone() as Entity;
					clonedRecord.AppendEntity(clonedEntity);
					targetTransaction.AddNewlyCreatedDBObject(clonedEntity, true);
				}
			}

			return clonedRecord;
		}
	}
}
