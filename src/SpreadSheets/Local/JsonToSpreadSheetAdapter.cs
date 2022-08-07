using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Spire.Xls;
using StatusUpdateBot.DataSources.Json;

namespace StatusUpdateBot.SpreadSheets.Local
{
    public class JsonToSpreadSheetAdapter: ISpreadSheet
    {
        private readonly IJson _json;

        public object Clone()
        {
            return MemberwiseClone();
        }

        public JsonToSpreadSheetAdapter(string filePath = @"statuses202.json", ISpreadSheet sourceSheet = null)
        {
            _json = new Json(filePath);

            var sheetsToSync = new List<string>();
            
            foreach (var sheetName in Enum.GetValues(typeof(Sheets)))
            {
                if (_json.GetContents().GetValue(sheetName.ToString()) != null)
                    continue;

                _json.GetContents().Add(new JProperty(sheetName.ToString()!, new JArray()));
                sheetsToSync.Add(sheetName.ToString());
            }

            if (sheetsToSync.Count != 0)
                SpreadSheetUtils.SyncSheets(sourceSheet, this, sheetsToSync.ToArray());
        }

         public IList<IList<object>> GetAllRows(string sheet)
         {
             var jSheet = (JArray) _json.GetContents().GetValue(sheet);

             return jSheet!.ToObject<IList<IList<object>>>();
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
             var jSheet = (JArray)_json.GetContents().GetValue(sheet);

             jSheet![rowId - 1] = JArray.FromObject(row);
             
             _json.Save();
         }

         private void CreateRow(string sheet, IList<object> row)
         {
             var jSheet = (JArray) _json.GetContents().GetValue(sheet);
             
             jSheet!.Add(new JArray());

             UpdateRow(sheet, jSheet!.Count, row);
         }
         
         private IList<object> UpdateRowCells(IList<object> row, Dictionary<int, object> cells)
         {
             foreach (var (key, value) in cells)
                 UpdateRowCell(row, key, value);

             return row;
         }
         
         private void UpdateRowCell(IList<object> row, int cellToUpdate, object newCellData)
         {
             while (cellToUpdate > row.Count - 1) 
                 row.Add("");

             row[cellToUpdate] = newCellData;
         }
    }
}