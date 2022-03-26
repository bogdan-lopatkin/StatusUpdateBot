using System;
using System.Collections.Generic;
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
    }
}