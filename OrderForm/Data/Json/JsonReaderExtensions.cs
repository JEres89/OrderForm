using Newtonsoft.Json;
using static OrderForm.Data.FormConfig;
using static OrderForm.Data.FormConfig.SectionConfig;

namespace OrderForm.Data.Json
{
	public static class JsonReaderExtensions
	{
		public static T? SelectByName<T>(this JsonReader jsonReader, string name, bool isNextObject = true) {
			JsonSerializer serializer = new JsonSerializer();
			do {
				if (jsonReader.TokenType == JsonToken.PropertyName) {
					if (jsonReader.Path.EndsWith(name)) {
						jsonReader.Read();
						return serializer.Deserialize<T>(jsonReader);
					}
					else if(isNextObject) {
						return default;
					}
				}
			} while (jsonReader.Read());
			return default;
		}

		//public static SectionConfig? ParseSectionConf(this JsonReader jsonReader, Dictionary<string, string> constMap) {
		//	var fieldConfigs = new List<FieldConfig?>();
		//	JsonSerializer serializer = new JsonSerializer();
		//	while (jsonReader.Read()) {
		//		jsonReader.
		//		if (jsonReader.Path.Contains('@')
		//				&& (jsonReader.TokenType == JsonToken.StartObject || jsonReader.TokenType == JsonToken.StartArray)) {
		//			fieldConfigs.Add(serializer.Deserialize<SectionConfig.FieldConfig>(jsonReader));
		//		}
		//	}
		//	return default;
		//}
		//public static T? ParseConfigConstRef<T>(this JsonReader jsonReader, Dictionary<string, string> constMap) {

		//	JsonSerializer serializer = new JsonSerializer();

		//	while (jsonReader.Read()) {
		//		if (jsonReader.TokenType == JsonToken.StartObject || jsonReader.TokenType == JsonToken.StartArray) {
		//			jsonReader.
		//			if (jsonReader.Path.Contains('@')) {
		//				return serializer.Deserialize<T>(jsonReader);
		//			}
		//			if (true) {

		//			}

		//		}

		//	}
		//	return default;
		//}
	}
}
