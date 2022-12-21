using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OrderForm.Data;
using static OrderForm.Data.FieldOptionStrings;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace OrderForm.Form.Fields
{
	// Should be separated to one int and one decimal component,
	// too little to gain for the extra complexity
	public partial class NumberField : FieldComponent<decimal>
	{
		private float step;
		private bool isNegative = false;

		private string _currency;

		private string currency { 
			get => _currency; 
			set {
				if (_currency != value) { StateHasChanged(); }
				_currency = value;
				currencyRate = Model.GetCurrencyRate(value);
			}
		}
		private decimal currencyRate;

		[Parameter]
		public bool asInlineField { get; set; } = false;

		public NumberField() : base() {
			ValueExpression = () => ValueObject.NumberValue;
		}

		[Range(0, int.MaxValue)]
		internal int? numberValue {
			get {
				var val = Value;
				return val == 0m ? null : (int)val;
			}
			set {
				CurrentValue = value == null ? 0m : Math.Max((decimal)value,0);
			}
		}
		internal decimal? currencyValue {
			get {
				var val = Value;
				return val == 0m ? null : SetCorrectSign(val/currencyRate);
			}
			set => CurrentValue = value == null ? 0m : SetCorrectSign((decimal)value*currencyRate);
		}

		private decimal SetCorrectSign(decimal value) {
			if ((isNegative && value > 0) || (!isNegative && value < 0)) {
				value *= -1;
			}
			return value;
		}

		protected override void ConditionsChanged() {
			if (Config!.InputType == FieldTypes.Sum) {
				Value = 0;
				foreach (var condition in conditionValues!.Values) {
					if (condition?.ProductValue != null) {
						foreach (var item in condition.ProductValue.SelectedProducts) {
							var (planPrice, yearlyFactor) = Model.GetPrices(item.Value.quantity, item.Value.price, item.Value.plan);

							Value += planPrice*yearlyFactor;
						}
					}
				}
			}
		}
		protected override bool CompareToValue(decimal value) {
			return Math.Abs(Value) == Math.Abs(value);
		}

		protected override void OnInitialized() {
			base.OnInitialized();
			//step = Config!.InputType == Data.FieldTypes.Price ? 0.01f : 1;
			Placeholder = Config.Placeholder;
			if (OwnerSection != null) {
				ValueChanged = EventCallback.Factory.Create<decimal>(ValueObject, (value) => { ValueObject.NumberValue = value; ValueObject.BoolValue = isNegative; });
				CurrentValue = ValueObject.NumberValue;
			}
			if (Config!.InputType != FieldTypes.Number) {
				RegisterDependency(INVOICE_CURRENCY, (currencyField => currency = currencyField.FieldType == FieldTypes.Choice ? currencyField.ChoiceValue?[0]??DEFAULT_CURRENCY : currencyField.StringValue?? DEFAULT_CURRENCY), Config!.InputType == FieldTypes.Sum);
			}
			if(Config!.InputType == FieldTypes.Sum) {
				if (_condValid) {
					ConditionsChanged();
				}
			}
			if (Config!.Constraints?.TryGetValue("Negative", out var negative)??false) {
				isNegative = negative! == bool.TrueString;
			}
		}
		public override bool ValidateFieldvalue() {
			if (Value == 0m) {
				return false;
			}
			if (Config.InputType == FieldTypes.Number) {
				// Todo: Add validationmessage for fractional number ??
				return (Value > 0) && (Decimal.Truncate(Value) == Value);
			}
			else {
				return Value != 0 && (isNegative == (Value < 0));
			}
		}

		protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out decimal result, [NotNullWhen(false)] out string? validationErrorMessage) {
			throw new NotImplementedException();
		}
	}
}
