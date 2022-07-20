using System;
using System.Collections.Generic;

namespace StatusUpdateBot.SpreadSheets
{
    public interface ICachedSpreadSheet: ISpreadSheet
    {
        IList<IList<object>> GetAllRows(string sheet, bool ignoreCache);
        
        void LoadCache(string[] sheets);

        void RemoveCache(string[] sheets = null);

        void EnableBatchUpdate();

        void ExecuteBatchUpdate();
    }
}