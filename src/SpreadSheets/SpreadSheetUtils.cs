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

            if (rowId == -1 || row.Count + 1 < (int) SettingsSheetCells.Value)
                return defaultValue;

            return row[(int) SettingsSheetCells.Value].ToString();
        }

        public static void SetSetting(ISpreadSheet spreadSheet, Enum setting, string value)
        {
            spreadSheet.UpdateOrCreateRow(
                Sheets.Settings.GetStringValue(),
                setting.GetStringValue(),
                (int) SettingsSheetCells.Id,
                new Dictionary<int, string>
                {
                    {(int) SettingsSheetCells.Value, value}
                }
            );
        }

        public static bool IsRowHasCell(IList<object> row, Enum cell)
        {
            return row != null && row.Count > (int) (object) cell && row[(int) (object) cell].ToString() != "";
        }
    }
}