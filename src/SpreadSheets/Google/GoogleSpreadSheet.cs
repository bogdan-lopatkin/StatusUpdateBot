using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;

namespace StatusUpdateBot.SpreadSheets.Google
{
    public class GoogleSpreadSheets : ISpreadSheet
    {
        private readonly Dictionary<string, IList<IList<object>>> _cache = new();
        private readonly List<ValueRange> _pendingUpdates = new();
        private readonly SheetsService _service;
        private readonly string _spreadSheetId;
        private bool _isBatchUpdatesEnabled;

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public GoogleSpreadSheets(string spreadSheetId, string configurationPath = "token.json")
        {
            _spreadSheetId = spreadSheetId;
            _service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = CreateUserCredentialFromConfiguration(configurationPath),
                ApplicationName = "Status update bot"
            });
        }

        public void LoadCache(string[] sheets)
        {
            foreach (var (key, value) in GetAllRows(sheets)) _cache[key] = value;
        }

        public void RemoveCache(string[] sheets = null)
        {
            if (sheets == null)
                _cache.Clear();
            else
                foreach (var sheet in sheets)
                    _cache.Remove(sheet);
        }

        public void EnableBatchUpdate()
        {
            _isBatchUpdatesEnabled = true;
        }

        public void ExecuteBatchUpdate()
        {
            BatchUpdateValuesRequest requestBody = new()
            {
                Data = _pendingUpdates,
                ValueInputOption = "RAW"
            };

            _service.Spreadsheets.Values.BatchUpdate(requestBody, _spreadSheetId).Execute();
            _pendingUpdates.Clear();
            _isBatchUpdatesEnabled = false;
        }

        public IList<IList<object>> GetAllRows(string sheet, bool ignoreCache = false)
        {
            if (ignoreCache == false && _cache.ContainsKey(sheet))
                return _cache[sheet];

            var response = _service.Spreadsheets.Values.Get(_spreadSheetId, sheet).Execute();
            var values = response.Values;

            return values;
        }

        public (int id, IList<object>) FindRow(string sheet, string searchFor, int searchIn = 0)
        {
            var values = GetAllRows(sheet);

            if (values is not {Count: > 0}) return (-1, null);

            var index = 1;
            foreach (var row in values)
            {
                if (row.Count > searchIn && row[searchIn].ToString() == searchFor)
                    return (index, row);

                index++;
            }

            return (-1, null);
        }

        public bool FindAndUpdateRow(string sheet, string searchFor, int searchIn, Dictionary<int, string> cells)
        {
            var (rowId, row) = FindRow(sheet, searchFor, searchIn);

            if (rowId == -1)
                return false;

            row = UpdateRowCells(row, cells);
            UpdateRow(sheet, rowId, row);

            return true;
        }

        public bool UpdateOrCreateRow(string sheet, string searchFor, int searchIn, Dictionary<int, string> cells)
        {
            var (rowId, row) = FindRow(sheet, searchFor, searchIn);

            row = UpdateRowCells(row ?? new List<object>(), cells);

            if (rowId == -1)
                AppendRow(sheet, row);
            else
                UpdateRow(sheet, rowId, row);

            return true;
        }

        private Dictionary<string, IList<IList<object>>> GetAllRows(string[] sheets)
        {
            List<string> ranges = new(sheets);

            var request = _service.Spreadsheets.Values.BatchGet(_spreadSheetId);
            request.Ranges = ranges;

            var valueRanges = request.Execute().ValueRanges;
            Dictionary<string, IList<IList<object>>> resultValues = new();

            foreach (var valueRange in valueRanges)
                resultValues[valueRange.Range.Split("!").First()] = valueRange.Values;

            return resultValues;
        }

        private void UpdateRow(string sheet, int rowId, IList<object> row)
        {
            ValueRange valueRange = new()
            {
                MajorDimension = "ROWS",
                Values = new List<IList<object>> {row},
                Range = $"{sheet}!A" + rowId
            };

            if (_isBatchUpdatesEnabled)
            {
                _pendingUpdates.Add(valueRange);
                return;
            }

            var update = _service.Spreadsheets.Values.Update(valueRange, _spreadSheetId, valueRange.Range);
            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            update.Execute();
        }

        private void AppendRow(string sheet, IList<object> row)
        {
            ValueRange valueRange = new()
            {
                MajorDimension = "ROWS",
                Values = new List<IList<object>> {row}
            };

            var append = _service.Spreadsheets.Values.Append(valueRange, _spreadSheetId, $"{sheet}!A1");

            append.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;

            append.Execute();
        }

        private UserCredential CreateUserCredentialFromConfiguration(string configurationPath)
        {
            return GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromFile("credentials.json").Secrets,
                new[] {SheetsService.Scope.Spreadsheets},
                "user",
                CancellationToken.None,
                new FileDataStore(configurationPath, true)).Result;
        }

        private void UpdateRowCell(IList<object> row, int cellToUpdate, string newCellData)
        {
            while (cellToUpdate > row.Count - 1) row.Add("");

            row[cellToUpdate] = newCellData;
        }

        private IList<object> UpdateRowCells(IList<object> row, Dictionary<int, string> cells)
        {
            foreach (var (key, value) in cells) UpdateRowCell(row, key, value);

            return row;
        }
    }
}