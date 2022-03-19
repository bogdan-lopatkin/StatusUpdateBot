using System.Collections.Generic;

namespace StatusUpdateBot.SpreadSheets
{
    public interface ISpreadSheet
    {
        IList<IList<object>> GetAllRows(string sheet, bool ignoreCache = false);

        (int id, IList<object>) FindRow(string sheet, string searchFor, int searchIn = 0);

        bool FindAndUpdateRow(string sheet, string searchFor, int searchIn, Dictionary<int, string> cells);

        bool UpdateOrCreateRow(string sheet, string searchFor, int searchIn, Dictionary<int, string> cells);

        void LoadCache(string[] sheets);

        void RemoveCache(string[] sheets = null);

        void EnableBatchUpdate();

        void ExecuteBatchUpdate();
    }
}