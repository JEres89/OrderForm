using Newtonsoft.Json;
using NPOI.Util;
using OrderForm.Data.Json;
using System.Text;

namespace OrderForm.Data
{
	public static class OrderConfigurationManager
	{

		internal static JsonSerializerSettings jsonConfigSettings = new JsonSerializerSettings {
			DateFormatString = @"yyyy'-'MM'-'dd",
			NullValueHandling = NullValueHandling.Include,
			DefaultValueHandling = DefaultValueHandling.Populate,
			Formatting = Formatting.Indented,
			
		};

		private static string path = ".\\forms\\";
		private static string fileEnding = ".json";
		public static string defaultConfName = "DefaultConfiguration";
		private static FormConfig.SectionConfig[]? defaultConfiguration;
		public static string? defaultConfVersion { get; private set; }
		private static FormVars vars = new();


		private static void SaveConfig(string? name, FormConfig.SectionConfig[]? config) {
			name ??= defaultConfName;
			dynamic dynJson = new {
				ConfigurationVersion = $"{name}_{DateTimeOffset.Now.ToString("yyyyMMddhhmmss")}",
				FormConfig = config??FormConfig.GetSectionConfigs()
			};

			var filePath = $"{path}{name}{fileEnding}";
			if (!System.IO.Directory.Exists(path)) {
				System.IO.Directory.CreateDirectory(path);
			}

			var writer = System.IO.File.CreateText(filePath);

			writer.Write(JsonConvert.SerializeObject(dynJson, jsonConfigSettings));

			writer.Flush();
			writer.Close();
		}

		public static dynamic? LoadConfig(string? name) {
			if ((name == null || name == defaultConfName) && defaultConfiguration != null) {
				return new {
					ConfigurationVersion = defaultConfVersion,
					FormConfig = defaultConfiguration
				};
			}
			var fileName = $"{path}{name??defaultConfName}{fileEnding}";
			if (!System.IO.Directory.Exists(path)) {
				return null;
			}
			else if(!File.Exists(fileName)) {
				if (name == null) {
					SaveConfig(null, null);
				}
				else
					return null;
			}

			string configString = File.ReadAllText(fileName);
			if (configString == "") {
				Console.WriteLine("Config is empty "+fileName);
				return null;
			}

			var reader = new JsonTextReader(new StringReader(configString));
			var version = reader.SelectByName<string>("ConfigurationVersion");
			reader.Close();
			var start = configString.IndexOf("FormConfig")-1;

			string filteredConfig = ReplaceConstRef(configString, start);
			reader = new JsonTextReader(new StringReader(filteredConfig));

			var config = reader.SelectByName<FormConfig.SectionConfig[]>("FormConfig", false);
			reader.Close();
			if (name == null) {
				defaultConfVersion = version;
				defaultConfiguration = config;
			}
			return new {
				ConfigurationVersion = version,
				FormConfig = config
			};
		}

		private static string ReplaceConstRef(string confString, int start) {

			var getName = (ReadOnlySpan<char> span, int start, out int endPos) => {
				List<char> result = new();
				for (endPos = start; endPos < span.Length; endPos++) {
					char c;
					if ((c = span[endPos]) != '@') {
						if (!char.IsWhiteSpace(c)) {
							result.Add(c);
						}
					}
					else {
						break;
					}
				}
				return new string(result.ToArray());
			};

			StringBuilder filteredConfig = new(confString, 0, start - 1, confString.Length);
			var confSpan = confString.AsSpan();

			for (int readPos = start; readPos < confSpan.Length; readPos++) {
				char c = confSpan[readPos];
				if (c == '@') {
					var constName = getName(confSpan, readPos + 1, out var endPos);
					readPos = endPos;
					filteredConfig.Append((string?)vars.GetType().GetProperty(constName)?.GetValue(vars) ?? constName);
				}
				else {
					filteredConfig.Append(c);
				}
			}
			return filteredConfig.ToString();
		}


		//public static FormVars varsNames = new FormVars() {
		//	CUSTOMER_NAME = "@CUSTOMER_NAME@",
		//	ORDER_DATE = "@ORDER_DATE@",
		//	ORDER_COUNTRY = "@ORDER_COUNTRY@",
		//	INVOICE_CURRENCY = "@INVOICE_CURRENCY@",
		//	INVOICE_PERIOD = "@INVOICE_PERIOD@",
		//	DEP_PROVIDER = "@DEP_PROVIDER@",
		//	DEP_VALID = "@DEP_VALID@",
		//	DEP_CONDITION = "@DEP_VALID@",
		//	PRODUCT_GROUP = "@PRODUCT_GROUP@",
		//	THIS_STRING = "@THIS_STRING@",
		//	DEFAULT = "@DEFAULT@",
		//	TRUE_STRING = "@TRUE_STRING@",
		//	FALSE_STRING = "@FALSE_STRING@"
		//};
	}

	public struct FormVars
	{
		public FormVars() {
		}
		/** 'Singleton' dependencies - a single instance per form **/

		public readonly string CUSTOMER_NAME { get; init; } = "CustomerName";

		public readonly string ORDER_DATE { get; init; } = "OrderDate";
		public readonly string ORDER_COUNTRY { get; init; } = "OrderCountry";

		public readonly string INVOICE_CURRENCY { get; init; } = "InvoiceCurrency";
		public readonly string INVOICE_PERIOD { get; init; } = "InvoicePeriod";


		/** Keywords **/

		// The configured field is the provider for the specified dependency value
		public readonly string DEP_PROVIDER { get; init; } = "Provider";
		// The configured field requires a valid value in field to allow input. Is visible and explicitly stated
		public readonly string DEP_VALID { get; init; } = "ValidField";
		// The configured field is not enabled (hidden and not included in form submition) unless the conditional field is filled. '/' separates identifiers which require one of them, ',' separates groups which are all required, ':' after an identifier specifies the value it must have to be a valid condition
		public readonly string DEP_CONDITION { get; init; } = "ConditionalField";

		public readonly string PRODUCT_GROUP { get; init; } = "ProductGroup";

		public readonly string THIS_STRING { get; init; } = "This";
		public readonly string DEFAULT { get; init; } = "default";
		public readonly string TRUE_STRING { get; init; } = "True";
		public readonly string FALSE_STRING { get; init; } = "False";

		public readonly string DEFAULT_CURRENCY { get; init; } = "SEK";
		public readonly string DEFAULT_COUNTRY { get; init; } = "sv-SE";

		public readonly string PLAN_YEARLY { get; init; } = "Yearly";
		public readonly string PLAN_RECURRING { get; init; } = "Recurring";
		public readonly string PLAN_ONCE { get; init; } = "Once";
		public readonly string PLAN_FREE { get; init; } = "Free";
	}
}
