using Newtonsoft.Json;
using NPOI.HPSF;
using OrderForm.Data.Json;
using OrderForm.Pages;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using static NPOI.HSSF.UserModel.HeaderFooter;
using static OrderForm.Data.FormConfig;

namespace OrderForm.Data
{
	public class OrderFileManager {
		static string path = ".\\orders\\";
		static string fileEnding = ".json";
		private static int lastId = 100000;

		internal static JsonSerializerSettings jsonSettings = new JsonSerializerSettings {
			DateFormatString = @"yyyy'-'MM'-'dd",
			NullValueHandling = NullValueHandling.Ignore,
			DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
			Formatting = Formatting.Indented,

		};
		private static Dictionary<int,(string fileName, ModelProps props)> files = new();
		private static List<int> filesInUse = new();
		private static List<LoadOrder> fileListeners = new();

		internal static bool SaveToFile(dynamic jsonObj) {
			ModelProps props = jsonObj.ModelProps;
			var fileName = $"{path}{props.orderName}_{props.orderId}{fileEnding}";
			if (!System.IO.Directory.Exists(path)) {
				System.IO.Directory.CreateDirectory(path);
			}
			else {
				LoadFileMetadata();
			}
			if (files.TryGetValue(props.orderId, out var oldOrder)) {
				if (oldOrder.props.orderName != props.orderName) {
					// ask if to keep old order with same Id but different name
					// and change Id of the new order
				}
				else if (System.IO.File.Exists(oldOrder.fileName)){
					// if same Id and same ordername, ask to replace?
					System.IO.File.Delete(oldOrder.fileName);
				}
			}
			if (lastId <= props.orderId) {
				lastId = props.orderId + 1;
			}
			files[props.orderId] = (fileName, props);
			var writer = System.IO.File.CreateText(fileName);
			writer.Write(JsonConvert.SerializeObject(jsonObj, jsonSettings));

			writer.Flush();
			writer.Close();
			return true;
		}

		internal static Dictionary<string, dynamic?>? LoadFromFile(int id) {
			if (files.Count == 0) {
				LoadFileMetadata(false);
			}
			if (files.TryGetValue(id, out var file)) {
				if (filesInUse.Contains(id)) {
					return null;
				}
				filesInUse.Add(id);
				var fileReader = new StreamReader(files[id].fileName);
				var fileString = fileReader.ReadToEnd();
				fileReader.Close();
				var reader = new JsonTextReader(new StringReader(fileString));

				Dictionary<string, dynamic?> load = new();
				load["ModelProps"] = reader.SelectByName<ModelProps>("ModelProps");
				load["sectionConfigs"] = reader.SelectByName<Dictionary<int, SectionConfig>>("sectionConfigs");
				load["fields"] = reader.SelectByName<Dictionary<int, Dictionary<int, FieldValue>>>("fields");

				try {
					load["comments"] = reader.SelectByName<Dictionary<string, (string, bool)>>("comments");
				}
				catch {
					reader.Close();
					reader = new JsonTextReader(new StringReader(fileString));
					load["comments"] = TryParseOldComments(reader.SelectByName<Dictionary<string, string>>("comments", false));
				}

				load["productCategories"] = reader.SelectByName<Dictionary<string, Dictionary<int, Product>>>("productCategories");
				load["currencyRates"] = reader.SelectByName<Dictionary<string, decimal>>("currencyRates");
				load["plans"] = reader.SelectByName<List<string>>("plans");
				load["planFactors"] = reader.SelectByName<Dictionary<string, (decimal planFactor, int yearlyFactor)>>("planFactors");
				load["regions"] = reader.SelectByName<Dictionary<string, (string name, string nativeName, string phonePrefix)>>("regions");
				load["activeSections"] = reader.SelectByName<List<int>>("activeSections");

				reader.Close();

				return load;
			}
			else {
				return null;
			}
		}
		private static Dictionary<string, (string, bool)>? TryParseOldComments(Dictionary<string, string>? obj) {
			if (obj == null) { return null; }
			Dictionary<string, (string, bool)> result = new();
			foreach (var item in obj) {
				(string, bool) comment = (item.Value, false);
				result.Add(item.Key, comment);
				// More generic form if fallback type is Dictionary<string, dynamic>
				//if (item.Value is string text) {
				//	(string, bool) comment = (text, false);
				//	result.Add(item.Key, comment);
				//}
				//else {
				//	(string, bool) comment = (item.Value.ToString(), false);
				//	result.Add(item.Key, comment);
				//}
			}
			return result;
		}
		internal static List<(int id, ModelProps props, bool inUse)> GetOrderInfo() {
			if(files.Count == 0) {
				LoadFileMetadata();
			}
			List<(int, ModelProps, bool)> propList = new();
			foreach (var order in files) {
				propList.Add((order.Key, order.Value.props, filesInUse.Contains(order.Key)));
			}
			return propList;
		}
		internal static bool OrderExists(int id) {
			if(!files.ContainsKey(id)) {
				LoadFileMetadata(false);
			}
			return files.ContainsKey(id);
		}
		internal static bool OrderInUse(int id) {
			return filesInUse.Contains(id);
		}
		internal static int GetNextId() {
			LoadFileMetadata(false);
			lastId++;
			return lastId;
		}
		internal static void DiscardingOrder(int orderId) {
			if (!files.ContainsKey(orderId) && lastId == orderId) {
				lastId--;
			}
			else if(filesInUse.Contains(orderId)) {
				filesInUse.Remove(orderId);
			}
		}

		private static void LoadFileMetadata(bool reloadAll = true) {
			if(reloadAll) {
				files.Clear();
			}
			if (!System.IO.Directory.Exists(path)) {
				System.IO.Directory.CreateDirectory(path);
				return;
			}
			Dictionary<int, (string fileName, ModelProps props)> newFileList = new();
			foreach (var fileName in System.IO.Directory.EnumerateFiles(path)) {
				
				var nameParts = fileName.Split('_','.','\\');
				for (int i = nameParts.Length-1; i > -1; i--) {
					var namePart = nameParts[i];
					if (namePart.All(c => char.IsDigit(c))) {
						int orderId = int.Parse(namePart);
						if (lastId <= orderId) {
							lastId = orderId;
						}
						if (!reloadAll && files.ContainsKey(orderId)) {
							newFileList[orderId] = (fileName, files[orderId].props);
							break;
						}
						var reader = new JsonTextReader(new StreamReader(fileName));
						
						ModelProps? props = reader.SelectByName<ModelProps>("ModelProps");
						newFileList[orderId] = (fileName, props!);
						reader.Close();
						break;
					}
					else if (i == 0) {
						Console.WriteLine("Invalid file name for order: '" + fileName + "'");
					}
				}
			}
			files.Clear();
			files = newFileList;
		}

	}
}
