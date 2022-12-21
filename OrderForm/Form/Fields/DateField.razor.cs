using BlazorDateRangePicker;
using Microsoft.AspNetCore.Components;
using OrderForm.Data;
using System.Diagnostics.CodeAnalysis;

namespace OrderForm.Form.Fields
{
	//public partial class DateField : IFieldComponent<DateTime> {
	//	[Parameter]
	//	[DataType(DataType.DateTime)]
	//	public DateTime Value { get; set; } = DateTime.Today;
	//	[Parameter]
	//	public EventCallback<DateTime> ValueChanged { get; set; }

	//	public override void SaveData(FormModel model) {
	//		throw new NotImplementedException();
	//	}

	//	protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out DateTime result, [NotNullWhen(false)] out string? validationErrorMessage) {
	//		throw new NotImplementedException();
	//	}

	//	//protected override void OnInitialized() {
	//	//	base.OnInitialized();
	//	//}
	//}
	public partial class DateField : FieldComponent<DateRange?>
	{

		public DateField() : base() {
			ValueExpression = () => ValueObject.DateValue;
		}
		[Parameter]
		public DateTimeOffset? StartDate { 
			get => Value?.Start;
			set {
				if (value.HasValue) {
					(Value ??= new()).Start = FormatDate(value.Value);
					//Span<char> formatDate = new();
					//if(value.Value.TryFormat(formatDate, out var charsWritten, Placeholder, null)) {
					//	if(DateTimeOffset.TryParseExact(formatDate, Placeholder, null, default, out var result)) {
					//		(Value ??= new()).Start = result;
					//	}
					//	else {
					//		(Value ??= new()).Start = value.Value;
					//	}
					//}
					//else {
					//	(Value ??= new()).Start = value.Value;
					//}

					if (_isSingleDate) {
						Value.End = Value.Start;
						PropagateChange();
					}
				}
			}
		}
		[Parameter]
		public DateTimeOffset? EndDate {
			get => Value?.End;
			set {
				if (value.HasValue) {
					(Value ??= new()).End = value.Value;
				}
			}
		}
		[Parameter]
		public DateTimeOffset? MinDate { get; set; }

		[Parameter]
		public DateTimeOffset? MaxDate { get; set; }

		private int smallestTimeUnit = 2;
		private bool _isSingleDate = true;
		private TimeSpan _span;
		//private TimeSpan _step;
		private bool _strictSpan = true;
		private bool relativeToOther = false;
		private DateTimeOffset? previousOtherDate;

		private DateTimeOffset FormatDate(DateTimeOffset date) {
			var offset = date.Offset;
			int year = date.Year;
			int month = smallestTimeUnit > 0 ? date.Month : 1;
			int day = smallestTimeUnit > 1 ? date.Day : 1;
			int hour = smallestTimeUnit > 2 ? date.Hour : 0;

			return new(year, month, day, hour, 0, 0, offset);
		}

		protected override void ConditionsChanged() {
			if (_condValid) {
				if (relativeToOther) {

					var isDateType = (FieldTypes type) => type == FieldTypes.Date || type == FieldTypes.Duration;
					var otherField = conditionValues!.First(val => isDateType(val.Value!.FieldType));
					var otherDate = otherField.Value!.DateValue!.Start;
					if (previousOtherDate != null && previousOtherDate == otherDate) {
						return;
					}

					MinDate = _strictSpan ? FormatDate(otherDate.Add(_span)) : FormatDate(otherDate.AddMonths(1));
					StartDate = previousOtherDate == null ? otherDate.Add(_span) : StartDate!.Value.Add(FormatDate(otherDate) - FormatDate(previousOtherDate!.Value));
					previousOtherDate = otherDate;

				}
			}
		}

		protected override bool CompareToValue(DateRange? value) {
			return Value?.Start == value?.Start && Value?.End == value?.End;
		}
		protected override void OnInitialized() {
			base.OnInitialized();
			_isSingleDate = Config.InputType == FieldTypes.Date;

			if (Config!.Constraints?.TryGetValue("Span", out var span) ?? false) {
				char mod;
				if (span[0] != '-' & !char.IsNumber(span[0])) {
					mod = span[0];
					_strictSpan = mod != '?';
					span = span[1..];
				}
				_span = TimeSpan.ParseExact(span, "%d", null, 0);
				relativeToOther = (_isSingleDate && _span.Days != 0);

				#region unused _step code 
				/*// Only used in one case as of now (Subscription end-month)
						// Constraints = new(new KeyValuePair<string, string>[] { new("Step", "1m,>10d") })
						_step = new TimeSpan(10, 0, 0, 0);
						if (Config!.Constraints?.TryGetValue("Step", out var step) ?? false) {
							var split = step.Split(',');

							char mod = split[0][^1];
							int num = int.Parse(split[0][..^1]);

							if (!char.IsNumber(span[0])) {
								mod = span[0];
								_strictSpan = mod != '?';
								span = span[1..];
							}
							_step = new TimeSpan();
						}
						else {
							_step = new TimeSpan(10, 0, 0, 0);
						}*/ 
				#endregion
			}

			if (Config!.Constraints?.TryGetValue("Tempus", out var tempus) ?? false) {
				if (tempus == "past") {
					MaxDate = DateTimeOffset.Now;
				}
				else if (tempus == "future") {
					MinDate = DateTimeOffset.Now;
				}
			}

			if (Config.Placeholder != null) {
				Placeholder = Config.Placeholder;
				smallestTimeUnit = 
					Placeholder.Contains('M', StringComparison.OrdinalIgnoreCase) ? 
					Placeholder.Contains('d', StringComparison.OrdinalIgnoreCase) ? 
					Placeholder.Contains('h', StringComparison.OrdinalIgnoreCase) ? 
					3 : 2 : 1 : 0;
			}
			else {
				Placeholder = "yyyy-MM-dd";// + (_isSingleDate?"": " - yyyy-MM-dd");
			}

			if (OwnerSection != null) {
				ValueChanged = EventCallback.Factory.Create<DateRange?>(ValueObject, (value) => ValueObject.DateValue = value);
				CurrentValue = ValueObject.DateValue ??= new() { Start = DateTimeOffset.Now, End = DateTimeOffset.Now.Add(_span) };
			}
			else if (Value == null) {
				if (_span.Days != 0 && _isSingleDate) {
					CurrentValue = new() { Start = DateTimeOffset.Now, End = DateTimeOffset.Now.Add(_span) };
				}
				else {
					CurrentValue = new() { Start = DateTimeOffset.Now, End = DateTimeOffset.Now.Add(_span) };
				}
			}

			if(relativeToOther) {
				ConditionsChanged();
			}
		}

		public override bool ValidateFieldvalue() {
			return true;
		}

		protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out DateRange result, [NotNullWhen(false)] out string? validationErrorMessage) {
			throw new NotImplementedException();
		}
	}
}
