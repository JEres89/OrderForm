using BlazorDateRangePicker;
using System.Runtime.Serialization;

namespace OrderForm.Data
{

	public class FieldValue 
	{
		public string? FieldIdentifier { get; init; }
		public FieldTypes FieldType { get; init; }

		public decimal NumberValue { get; set; } = default;
		public string? StringValue { get; set; }
		public bool BoolValue { get; set; }
		public DateRange? DateValue { get; set; }
		public Address? AddressValue { get; set; }
		public Person? PersonValue { get; set; }
		public ProductCategory? ProductValue { get; set; }
		public string[]? ChoiceValue { get; set; }

		//public void GetObjectData(SerializationInfo info, StreamingContext context) {
		//	info.AddValue()
		//}

		internal bool HasValue() {
			switch (FieldType) {
				case FieldTypes.Number:
				case FieldTypes.Price:
					return NumberValue != default;
				case FieldTypes.Text:
				case FieldTypes.Multiline:
				case FieldTypes.Phone:
				case FieldTypes.Email:
				case FieldTypes.Url:
					return StringValue != default;
				case FieldTypes.Boolean:
					return BoolValue;
				case FieldTypes.Date:
				case FieldTypes.Duration:
					return DateValue != default;
				case FieldTypes.Address:
					return AddressValue != default;
				case FieldTypes.Choice:
					return ChoiceValue != default;
				case FieldTypes.Person:
					return PersonValue != default;
				case FieldTypes.Product:
					return ProductValue != default;
				case FieldTypes.Info:
				case FieldTypes.Sum:
				default:
					break;
			}
			return false;
		}
		public bool IsMatch(string queryString) {
			switch (FieldType) {
				//case FieldTypes.Number:
				//	break;
				//case FieldTypes.Price:
				//	break;
				case FieldTypes.Email:
				case FieldTypes.Url:
				case FieldTypes.Text:
				case FieldTypes.Multiline:
					return StringValue?.Contains(queryString) ?? false;
				case FieldTypes.Boolean:
					return BoolValue.ToString() == queryString;
				//case FieldTypes.Date:
				//	break;
				//case FieldTypes.Duration:
				//	break;
				//case FieldTypes.Phone:
				//	break;
				//case FieldTypes.Address:
				//	break;
				case FieldTypes.Choice:
					return ChoiceValue?.Contains(queryString) ?? false;
				//case FieldTypes.Person:
				//	break;
				case FieldTypes.Product:
					int id;
					if (int.TryParse(queryString, out id)) {
						return ProductValue?.CategoryId == id || (ProductValue?.SelectedProducts.ContainsKey(int.Parse(queryString)) ?? false);
					}
					else {
						return ProductValue?.Name == queryString || (ProductValue?.LongName.Contains(queryString) ?? false);
					}
				//case FieldTypes.Info:
				//	break;
				default:
					break;
			}
			return false;
		}
	}
}
