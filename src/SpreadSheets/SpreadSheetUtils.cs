using System;
using System.Collections.Generic;
using System.Linq;
using EnumStringValues;

namespace StatusUpdateBot.SpreadSheets
{
    public static class SpreadSheetUtils
    {
        public static string GetSetting(ISpreadSheet spreadSheet, Enum setting, string defaultValue = null)
        {
            var (rowId, row) = spreadSheet.FindRow(Sheets.Settings.GetStringValue(), setting.GetStringValue());

            if (rowId == -1 || row.Count <= (int) SettingsSheetCells.Value)
                return defaultValue;

            return row[(int) SettingsSheetCells.Value].ToString();
        }

        public static void SetSetting(ISpreadSheet spreadSheet, Enum setting, string value)
        {
            spreadSheet.UpdateOrCreateRow(
                Sheets.Settings.GetStringValue(),
                setting.GetStringValue(),
                (int) SettingsSheetCells.Id,
                new Dictionary<int, object>
                {
                    {(int) SettingsSheetCells.Value, value}
                }
            );
        }

        public static bool IsRowHasCell(IList<object> row, Enum cell)
        {
            return IsRowHasCell(row, (int) (object) cell);
        }
        
        public static bool IsRowHasCell(IList<object> row, Enum cell, out string value)
        {
            return IsRowHasCell(row, (int) (object) cell, out value);
        }
        
        public static bool IsRowHasCell(IList<object> row, int cell)
        {
            return IsRowHasCell(row, cell, out _);
        }
        
        public static bool IsRowHasCell(IList<object> row, int cell, out string value)
        {
            var isRowHasCell = row != null && row.Count > cell && row[cell].ToString() != "";
            
            value = isRowHasCell ? row[cell].ToString() : null;

            return isRowHasCell;
        }
        
        public static string TryGetCell(IList<object> row, Enum cell, string fallback = null)
        {
            return IsRowHasCell(row, cell, out var value) ? value : fallback;
        }

        public static void SyncSheets(ISpreadSheet originSpreadSheet, ISpreadSheet targetSpreadSheet, string[] sheetsToSync = null)
        {
            sheetsToSync ??= Enum.GetValues(typeof(Sheets))
                .Cast<Sheets>()
                .Select(v => v.ToString())
                .ToArray();
            
            var cachedOriginSpreadSheet = originSpreadSheet as ICachedSpreadSheet;
            var cachedTargetSpreadSheet = targetSpreadSheet as ICachedSpreadSheet;
            cachedOriginSpreadSheet?.LoadCache(sheetsToSync);
            cachedTargetSpreadSheet?.LoadCache(sheetsToSync);
            cachedTargetSpreadSheet?.EnableBatchUpdate();

            foreach (var sheet in sheetsToSync)
            {
                var rows = cachedOriginSpreadSheet?.GetAllRows(sheet, false) ?? originSpreadSheet.GetAllRows(sheet);

                if (rows == null)
                    continue;
                
                foreach (var row in rows)
                {
                    if (row.Count == 0)
                        continue;
                    
                    var rowById = new Dictionary<int, object>();

                    for (int i = 0; i < row.Count; i++)
                    {
                        var rowValue = row[i];

                        if (Double.TryParse(rowValue.ToString(), out var parsedRowValue))
                            rowValue = parsedRowValue;
                        
                        rowById.Add(i, rowValue);
                    }

                    targetSpreadSheet.UpdateOrCreateRow(sheet, row[0].ToString(), (int) UserStatusSheetCells.Id, rowById);
                }
            }
            
            cachedTargetSpreadSheet?.ExecuteBatchUpdate();
        }
    }
}