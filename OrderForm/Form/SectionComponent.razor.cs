using Microsoft.AspNetCore.Components;
using OrderForm.Data;
using OrderForm.Pages;
using static OrderForm.Data.FormConfig.SectionConfig;
using static OrderForm.Data.FormConfig;
using static OrderForm.Data.FieldOptionStrings;
using System;
using OrderForm.Form.Ext;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace OrderForm.Form
{
	public partial class SectionComponent : IDisposable, IFieldListener
	{
		[Parameter]
		[EditorRequired]
		public FormComponent Form { get; set; } = default!;
		[Parameter]
		[EditorRequired]
		public FormModel Model { get; set; } = default!;
		[Parameter]
		[EditorRequired]
		public int SectionId { get; set; } = -1;
		[Parameter]
		public bool Active { get; set; } = false;
		[Parameter]
		public string Name { get; set; }
		public SectionConfig Config { get; private set; } = default!;
		public string HtmlId { get; private set; }

		public readonly Dictionary<int, IFieldComponent> fields = new();
		private List<FieldSet> fieldSets = new();

		protected Dictionary<string, FieldValue?>? conditionValues;

		private bool disposedValue;

		private bool isValid = false;
		public bool IsValid {
			get {
				if (isValid) {
					return isValid;
				}
				isValid = true;
				foreach (var set in fieldSets) {
					isValid &= set.valid;
				}
				return isValid;
			}
		}


		protected override void OnInitialized() {
			if (SectionId == -1) {
				throw new ArgumentNullException($"Section must have an Id, Parameter:{nameof(SectionId)}");
			}
			HtmlId = "S_" + SectionId;
			if (Active) { 
				Activate();
			}
		}
		private void Activate() {
			Form.RegisterSection(this);
			Config = Model.GetSectionConfig(SectionId)??new();
			Model.ActivateSection(Config);
			MakeSets();
			Active = true;
			StateHasChanged();
		}
		protected override void OnParametersSet() {
			base.OnParametersSet();	
		}

		internal void Register(IFieldComponent field) {
			if (!fields.ContainsKey(field.Id)) {
				fields.Add(field.Id, field);
			}
		}
		internal void FieldChanged(IFieldComponent field) {
			if (!(fields.TryGetValue(field.Id, out var val) && val == field)) {
				return;
			}
			for (int i = 0; i < fieldSets.Count; i++) {
				var set = fieldSets[i];
				if (set.fields.ContainsKey(field.Id)) {
					if (set.config != null) {
						bool wasValid = set.valid;
						if (wasValid != IsSetValid(set)) {
							set.htmlClasses = GetSetClasses(set.config, set.valid);
						}
						//if (wasValid != fieldSets[i].valid) {
						//	bool success = true;
						//}
					}
					break;
				}
			}
			StateHasChanged();
		}


		protected (Func<bool>, HashSet<string>) RegisterConditions(string condString) {

			conditionValues ??= new();

			List<Func<bool>> allConditions = new();
			HashSet<string> identifiers = new();


			string[] mustHave = condString.Split(',');

			foreach (string cond in mustHave) {

				string[] anyOf = cond.Split('/');
				List<Func<bool>> anyConditions = new();

				foreach (string value in anyOf) {

					var specValue = value.Split(':');
					var condId = specValue[0];
					identifiers.Add(condId);
					if (specValue.Length > 1) {
						specValue = specValue[1..];
						anyConditions.Add(() =>
							conditionValues!.TryGetValue(condId, out var val) && specValue.Any(s => val?.IsMatch(s) ?? false)
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
				if (!conditionValues.ContainsKey(condId) ) {
					conditionValues.Add(condId, Model.SetConditionalListener(condId, this));
				}
			}
			if (allConditions.Count > 1) {
				return (() => allConditions.All(c => c()), identifiers);
			}
			else {
				return (allConditions[0], identifiers);
			}
		}
		public void ConditionUpdated(string dependencyIdentifier, FieldValue dependencyObject) {
			if (conditionValues?.ContainsKey(dependencyIdentifier) ?? false) {
				conditionValues[dependencyIdentifier] = dependencyObject;
				for (int i = 0; i < fieldSets.Count; i++) {
					var set = fieldSets[i];
					if (set.conditions?.Contains(dependencyIdentifier)??false) {
						if (set.conditionValid != set.conditionCheck!()) {
							set.conditionValid = !set.conditionValid;
							StateHasChanged();
						}
					}
				}
			}
		}


		private void MakeSets() {
			FieldConfig[] configs = Config.Fields;
			FieldConfig? setConfig = null;
			List<FieldSet> sets = new();
			int nextId = 0;
			Dictionary<int, (Type type, Dictionary<string, object> props, FieldConfig config)> fields = new();

			for (int i = 0; i < configs.Length;) {
				if (configs[i].InputType == FieldTypes.Info) {
					setConfig = configs[i];
					i++;
				}
				else {
					while (i < configs.Length && configs[i].InputType != FieldTypes.Info) {
						fields.Add(nextId, (IFieldComponent.GetFieldtypeForConfig(configs[i]), MakeProps(configs[i], nextId), configs[i]));
						nextId++;
						i++;
					}
					sets.Add(new(sets.Count, this, setConfig, fields));
					fields = new();
				}
			}
			fieldSets = sets;
		}
		private Dictionary<string, object> MakeProps(FieldConfig config, int fieldId) {
			var fieldObj = Model.RegisterField(new() { FieldIdentifier = config.AccessName, FieldType = config.InputType }, SectionId, fieldId);
			var props = new Dictionary<string, object> {
				{ "Config", config },
				{ "model", Model },
				{ "OwnerSection", this },
				{ "Id", fieldId },
				{ "ValueObject", fieldObj }
			};
			return props;
		}
		private string GetSetClasses(FieldConfig setConfig, bool valid) {
			return Css_fieldSet + (valid ? (setConfig.Required ? Css_req_valid : Css_opt_valid) : (setConfig.Required ? Css_req_inValid : Css_opt_inValid));
		}
		/**  //private string ParseFieldsetInfo(FieldConfig setConfig, int setIndex) {
			string htmlClasses;

			if (setConfig.DisplayName == "") {
				htmlClasses = "ungrouped";
			}
			else {
				htmlClasses = Css_fieldSet + (valid ? (setConfig.Required ? Css_req_valid : Css_opt_valid) : (setConfig.Required ? Css_req_inValid : Css_opt_inValid));
			}

			// maybe Todo: Refactor below to using the Constraints field in FieldConfig
			// for custom classes and whatnot. Not used anywhere 
			//var getSliceUntil = (ReadOnlySpan<char> s, int i, char end, out string slice) => {
			//	var iEnd = i++;
			//	while (s[iEnd] != end) {
			//		iEnd++;
			//	}
			//	slice = s[i..iEnd].ToString() + ' ';
			//	return iEnd;
			//};
			//if (config.Description != null) {
			//	var span = config.Description.AsSpan();
			//	for (int i = 0; i < span.Length; i++) {
			//		switch (span[i]) {
			//			case '[':
			//				i = getSliceUntil(span, i, ']', out string slice);
			//				htmlClasses += slice;
			//				break;
			//			case '{':
			//				i = getSliceUntil(span, i, '}', out slice);
			//				for (int cat = 1; cat < set.Count; cat++) {
			//					var field = set[i];
			//					if (field.InputType == FieldTypes.Product && field.AsSummary) {

			//					}
			//				}
			//				break;
			//			default:
			//				break;
			//		}
			//	}
			//}

			return htmlClasses;
		}*/
		private bool IsSetValid(FieldSet set, bool checkAll = false) {
			bool valid = true;
			foreach (var fieldConf in set.fields) {
				valid &= fields.TryGetValue(fieldConf.Key, out var field) ? field.IsValid : false;
				//valid &= fields[fieldConf.id].IsValid;
				if (!valid & !checkAll) {
					break;
				}
			}
			if(set.valid != valid) {
				set.valid = valid;
				//fieldSets[setIndex] = set;
			}
			isValid &= valid;
			return valid;
		}

		private record FieldSet {
			public FieldSet(int setIndex, SectionComponent section, FieldConfig? config, Dictionary<int, (Type type, Dictionary<string, object> props, FieldConfig config)> fields) {

				id = setIndex;
				htmlId = section.HtmlId + "_" + setIndex;
				this.fields = fields;

				if (config?.InputType == FieldTypes.Info) {
					this.config = config;
					htmlClasses = Css_fieldSet + (config.Required ? Css_req_inValid : "");
					required = (header = config.DisplayName) == "" ? false : config.Required;

					if (config?.Constraints?.TryGetValue(DEP_CONDITION, out var condString) ?? false) {
						( conditionCheck, conditions ) = section.RegisterConditions(condString);
						conditionValid = conditionCheck();
					}
				}
			}

			public FieldConfig? config = null;
			public int id = 0;
			public string htmlId;
			public bool valid = false;
			public bool conditionValid = true;
			public bool required = false;
			public string htmlClasses = "";
			public string header = "";
			public Dictionary<int, (Type type, Dictionary<string,object> props, FieldConfig config)> fields;

			public HashSet<string>? conditions = null;
			public Func<bool>? conditionCheck = null;
		}


		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {
					// TODO: dispose managed state (managed objects)
					Form = default!;
					foreach (var field in fields.Values) {
						field.Dispose();
					}
					fieldSets.ForEach(set => set.fields.Clear());
					fieldSets.Clear();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}
		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~SectionComponent()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }
		public void Dispose() {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		
	}
}