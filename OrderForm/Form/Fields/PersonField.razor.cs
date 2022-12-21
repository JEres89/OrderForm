using static OrderForm.Data.FieldOptionStrings;
using static OrderForm.Data.FormConfig.SectionConfig;
using BlazorDateRangePicker;
using OrderForm.Data;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Reflection;

namespace OrderForm.Form.Fields
{
	public partial class PersonField : FieldComponent<Person?>
	{

		public PersonField() : base() {
			ValueExpression = () => ValueObject.PersonValue;
		}
		// Flagindexes (defaults) : property
		// i = 0 (y) : First name
		// i = 1 (y) : Last name
		// i = 2 (n) : Birthdate
		// i = 3 (y) : Phone number
		// i = 4 (y) : Email address
		// i = 5 (n) : Work address
		// i = 6 (n) : Home address
		
		private string flags;
		private bool useOrderCountry = false;

		private FieldConfig? birthConfig;
		private DateField? dateRef;
		private FieldConfig? phoneConfig;
		private TextField? phoneRef;
		private FieldConfig? emailConfig;
		private TextField? emailRef;
		private FieldConfig? workConfig;
		private AddressField? workRef;
		private FieldConfig? homeConfig;
		private AddressField? homeRef;

		#region setgetters
		private string? FirstName {
			get => Value?.FirstName;
			set {
				if (Value?.FirstName != value) {
					(Value ??= new()).FirstName = value;
					ValidateChange();
					PropagateChange();
				}
			}
		}
		private string? LastName {
			get => Value?.LastName;
			set {
				if (Value?.LastName != value) {
					(Value ??= new()).LastName = value;
					ValidateChange();
					PropagateChange();
				}
			}
		}
		private DateRange? BirthDate {
			get => Value?.BirthDate;
			set {
				if (Value?.BirthDate != value) {
					(Value ??= new()).BirthDate = value;
					ValidateChange();
					PropagateChange();
				}
			}
		}
		private string? PhoneNumber {
			get => Value?.PhoneNumber;
			set {
				if (Value?.PhoneNumber != value) {
					(Value ??= new()).PhoneNumber = value;
					ValidateChange();
					PropagateChange();
				}
			}
		}
		private string? Email {
			get => Value?.Email;
			set {
				if (Value?.Email != value) {
					(Value ??= new()).Email = value;
					ValidateChange();
					PropagateChange();
				}
			}
		}
		private Address? WorkAddress {
			get => Address.Copy(Value?.WorkAddress);
			set {
				if (Value?.WorkAddress != value) {
					(Value ??= new()).WorkAddress = Address.Copy(value);
					ValidateChange();
					PropagateChange();
				}
			}
		}
		private Address? HomeAddress {
			get => Address.Copy(Value?.HomeAddress);
			set {
				if (Value?.HomeAddress != value) {
					(Value ??= new()).HomeAddress = Address.Copy(value);
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

		protected override bool CompareToValue(Person? value) {
			return Value == value;
		}
		protected override void OnInitialized() {
			base.OnInitialized();
			if (Config!.Constraints?.TryGetValue(ORDER_COUNTRY, out string globalCountry) ?? false) {
				if (useOrderCountry = globalCountry == bool.TrueString) {
					SetCountryDependency();
				}
			}
			if (!(Config.Constraints?.TryGetValue("RequiredFlags", out flags) ?? false)) {
				flags = "yynyynn";
			}
			if (OwnerSection != null) {
				ValueChanged = EventCallback.Factory.Create<Person?>(ValueObject, (value) => ValueObject.PersonValue = value);
				CurrentValue = ValueObject.PersonValue;
			}
			if (flags[2] == 'y') {
				birthConfig = new FieldConfig() { DisplayName = "Birthdate", Required = true, InputType = Data.FieldTypes.Date, Constraints = new( new KeyValuePair<string, string>[] { new("Tempus", "past")} ) };
			}
			if (flags[3] == 'y') {
				phoneConfig = new FieldConfig() { DisplayName = "Phone number", Required = true, InputType = Data.FieldTypes.Phone };
			}
			if (flags[4] == 'y') {
				emailConfig = new FieldConfig() { DisplayName = "Email address", Required = true, InputType = Data.FieldTypes.Email };
			}
			if (flags[5] == 'y') {
				workConfig = new FieldConfig() { DisplayName = "Work address", Required = true, InputType = Data.FieldTypes.Address };
			}
			if (flags[6] == 'y') {
				homeConfig = new FieldConfig() { DisplayName = "Home address", Required = true, InputType = Data.FieldTypes.Address };
			}
		}

		public override bool ValidateFieldvalue() {
			if (Value == null) {
				return false;
			}
			if (flags[0] == 'y') {
				if (string.IsNullOrEmpty(Value?.FirstName)) {
					return false;
				}
			}
			if (flags[1] == 'y') {
				if (string.IsNullOrEmpty(Value?.LastName)) {
					return false;
				}
			}
			if (flags[2] == 'y') {
				if (!dateRef?.IsValid??false) {
					return false;
				}
			}
			if (flags[3] == 'y') {
				if (!phoneRef?.IsValid ?? false) {
					return false;
				}
			}
			if (flags[4] == 'y') {
				if (!emailRef?.IsValid ?? false) {
					return false;
				}
			}
			if (flags[5] == 'y') {
				if (!workRef?.IsValid ?? false) {
					return false;
				}
			}
			if (flags[6] == 'y') {
				if (!homeRef?.IsValid ?? false) {
					return false;
				}
			}
			return true;
		}

		protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out Person result, [NotNullWhen(false)] out string? validationErrorMessage) {
			throw new NotImplementedException();
		}
	}
}
