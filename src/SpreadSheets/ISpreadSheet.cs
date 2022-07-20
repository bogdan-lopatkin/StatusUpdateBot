using System;
using System.Collections.Generic;

namespace StatusUpdateBot.SpreadSheets
{
    public interface ISpreadSheet: ICloneable
    {
        IList<IList<object>> GetAllRows(string sheet);

        (int id, IList<object>) FindRow(string sheet, string searchFor, int searchIn = 0);

        bool FindAndUpdateRow(string sheet, string searchFor, int searchIn, Dictionary<int, object> cells);

        bool UpdateOrCreateRow(string sheet, string searchFor, int searchIn, Dictionary<int, object> cells);
    }
}