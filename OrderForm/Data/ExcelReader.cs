using System.Data;
using System.Diagnostics.CodeAnalysis;
using NPOI.SS.UserModel;
using NPOI.Util.ArrayExtensions;
using NPOI.XSSF.UserModel;
using static OrderForm.Data.ExcelReader;
using static OrderForm.Data.FieldOptionStrings;

namespace OrderForm.Data
{
	internal sealed class ExcelReader
	{
		private static readonly string defaultFile = "excel_inData.xlsx";

		private static readonly string[] tableNames = { "PriceList", "Currencies", "Plans", "Regions" };
		private static Dictionary<string, ExcelReader> _fileReaders;

		static void ReadExcelData(string? fileName) {
			ExcelReader reader;
			fileName ??= defaultFile;
			if (!(_fileReaders ??= new()).TryGetValue(fileName, out reader)) {
				if (!System.IO.File.Exists(".\\" + fileName)) {
					Console.WriteLine("Excel file does not exist: "+ fileName);
					return;
				}
				reader = new ExcelReader(fileName);
				_fileReaders.Add(fileName, reader);
				reader.Read();
			}
		}

		internal static Dictionary<int, Product>? GetProducts(string? fileName, string categoryName) {
			fileName ??= defaultFile;
			if (!(_fileReaders?.ContainsKey(fileName) ?? false)) {
				ReadExcelData(fileName);
			}

			if (_fileReaders!.TryGetValue(fileName, out var reader)) {
				if (reader.productCategories.TryGetValue(categoryName, out var value)) {
					return value;
				}
				Console.WriteLine("Category " + categoryName + " does not exist in file " + fileName);
			}
			return null;
		}
		internal static Dictionary<string, decimal>? GetCurrencies(string? fileName) {
			fileName ??= defaultFile;
			if (!(_fileReaders?.ContainsKey(fileName) ?? false)) {
				ReadExcelData(fileName);
			}

			if (_fileReaders!.TryGetValue(fileName, out var reader)) {
				return reader.currencyRates;
			}
			return null;
		}
		internal static Dictionary<string, (decimal planFactor, int yearlyFactor)>? GetPlans(string? fileName) {
			fileName ??= defaultFile;
			if (!(_fileReaders?.ContainsKey(fileName) ?? false)) {
				ReadExcelData(fileName);
			}

			if (_fileReaders!.TryGetValue(fileName, out var reader)) {
				return reader.planFactors;
			}
			return null;
		}
		internal static Dictionary<string, (string name, string nativeName, string phonePrefix)>? GetRegions(string? fileName) {
			fileName ??= defaultFile;
			if (!(_fileReaders?.ContainsKey(fileName) ?? false)) {
				ReadExcelData(fileName);
			}

			if (_fileReaders!.TryGetValue(fileName, out var reader)) {
				return reader.regions;
			}
			return null;
		}

		private ExcelReader(string fileName) {
			this.fileName = fileName;
		}

		private readonly string fileName;

		private Dictionary<string, Dictionary<int, Product>> productCategories;
		private Dictionary<string, decimal> currencyRates;
		private Dictionary<string, (decimal planFactor, int yearlyFactor)> planFactors;
		private Dictionary<string, (string name, string nativeName, string phonePrefix)> regions;

		private void Read() {
			FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			XSSFWorkbook wb;
			try {
				wb = new XSSFWorkbook(fs);
				wb.Close();
			}
			catch {
				Console.WriteLine("Bad excel file: " + fileName);
				return;
			}
			wb.MissingCellPolicy = MissingCellPolicy.RETURN_BLANK_AS_NULL;
			ReadPriceList(wb);
			ReadCurrencies(wb);
			ReadPlans(wb);
			ReadRegions(wb);
			//ReadCurrenciesAndPlans(wb.GetTable(currencyTableName));
			//ReadCountries(wb.GetTable(countryTableName));

		}
		//private XSSFTable[] GetTables(XSSFWorkbook wb) {
		//	XSSFTable[] tables = new XSSFTable[tableNames.Length];
		//	for (int i = 0; i < tables.Length; i++) {
		//		tables[i] = wb.GetTable(tableNames[i]);
		//	}
		//	return tables;
		//}

		private void ReadPriceList(XSSFWorkbook wb) {
			var table = wb.GetTable(tableNames[0]);
			var sheet = wb.GetSheet(table.SheetName);
			var yStart = table.StartCellReference.Row + 1; // First row is header (column names)
			var xStart = table.StartCellReference.Col;
			var rows = table.RowCount - 1; // yStart + 1 => rows - 1
			productCategories = new();

			for (int i = yStart; i < yStart + rows;) {
				var row = sheet.GetRow(i);
				//var cells = row.Cells.Take(xStart..(xStart + 7)).ToArray();
				ICell?[] cells = new ICell[7];
				for (int cx = xStart; cx < xStart + 7; cx++) {
					cells[cx-xStart] = row.GetCell(cx);
				}

				if (!string.IsNullOrEmpty(cells[0]?.StringCellValue)
					/*cells.Any(cell => !string.IsNullOrEmpty(cell.StringCellValue)) && row.FirstCellNum == xStart*/) {
					string catName = cells[0]!.StringCellValue;
					Dictionary<int, Product> categoryRows = new();
					while (i < yStart + rows) {
						if (ReadPriceRow(catName, cells, out var value)) {
							categoryRows.TryAdd(value.Value.Key, value.Value.Value);
							i++;
							if (i < (yStart + rows)) {
								row = sheet.GetRow(i);
								//cells = row.Cells.Take(xStart..(xStart + 7)).ToArray();
								cells = new ICell[7];
								for (int cx = xStart; cx < xStart + 7; cx++) {
									cells[cx-xStart] = row.GetCell(cx);
								}
							}
							else {
								break;
							}
						}
						else {
							break;
						}
					}
					productCategories.Add(catName, categoryRows);
				}
				else
					i++;
				continue;
			}
		}
		private bool ReadPriceRow(string catName, ICell?[] row, [NotNullWhen(true)] out KeyValuePair<int, Product>? value) {
			value = null;
			int id;
			string? req = null;
			decimal? price = null;
			string plan = PLAN_RECURRING;
			bool single = false;
			string name = null;
			string desc = null;

			ICell? cell;
			if ((cell = row[0]) != null) {
				if (cell.CellType == CellType.String && cell.StringCellValue == catName) {
					name = catName;
				}
				else
					return false;
			}
			if ((cell = row[1]) == null) {
				return false;
			}
			else {
				var cellVal = ReadCellAsNumeric(cell);
				if (double.IsNaN(cellVal)) {
					return false;
				}
				id = (int)cellVal;
			}
			if ((cell = row[2]) != null) {
				req = ReadCellAsString(cell);
			}
			if ((cell = row[3]) != null) {
				var cellVal = ReadCellAsNumeric(cell);
				if (double.IsNaN(cellVal)) {
					return false;
				}
				price = cellVal == 0d ? null : (decimal)cellVal;
			}
			if ((cell = row[4]) != null) {
				single = ReadCellAsString(cell)!.First() == 'Y';
			}
			if ((cell = row[5]) != null) {
				plan = ReadCellAsString(cell) ?? PLAN_RECURRING;
			}
			if ((cell = row[6]) == null) {
				if (name == null) {
					name = "";
				}
				desc = "";
			}
			else {
				var cellVal = ReadCellAsString(cell);
				if (string.IsNullOrEmpty(cellVal)) {
					name = name ?? "";
					desc = "";
				}
				else {
					if (name == null) {
						name = cellVal;
						desc = "";
					}
					else {
						desc = cellVal;
					}
				}
			}
			value = new(id, new(id, req, price, plan, single, name, desc));

			return true;
		}

		private void ReadCurrencies(XSSFWorkbook wb) {
			var table = wb.GetTable(tableNames[1]);
			var sheet = wb.GetSheet(table.SheetName);
			var yStart = table.StartCellReference.Row + 1; // First row is header (column names)
			var xStart = table.StartCellReference.Col;
			var rows = table.RowCount - 1; // yStart + 1 => rows - 1
			currencyRates = new();

			for (int i = yStart; i < yStart + rows; i++) {
				var row = sheet.GetRow(i);
				//var cells = row.Cells.Take(xStart..(xStart + 7)).ToArray();
				ICell?[] cells = new ICell[2];
				for (int cx = xStart; cx < xStart + 2; cx++) {
					cells[cx-xStart] = row.GetCell(cx);
				}
				if (cells[0] == null || cells[1] == null) {
					continue;
				}

				string currencyName = cells[0]!.StringCellValue;
				decimal currencyRate = Convert.ToDecimal(cells[1]!.NumericCellValue);
				currencyRates.Add(currencyName, currencyRate);
			}
		}

		private void ReadPlans(XSSFWorkbook wb) {
			var table = wb.GetTable(tableNames[2]);
			var sheet = wb.GetSheet(table.SheetName);
			var yStart = table.StartCellReference.Row + 1; // First row is header (column names)
			var xStart = table.StartCellReference.Col;
			var rows = table.RowCount - 1; // yStart + 1 => rows - 1
			int columns = 3;
			planFactors = new();

			for (int i = yStart; i < yStart + rows; i++) {
				var row = sheet.GetRow(i);
				//var cells = row.Cells.Take(xStart..(xStart + 7)).ToArray();
				ICell?[] cells = new ICell[columns];
				for (int cx = xStart; cx < xStart + columns; cx++) {
					cells[cx-xStart] = row.GetCell(cx);
				}

				if (cells.Any(c => c == null)) {
					continue;
				}

				string planName = cells[0]!.StringCellValue;
				decimal planFactor = (decimal)cells[1]!.NumericCellValue;
				int yearlyFactor = cells[2]!.CellType == CellType.Numeric ? (int)cells[2]!.NumericCellValue : 1;
				planFactors.Add(planName, (planFactor, yearlyFactor));
			}
		}
		private void ReadRegions(XSSFWorkbook wb) {
			var table = wb.GetTable(tableNames[3]);
			var sheet = wb.GetSheet(table.SheetName);
			var yStart = table.StartCellReference.Row + 1; // First row is header (column names)
			var xStart = table.StartCellReference.Col;
			var rows = table.RowCount - 1; // yStart + 1 => rows - 1
			int columns = 4;
			regions = new();

			for (int i = yStart; i < yStart + rows; i++) {
				var row = sheet.GetRow(i);
				//var cells = row.Cells.Take(xStart..(xStart + 7)).ToArray();
				ICell?[] cells = new ICell[columns];
				for (int cx = xStart; cx < xStart + columns; cx++) {
					cells[cx-xStart] = row.GetCell(cx);
				}
				if (cells[0] == null || cells[1] == null) {
					continue;
				}

				string regionCode = cells[0]!.StringCellValue;
				string englishName = cells[1]!.StringCellValue;
				string nativeName = cells[2]?.StringCellValue??"";
				string callingCode = cells[3]?.StringCellValue??"";

				regions.Add(regionCode, (englishName, nativeName, callingCode));
			}
		}

		private double ReadCellAsNumeric(ICell cell) => cell.CellType == CellType.Numeric ? cell.NumericCellValue : cell.CellType == CellType.String ? double.TryParse(cell.StringCellValue, out var result) ? result : double.NaN : double.NaN;

		private string? ReadCellAsString(ICell cell) => cell.CellType == CellType.String ? cell.StringCellValue : cell.CellType == CellType.Numeric ? cell.NumericCellValue.ToString() : null;

	}
}
