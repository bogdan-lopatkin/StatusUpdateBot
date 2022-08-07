using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Spire.Xls;

namespace StatusUpdateBot.SpreadSheets.Local
{
    public class ExcelSpreadSheet: ISpreadSheet
    {
        private readonly Workbook _package;
        private readonly string _filePath;

        public object Clone()
        {
            return MemberwiseClone();
        }

        public ExcelSpreadSheet(string filePath = @"statuses.xlsx", ISpreadSheet sourceSheet = null)
        {
            _filePath = filePath;
            var fileStream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            fileStream.Dispose();
            _package = new Workbook();
            _package.LoadFromFile(filePath);
            var workSheets = _package.Worksheets;

            var sheetsToSync = new List<string>();
            
            foreach (var sheetName in Enum.GetValues(typeof(Sheets)))
            {
                if (workSheets.Any(workSheet => workSheet.Name == sheetName.ToString()))
                    continue;

                _package.Worksheets.Add(sheetName.ToString());
                sheetsToSync.Add(sheetName.ToString());
            }

            if (sheetsToSync.Count != 0)
                SpreadSheetUtils.SyncSheets(sourceSheet, this, sheetsToSync.ToArray());

            SaveSheet();
        }

         public IList<IList<object>> GetAllRows(string sheet)
         {
             if (!_package.Worksheets[sheet].Cells.Any())
                 return new List<IList<object>>();
             
             int rowCount = _package.Worksheets[sheet].LastDataRow;
             int columnCount = _package.Worksheets[sheet].LastDataColumn;

             IList<IList<object>> rows = new List<IList<object>>();

             for (int i = 0; i < rowCount; i++)
             {
                 var row = new List<object>();

                 for (int j = 0; j < columnCount; j++)
                 {
                     var cell = _package.Worksheets[sheet].Rows[i]?.CellList[j];

                     if (cell == null)
                     {
                         row.Add("");
                         continue;
                     }
                     
                     row.Add((cell.HasDateTime ? cell.DateTimeValue.ToLongTimeString() : cell.Value) ?? "");
                 }
                 
                 rows.Add(row);
             }

             return rows;
         }

         public (int id, IList<object>) FindRow(string sheet, string searchFor, int searchIn = 0)
         {
             var values = GetAllRows(sheet);

             if (values is not {Count: > 0}) return (-1, null);

             var index = 1;
             foreach (var row in values)
             {
                 if (row.Count > searchIn && row[searchIn] != null && row[searchIn].ToString() == searchFor)
                     return (index, row);

                 index++;
             }

             return (-1, null);
         }

         public bool FindAndUpdateRow(string sheet, string searchFor, int searchIn, Dictionary<int, object> cells)
         {
             var (rowId, row) = FindRow(sheet, searchFor, searchIn);

             if (rowId == -1)
                 return false;

             row = UpdateRowCells(row, cells);
             UpdateRow(sheet, rowId, row);

             return true;
         }

         public bool UpdateOrCreateRow(string sheet, string searchFor, int searchIn, Dictionary<int, object> cells)
         {
             var (rowId, row) = FindRow(sheet, searchFor, searchIn);

             row = UpdateRowCells(row ?? new List<object>(), cells);

             if (rowId == -1)
                 CreateRow(sheet, row);
             else
                 UpdateRow(sheet, rowId, row);

            return true;
         }

         private void UpdateRow(string sheet, int rowId, IList<object> row)
         {
             _package.Worksheets[sheet].InsertArray(row.ToArray(), rowId, 1, false);
             SaveSheet();
         }

         private void CreateRow(string sheet, IList<object> row)
         {
             if (!_package.Worksheets[sheet].Cells.Any())
                 UpdateRow(sheet, 1, row);
             else
                 UpdateRow(sheet, _package.Worksheets[sheet].LastDataRow + 1, row);
         }

         private void UpdateRowCell(IList<object> row, int cellToUpdate, object newCellData)
         {
             while (cellToUpdate > row.Count - 1) row.Add("");

             row[cellToUpdate] = newCellData;
         }

         private IList<object> UpdateRowCells(IList<object> row, Dictionary<int, object> cells)
         {
             foreach (var (key, value) in cells) UpdateRowCell(row, key, value);

             return row;
         }

         private void SaveSheet()
        {
            _package.SaveToFile(_filePath);
        }
    }
}