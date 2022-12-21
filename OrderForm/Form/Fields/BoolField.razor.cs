using Microsoft.AspNetCore.Components;
using OrderForm.Data;
using System.Diagnostics.CodeAnalysis;

namespace OrderForm.Form.Fields
{
	public partial class BoolField : FieldComponent<bool>
	{
		public BoolField() : base() {
			ValueExpression = () => ValueObject.BoolValue;
		}

		protected override bool CompareToValue(bool value) {
			return Value == value;
		}
		//protected bool? Value { 
		//	get => (bool?)ValueObject.Value; 
		//	set => ValueObject.Value = value; 
		//}

		protected override void OnInitialized() {
			base.OnInitialized();
			if (OwnerSection != null) {
				ValueChanged = EventCallback.Factory.Create<bool>(ValueObject, (value) => ValueObject.BoolValue = value);
				CurrentValue = ValueObject.BoolValue;

				// Since bool has false as default value, setting CurrentValue to false does not trigger an initial validation check
				if(!Value) {
					ReValidate();
				}
			}
		}
		public override bool ValidateFieldvalue() => true; // Checkbox can either be selected or not, nothing to validate.
		protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out bool result, [NotNullWhen(false)] out string? validationErrorMessage) {
			throw new NotImplementedException();
		}

	}
}
