using Microsoft.AspNetCore.Components;
using System.Globalization;
using OrderForm.Data;
using static OrderForm.Data.FormConfig;
using static OrderForm.Data.FieldOptionStrings;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Collections.ObjectModel;

namespace OrderForm.Form.Fields
{
	public partial class AddressField : FieldComponent<Address?>
	{

		public AddressField() : base() {
			ValueExpression = () => ValueObject.AddressValue;
		}
		protected override void SetCountry(FieldValue value) {
			(Value ??= new()).CountryCode = CountryCode = value.StringValue;
		}
		private bool useOrderCountry = false;
		private bool isFullAddress = true;
		[Parameter]
		public ReadOnlyDictionary<string, (string name, string nativeName, string phonePrefix)>? CountryList { get; set; }
		private List<string> optionList;
		
		#region get-setters
		private (string name, string nativeName, string phonePrefix)? _country {
			get => useOrderCountry ?
/*true*/   string.IsNullOrEmpty(CountryCode) ? null : Model.GetRegion(CountryCode) :
/*false*/  string.IsNullOrEmpty(Value?.CountryCode) ? null : CountryList![Value!.CountryCode];
		}
		private string? _countryCode {
			get => useOrderCountry ? CountryCode : Value?.CountryCode;
			set {
				if (!useOrderCountry && (value == null || CountryList!.ContainsKey(value))) {
					if (Value?.CountryCode != value) {
						(Value ??= new()).CountryCode = value;
						CountryCode = value;
						ValidateChange();
						PropagateChange();
					}
				}
			}
		}

		private string? Street {
			get => Value?.Street;
			set {
				if (Value?.Street != value) {
					(Value ??= new()).Street = value;
					ValidateChange();
					PropagateChange();
				}
			}
		}
		private string? City {
			get => Value?.City;
			set {
				if (Value?.City != value) {
					(Value ??= new()).City = value;
					ValidateChange();
					PropagateChange();
				}
			}
		}
		private string? PostCode {
			get => Value?.PostCode;
			set {
				if (Value?.PostCode != value) {
					(Value ??= new()).PostCode = value;
					ValidateChange();
					PropagateChange();
				}
			}
		}
		private void ValidateChange() {
			if (_isValid != ValidateFieldvalue()) {
				ReValidate();
			}
		}
		#endregion

		protected override bool CompareToValue(Address? value) {
			return Value == value;
		}

		protected override void OnInitialized() {
			base.OnInitialized();
			if (Model == null) {
				throw new ArgumentNullException("Model", "An AddressField needs a Model provided to interpret country codes");
			}
			if (OwnerSection != null) {
				ValueChanged = EventCallback.Factory.Create<Address?>(ValueObject, (value) => ValueObject.AddressValue = value);
				CurrentValue = ValueObject.AddressValue;
			}
			if (Config!.Constraints?.TryGetValue("FullAddress", out string fullAddress) ?? false) {
				isFullAddress = fullAddress == bool.TrueString;
			}
			if (Config!.Constraints?.TryGetValue(ORDER_COUNTRY, out string globalCountry) ?? false) {
				if (useOrderCountry = globalCountry == bool.TrueString) {
					SetCountryDependency();
				}
				else {
					if (globalCountry == THIS_STRING) {
						if (Model.SetDependencyProvider(this, ORDER_COUNTRY, () => new() { FieldType = FieldTypes.Text, StringValue = CountryCode! })) {
							isProvider = true;
							(providerFor ??= new()).Add(ORDER_COUNTRY);
						}
					}
					CountryList = Model.GetRegions();
				}
			}
			else {
				if (!(useOrderCountry != string.IsNullOrEmpty(CountryCode))) {
					CountryList = Model.GetRegions();
				}
				else {
					(Value ??= new()).CountryCode = CountryCode;
				}
			}
		}

		public override bool ValidateFieldvalue() {
			if (Value == null) {
				return false;
			}
			if (Value.CountryCode != null && (CountryList?.ContainsKey(Value.CountryCode) ?? Model.GetRegion(Value.CountryCode)!= null)) {
				bool isvalid = true;
				if (isFullAddress) {
					if (string.IsNullOrEmpty(Value.Street)) {
						isvalid = false;
					}
					if (string.IsNullOrEmpty(Value.City)) {
						isvalid = false;
					}
					if (string.IsNullOrEmpty(Value.PostCode)) {
						isvalid = false;
					}
				}
				return isvalid;
			}

			return false;
		}

		protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out Address result, [NotNullWhen(false)] out string? validationErrorMessage) {
			throw new NotImplementedException();
		}
	}
}
