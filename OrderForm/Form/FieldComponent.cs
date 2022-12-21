using Microsoft.AspNetCore.Components;
using static OrderForm.Data.FormConfig.SectionConfig;
using static OrderForm.Data.FormConfig;
using static OrderForm.Data.FieldOptionStrings;
using Microsoft.AspNetCore.Components.Web;
using OrderForm.Data;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Components.Forms;
using System.Reflection.Metadata;
using OrderForm.Form.Fields;
using NPOI.Util.ArrayExtensions;
using OrderForm.Form.Ext;

namespace OrderForm.Form
{
	public interface IFieldComponent : IDisposable
	{
		[Parameter]
		public FieldConfig Config { get; set; }
		[Parameter]
		public SectionComponent? OwnerSection { get; set; }
		[Parameter]
		[Range(0, int.MaxValue)]
		public int Id { get; set; }
		[Parameter]
		public string HtmlId { get; set; }
		[Parameter]
		public bool Required { get; set; }
		[Parameter]
		public abstract FieldValue ValueObject { get; set; }
		[Parameter]
		public string? CountryCode { get; set; }

		[Required]
		[Range(typeof(bool), "true", "true", ErrorMessage = "")]
		public abstract bool IsValid { get; }
		public abstract bool ReValidate();

		public bool ValidateProvider(string dependencyIdentifier);
		public bool ValidateFieldvalue();
		public void DependencyUpdated(string dependencyIdentifier, FieldValue dependencyObject);
		public void NewDependant(string name, string htmlId, bool required);

		public static Type GetFieldtypeForConfig(FieldConfig config) {
			switch (config.InputType) {
				case Data.FieldTypes.Number:
				case Data.FieldTypes.Price:
				case Data.FieldTypes.Sum:
					return typeof(NumberField);

				case Data.FieldTypes.Text:
				case Data.FieldTypes.Multiline:
				case Data.FieldTypes.Phone:
				case Data.FieldTypes.Email:
				case Data.FieldTypes.Url:
					return typeof(TextField);

				case Data.FieldTypes.Boolean:
					return typeof(BoolField);

				case Data.FieldTypes.Date:
				case Data.FieldTypes.Duration:
					return typeof(DateField);

				case Data.FieldTypes.Address:
					return typeof(AddressField);

				case Data.FieldTypes.Person:
					return typeof(PersonField);

				case Data.FieldTypes.Choice:
					return typeof(ChoiceField);

				case Data.FieldTypes.Product:
					return typeof(ProductField);
			}
			return null;
		}
	}

	
	/// <summary>
	/// Summary:
	///		FieldComponent as a field in a section requires:
	///			- A FieldValue and OwnerSection param set from the SectionComponent
	///			- A FieldConfig
	///			- An Id
	///			- A FormModel
	///			- Required if the FieldSet is marked as such
	///		
	///		FieldComponent as a field in a parent composite field requires:
	///			- A @bind-Value parameter bound to the corresponding value in the
	///				parents FieldValue object (or directly to the Value object i.e.
	///				@bind-Value="Value.PhoneNumber" instead of
	///				@bind-Value="ValueObject.PersonValue.PhoneNumber")
	///				OR explicit binding of Value and an onchange callback
	///			- A FieldConfig
	///			- A HtmlId
	/// </summary>
	/// 
	public abstract class FieldComponent<TValue> : InputBase<TValue>, IFieldComponent, IFieldListener
	{
		[Parameter]
		public FieldConfig Config { get; set; }
		public string? AccessName { get; set; }
		public string? Placeholder { get; set; }
		[Parameter]
		public SectionComponent? OwnerSection { get; set; }
		[Parameter]
		public FormModel Model { get; set; }
		[Parameter]
		[Range(0, int.MaxValue)]
		public int Id { get; set; } = -1;
		[Parameter]
		public string HtmlId { get; set; }
		[Parameter]
		public bool Required { get; set; } = false;
		[Parameter]
		public FieldValue ValueObject { get; set; }
		[Parameter]
		public string? CountryCode { get; set; }
		protected virtual void SetCountry(FieldValue value) { CountryCode = value.StringValue; }
		protected bool asSummary = false;
		private bool disposedValue;

		/// <summary>
		/// Except for maybe initially, CurrentValue is not used by composite Fields since they
		/// use a Class as TValue and changes to their input apply to members of the Class object.
		/// They call to PropagateChange directly.
		/// </summary>
		protected new TValue? CurrentValue {
			get => Value;
			set {
				var hasChanged = !CompareToValue(value);
				if (hasChanged) {
					Value = value;
					ReValidate();
					PropagateChange();
				}
				else if(value == null & !Required){
					ReValidate();
				}
			}
		}
		protected abstract bool CompareToValue(TValue? value);
		protected void PropagateChange() {

			StateHasChanged();
			_ = ValueChanged.InvokeAsync(Value);
			if (OwnerSection != null) {
				OwnerSection.FieldChanged(this);
			}
			EditContext.NotifyFieldChanged(FieldIdentifier);
			if (isProvider) {
				foreach (var dependency in providerFor!) {
					Model.NotifyDependencyListeners(dependency);
				}
			}
			if (asSummary) {
				Model.SummaryFieldUpdated(this);
			}
		}


		#region Validation
		protected bool _isValid = false;
		/// <summary>
		/// Below functionality MOVED TO ReValidate()! 
		/// (Checks whether the field is in a completely valid state. Checks are in the order conditions, dependencies and values, and stops whenever any of them fail.
		/// May create validation messages for any condition which fail.)
		/// </summary>
		public virtual bool IsValid {
			get => _isValid;
			//	if (_isValid) {
			//		return true;
			//	}
			//	validationMessages?.Clear();

			//	if (Required) {
			//		if (!_depValid) {
			//			if (!AreDepValid) {
			//				return false;
			//			}
			//		}
			//		if (Value == null || (Value is string val) && val == string.Empty) {
			//			AddValidationMessage(MSG_REQ_NULL);
			//			return false;
			//		}
			//	}
			//	else {
			//		if (hasDependants) {
			//			if (!dependantFields!.All(f => f.required == false)) {
			//				AddValidationMessage(MSG_REQ_DEP);
			//				return false;
			//			}
			//		}
			//	else if (Value == null || (Value is string val) && val == string.Empty) {
			//			return true;
			//		}
			//	}
			//	return _isValid = ValidateFieldvalue();
			//}
		}
		protected ValidationMessageStore? validationMessages;
		/// <summary>
		/// Checks whether the field is in a completely valid state. Checks are in the order conditions, dependencies and values, and stops whenever any of them fail.
		/// May create validation messages for any condition which fail.
		/// </summary>
		public bool ReValidate() {
			validationMessages?.Clear();
			// If conditions are false, this field is not used, thus always valid
			if (!(_condValid=conditionCheck?.Invoke()??true)) {
				return _isValid = true;
			}
			if (Required) {
				if (!_depValid) {
					if (!AreDepValid(false)) {
						return _isValid = false;
					}
				}
				if (Value == null || (Value is string val) && val == string.Empty) {
					AddValidationMessage(MSG_REQ_NULL);
					return _isValid = false;
				}
			}
			else {
				if (hasDependants) {
					if (!dependantFields!.All(f => f.required == false)) {
						AddValidationMessage(MSG_REQ_DEP);
						return _isValid = false;
					}
				}
				else if (Value == null || (Value is string val) && val == string.Empty) {
					return _isValid = true;
				}
			}
			return _isValid = ValidateFieldvalue();
		}
		/// <summary>
		///		Call to check if field has a valid non-zero Value, regardless of whether the field is required or not.
		/// </summary>
		/// <returns><see langword="true"/> if valid, otherwise <see langword="false"/></returns>
		protected void AddValidationMessage(string message) {
			(validationMessages ??= new(EditContext)).Add(FieldIdentifier, message);
		}
		public abstract bool ValidateFieldvalue();
		#endregion

		#region Dependencies and Conditions
		protected bool isListener = false;

		// Provider variables
		protected bool isProvider = false;
		protected List<string>? providerFor;
		protected bool hasDependants = false;
		protected List<(string name, string id, bool required)>? dependantFields;

		// Dependency variables
		protected bool isDependant = false;
		protected Dictionary<string, Action<FieldValue>?>? dependencies;
		protected bool _depUpdated = false;
		/// <summary>
		/// Only set to false when initially registering a dependency, or when a dependency value changes so no dependency validation happens on render.
		/// </summary>
		protected bool _depValid = true;
		// ToDo: separate Dependency and Condition validation and checks
		protected virtual bool AreDepValid(bool clearMsgs = true) {
			if (clearMsgs) { validationMessages?.Clear(); }
			//if (!_condValid) {
			//	return false;
			//}
			if (_depValid || !isDependant) {
				return _depValid = true;
			}
			else {
				foreach (var identifier in dependencies!.Keys) {
					if (!Model.ValidateDependency(identifier, out var message)) {
						AddValidationMessage(message);
						return _depValid = false;
					}
				}
				return _depValid = true;
			}
		}

		// Condition variables
		protected bool _condValid = true;
		protected Dictionary<string, FieldValue?>? conditionValues;
		protected Func<bool>? conditionCheck;

		// Provider methods
		protected virtual Func<FieldValue> GetProviderFunc() {
			return () => ValueObject;
		}
		public void NewDependant(string name, string htmlId, bool required) {
			(dependantFields ??= new()).Add((name, htmlId, required));
			hasDependants = true;
			StateHasChanged();
		}
		public virtual bool ValidateProvider(string dependencyIdentifier) {
			if (!providerFor?.Contains(dependencyIdentifier) ?? false) {
				return false;
			}
			return ValidateFieldvalue();
			//if(_isValid) {
			//	return true;
			//}
			//validationMessages?.Clear();

			//return _isValid = IsValid;
			//if (dependencyIdentifier == ORDER_COUNTRY) {
			//	return CountryCode != null && Model.Regions.ContainsKey(CountryCode);
			//}
			//else /*if (dependencyIdentifier == AccessName || dependencyIdentifier == INVOICE_CURRENCY)*/ {
			//	return IsValid;
			//}
		}

		// Dependency methods
		protected void SetCountryDependency() {
			RegisterDependency(ORDER_COUNTRY, SetCountry);
		}
		/// <summary>
		/// Registers with the FormModel as a listener and makes this FieldComponent dependant on the provider FieldComponent having a valid value.
		/// </summary>
		/// <param name="identifier">Unique identifier or provider field, <see cref="FieldConfig.AccessName"/> or <see cref="DEP_PROVIDER"/> in <see cref="FieldConfig.Constraints"/></param>
		/// <param name="procedure">What should be done with a new value from the provider. In simple cases this is null since the dependency only require that the field has a valid value. In other cases like with currency, specific implementations are needed in the different fields (see <see cref="ProductField.OnInitialized"/> call of RegisterDependency for example)</param>
		/// <param name="quiet">Wether the listener should not be reported to the provider (for example with non-input fields like <see cref="FieldTypes.Sum"/>. Changing default to <see langword="true"/> will remove all information text about dependendants, but also not validate properly if a required field depends on another optional field.</param>
		protected void RegisterDependency(string identifier, Action<FieldValue>? procedure, bool quiet = false) {
			(dependencies ??= new()).Add(identifier, procedure);
			isDependant = true;
			FieldValue? value;
			if (quiet) {
				value = Model.SetConditionalListener(identifier, this);
			}
			else {
				value = Model.SetDependencyListener(identifier, this);
			}
			isListener = true;
			_depValid = false;
			_depUpdated = true;
			/* initial dep registration, OnParametersSet() WILL run
			if (value != null) {
				procedure?.Invoke(value);
			}
			AreDepValid();
			*/
		}
		public virtual void DependencyUpdated(string dependencyIdentifier, FieldValue dependencyObject) {
			if (dependencies?.TryGetValue(dependencyIdentifier, out var procedure) ?? false) {
				//var wasValid = _depValid;
				_depValid = false;
				_depUpdated = true;
				procedure?.Invoke(dependencyObject);
				//if (wasValid != AreDepValid) {}
				//AreDepValid();
				StateHasChanged();
				AreDepValid();
			}
		}

		// Condition methods
		// Todo?: extract and generalize condition string parser and add to RegisterDependency
		// for complex dependencies.
		protected void RegisterConditions(string condString) {
			List<Func<bool>> allConditions = new();
			List<string> identifiers = new();
			conditionValues = new();
			string[] mustHave = condString.Split(',');

			foreach (string cond in mustHave) {

				string[] anyOf = cond.Split('/');
				List<Func<bool>> anyConditions = new();

				foreach (string value in anyOf) {

					var specValue = value.Split(':');
					var condId = specValue[0];
					if (!identifiers.Contains(condId)) {
						identifiers.Add(condId);
					}
					if (specValue.Length > 1) {
						specValue = specValue[1..];
						anyConditions.Add(() => 
							conditionValues!.TryGetValue(condId, out var val) ? specValue.Any(s => val?.IsMatch(s)??false) : false
						);
					}
					else {
						anyConditions.Add(() => Model.ValidateProviderValue(condId));
					}
				}

				if (anyConditions.Count > 1) {
					allConditions.Add(() => anyConditions.Any(c => c()));
				}
				else {
					allConditions.Add(anyConditions[0]);
				}
			}
			foreach (var condId in identifiers) {
				conditionValues.Add(condId, Model.SetConditionalListener(condId, this));
			}
			if (allConditions.Count > 1) {
				conditionCheck = () => allConditions.All(c => c());
			}
			else {
				conditionCheck = allConditions[0];
			}

			isListener = true;
			_condValid = conditionCheck();
		}
		public virtual void ConditionUpdated(string dependencyIdentifier, FieldValue dependencyObject) {
			if (conditionValues?.ContainsKey(dependencyIdentifier) ?? false) {
				conditionValues[dependencyIdentifier] = dependencyObject;
				if (!(_condValid = conditionCheck!())) {
					// Conditions are false, field is no longer in use, thus always valid
					_isValid = true;
				}
				ConditionsChanged();
				if (OwnerSection != null) {
					OwnerSection.FieldChanged(this);
				}
				StateHasChanged();
			}
			else {
				// If not found in conditions, it's a quiet dependency, as in the discount field which has both a complex condition (any of the related product fields) and a quiet dependency (currency)
				DependencyUpdated(dependencyIdentifier, dependencyObject);
			}
		}
		protected virtual void ConditionsChanged() {}
		#endregion

		#region Overrides
		protected override void OnParametersSet() {
			if (!isDependant)
				return;

			// ToDo: ? This should not be needed, but without it updating dependencies are not working correctly ?
			if(_depUpdated) {
				_depUpdated = false;
				foreach (var dep in dependencies!) {
					if (Model.TryGetDependencyProvider(dep.Key, out FieldValue? dependencyObject)) {
						dep.Value?.Invoke(dependencyObject);
					}
				}
				AreDepValid();
			}
		}

		protected override void OnInitialized() {
			if (Config == null)
				throw new ArgumentNullException(nameof(Config), "Config cannot be null");

			DisplayName = Config.DisplayName;
			AccessName = Config.AccessName;
			Required |= Config.Required;

			if (OwnerSection != null) {
				if (Id < 0)
					throw new ArgumentOutOfRangeException(nameof(Id), "Id must have a positive value");
				OwnerSection.Register(this);
				HtmlId = HtmlId ?? $"S_{OwnerSection.SectionId}-F_{Id}";
				if (asSummary = Config.AsSummary) {
					Model.RegisterSummaryField(this);
				}
			}
			if (!string.IsNullOrEmpty(AccessName)) {
				(providerFor ??= new()).Add(AccessName);
				isProvider = true;
				Model.SetDependencyProvider(this, AccessName, GetProviderFunc());
			}
			if (Config.Constraints?.TryGetValue(DEP_VALID, out var dep) ?? false) {
				string[] dep_fields = dep.Split(',');
				foreach (string dep_field in dep_fields) {
					RegisterDependency(dep_field, null);
				}
			}
			if (Config.Constraints?.TryGetValue(DEP_CONDITION, out var condString) ?? false) {
				RegisterConditions(condString);
			}

		}


		protected override void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {
					// TODO: dispose managed state (managed objects)
					if (isListener) {
						if (isDependant) {
							foreach (var identifier in dependencies!.Keys) {
								Model.DisposingListener(identifier, this);
							}
							dependencies.Clear();
							dependencies = null;
							isDependant = false;
						}
						if (conditionValues != null) {
							foreach (var identifier in conditionValues.Keys) {
								Model.DisposingListener(identifier, this);
							}
							conditionValues.Clear();
							conditionCheck = null;
							conditionValues = null;
						}
						isListener = false;
					}
					if (isProvider) {
						foreach (var identifier in providerFor!) {
							Model.DisposingProvider(identifier);
						}
						providerFor.Clear();
						providerFor = null;
					}
					if (asSummary) {
						Model.DisposingSummaryField(this);
					}
					//EditContext.
					validationMessages?.Clear();
					dependantFields?.Clear();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}
		// TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~FormComponent()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }
		public void Dispose() {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
