using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;
using OrderForm.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.JSInterop;
using System.Text;

namespace OrderForm.Form.Fields
{
	public partial class TextField : FieldComponent<string>
	{
		//[Inject] IJSRuntime JSRuntime { get; set; }

		public TextField() : base() {
			ValueExpression = () => ValueObject.StringValue;
		}
		protected override bool CompareToValue(string? value) {
			return Value == value;
		}
		private string? inputValue {
			get => Value;
			set {
				if (value == null || value == string.Empty) {
					CurrentValue = null;
				}
				else if (ValidateInput(value, out string validatedString)) {
					CurrentValue = validatedString;
				}
				else {
					validationMessages?.Clear();
					AddValidationMessage(formatValString);
					PropagateChange();
					//_ = JSRuntime.InvokeVoidAsync("setElementValue", HtmlId, Value);
				}
			}
		}

		//private string textValue;
		//[Phone]
		//private string phoneValue {
		//	get => Value;
		//	set {
		//		var filteredString = Regex.Replace(value, @"[^\+\d\(\)\s.-]", "");
		//		if (Regex.IsMatch(value, formatPattern)) {
		//			Value = filteredString;
		//		}
		//		else {
		//			_ = JSRuntime.InvokeVoidAsync("setElementValue", HtmlId, Value);
		//		}
		//	}
		//}
		//[EmailAddress]
		//private string emailValue {
		//	get => Value;
		//	set {
		//		if (Regex.IsMatch(value, formatPattern)) {
		//			Value = value;
		//		}
		//		else {
		//			Value = Value;

		//		}
		//	}
		//}
		//[Url]
		//private string urlValue {
		//	get => Value;
		//	set {
		//		if (Regex.IsMatch(value, formatPattern)) {
		//			Value = value;
		//		}
		//		else {
		//			Value = Value;
		//		}
		//	}
		//}

		private string? formatPattern;
		private string filterPattern = string.Empty;
		private string htmlType = "text";
		private string formatValString;

		private bool ValidateInput(string value, out string result) {
			if (Config!.InputType == FieldTypes.Text || Config.InputType == FieldTypes.Multiline || Regex.IsMatch(value, formatPattern)) {
				result = value;
				return true;
			}
			else {
				var filteredString = Regex.Replace(value, filterPattern, "");
				if (Regex.IsMatch(filteredString, formatPattern)) {
					result = filteredString;
					return true;
				}

			}
			result = string.Empty;
			return false;
		}

		protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string result, [NotNullWhen(false)] out string? validationErrorMessage) {
			throw new NotImplementedException();
		}

		protected override void OnInitialized() {
			base.OnInitialized();
			if (OwnerSection != null) {
				ValueChanged = EventCallback.Factory.Create<string>(ValueObject, (value) => ValueObject.StringValue = value);
				CurrentValue = ValueObject.StringValue;
			}

			Config.Constraints?.TryGetValue("InputFormat", out formatPattern);
			switch (Config.InputType) {
				case FieldTypes.Email:
					formatPattern ??= @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$";
					htmlType = "email";
					Placeholder = Config.Placeholder ?? "Ex. your.name@your-company.com";
					formatValString = "Must be a well-formed email address";
					break;
				case FieldTypes.Phone:
					formatPattern ??= @"^(\+\d?\d?\d?\s?)?(\(?\d?\d?\d?\d?\)?)[\s.-]?(\d?\d?\d?)[\s.-]?(\d?\d?\d?\d?)[\s.-]?\d?\d?\d?$";
					filterPattern = @"[^\+\d\(\)\s.-]";
					htmlType = "tel";
					Placeholder = Config.Placeholder ?? "Ex. +46 760 567432";
					formatValString = "Must be a well-formed phone number";
					break;
				case FieldTypes.Url:
					formatPattern ??= @"[(http(s)?):\/\/(www\.)?a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,6}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)";
					htmlType = "url";
					Placeholder = Config.Placeholder ?? "Ex. https://your-company.com";
					formatValString = "Must be a well-formed URL";
					break;
				default:
					filterPattern = string.Empty;
					htmlType = "text";
					formatValString = string.Empty;
					break;
			}
			formatPattern ??= string.Empty;
		}
		public override bool ValidateFieldvalue() => Value != null;
	}
}
