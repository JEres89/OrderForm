using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using static OrderForm.Data.FieldOptionStrings;
using OrderForm.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace OrderForm.Form.Fields
{
	public partial class ChoiceField : FieldComponent<string[]?>
	{
		[Inject] IJSRuntime JSRuntime { get; set; }

		public ChoiceField() : base() {
			ValueExpression = () => ValueObject.ChoiceValue;
		}
		/// <summary>
		/// ToDo: refactor Choices into Dictionary<string key, string value> to accept 
		/// dictionaries as parameter (in particular country selector from AddressField)
		/// </summary>
		[Parameter]
		public string[] Choices { get; set; }
		private int maxChoices = 1;
		private List<int> selectionList;
		private bool asDropDown = false;
		private bool multiple = false;
		private Dictionary<string, object> options = new();
		private string? selectionHtmlId;

		protected override bool CompareToValue(string[]? value) {
			string[]? notNull;
			if (Value == value) { return true; }
			else if (Value == null ^ value == null) {
				notNull = Value != null ? Value : value!;
				if (notNull.Any(str => str != string.Empty)) {
					return false;
				}
				return true;
			}
			else if (Value!.Length != value!.Length) { return false; }

			foreach (var choice in value) {
				if (!Value.Contains(choice)) {
					return false;
				}
			}
			return true;
		}
		#region Selection
		private void ToggleSelection(int choiceId) {
			if (selectionList.Contains(choiceId)) {
				selectionList.Remove(choiceId);
			}
			else if (selectionList.Count < maxChoices) {
				selectionList.Add(choiceId);
			}
			ApplySelection();
		}
		private void SelectChangeEvent(ChangeEventArgs e) {
			List<int> sel = new();
			if (e.Value is string choiceId) {
				if (int.TryParse(choiceId, out var id)) {
					sel.Add(id);
				}
			}
			else if (e.Value is string[] choiceIds) {
				for (int i = 0; i < Math.Min(maxChoices, choiceIds.Length); i++) {
					if (int.TryParse(choiceIds[i], out var id)) {
						sel.Add(id);
					}
				}
			}

			if(sel.Count == 0) {
				selectionList.Clear();
				ApplySelection();
				return;
			}

			if (maxChoices == 1 || (sel.Count > 1 && (sel.Count + selectionList.Count) > maxChoices)) {
				selectionList.Clear();
				selectionList.AddRange(sel.Take(maxChoices));
			}
			else if (sel.Count == 1) {
				ToggleSelection(sel[0]);
				return;
			}
			else {
				selectionList.AddRange(sel.Where(id => !selectionList.Contains(id)).Take(maxChoices - selectionList.Count));
			}
			ApplySelection();
		}
		private void RemoveSelection(int choiceId) {
			if (selectionList.Remove(choiceId)) {
				ApplySelection();
			}
		}
		private void ApplySelection() {
			string[]? newValue = null;
			if (multiple && !string.IsNullOrEmpty(selectionHtmlId)) {
				JSRuntime.InvokeVoidAsync("SetSelections", selectionHtmlId, selectionList.ToArray());
			}
			if (selectionList.Count == 0) {
				CurrentValue = null;
			}
			else if (maxChoices == 1) {
				newValue = new[] { Choices[selectionList.FirstOrDefault()] };
				CurrentValue = newValue;
			}
			else {
				newValue = new string[Math.Min(maxChoices, selectionList.Count)];
				for (int i = 0; i < newValue.Length; i++) {
					newValue[i] = Choices[selectionList[i]];
				}
				CurrentValue = newValue;
			}
		}
		#endregion

		protected override Func<FieldValue> GetProviderFunc() {
			if (maxChoices == 1) {
				return () => new() { FieldType = FieldTypes.Text, StringValue = Value?[0] };
			}
			else {
				return () => new() { FieldType = FieldTypes.Choice, ChoiceValue = Value };
			}
		}

		protected override void OnInitialized() {
			base.OnInitialized();
			if (OwnerSection != null) {
				ValueChanged = EventCallback.Factory.Create<string[]?>(ValueObject, (value) => ValueObject.ChoiceValue = value);
				CurrentValue = ValueObject.ChoiceValue;
			}
			if (Choices == null) {
				if (Config!.Constraints?.TryGetValue("Choices", out string choiceString) ?? false) {
					if (choiceString.Contains(':')) {
						Choices = Model.GetTableKeys(choiceString.Split(':'));
					}
					else {
						Choices = choiceString.Split(',');
					}
				}
				else {
					throw new ArgumentException($"No source of Choices for {DisplayName} could be found in {Config.Constraints}");
				}
			}

			if (Config!.Constraints?.TryGetValue("Multiple", out string choiceCount) ?? false) {
				int.TryParse(choiceCount, out maxChoices);
			}

			if (Config!.Constraints?.TryGetValue(DEP_PROVIDER, out string dependencyString) ?? false) {
				if (!string.IsNullOrEmpty(dependencyString)) {
					// Todo: if multiselect set type to choice and add full string array
					if (Model.SetDependencyProvider(this, dependencyString, GetProviderFunc())) {
						isProvider = true;
						(providerFor ??= new()).Add(dependencyString);
					}
				}
			}

			selectionList = new List<int>(Math.Min(maxChoices, Choices.Length));
			if (Value != null && Value.Length	> 0) {
				for (int i = 0; i < Choices.Length; i++) {
					if (Value.Contains(Choices[i])) {
						selectionList.Add(i);
					}
				}
				ApplySelection();
			}
			else if (Config!.Constraints!.TryGetValue("default", out string defChoice)) {
				foreach (var i in defChoice.Split(',')) {
					if (selectionList.Count < maxChoices) {
						selectionList.Add(int.Parse(i));
					}
					else {
						break;
					}
				}
				ApplySelection();
			}
			//else {
			//	Value = new string[maxChoices];
			//}
			if (Choices.Length > 5) {
				asDropDown = true;
				multiple = maxChoices > 1;//options.Add("multiple", maxChoices > 1);
				selectionHtmlId = HtmlId + (maxChoices == 1 ? hid_choiSingle : hid_choiMulti);
			}
		}

		public override bool ValidateFieldvalue() => Value != null && (Value.Length > 0) && (Value.Length <= maxChoices);

		protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string[] result, [NotNullWhen(false)] out string? validationErrorMessage) {
			throw new NotImplementedException();
		}
	}
}
