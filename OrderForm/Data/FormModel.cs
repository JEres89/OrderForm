using NPOI.Util;
using OrderForm.Form;
using OrderForm.Form.Ext;
using OrderForm.Pages;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using static OrderForm.Data.FieldOptionStrings;
using static OrderForm.Data.FormConfig;

namespace OrderForm.Data
{
	public class FormModel
	{
		public const int ORDER_IN_PROGRESS = 0;
		public const int ORDER_REVIEW = 1;
		public const int ORDER_APPROVED = 2;
		public const int ORDER_ACTIONED = 3;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		private FormModel(ModelProps? formProps) {
			if (formProps == null) {
				return;
			}

			orderId = formProps.orderId;
			orderState = formProps.orderState;
			User = formProps.User;
			orderName = formProps.orderName;
			customerName = formProps.customerName;
			orderDate = formProps.orderDate;
			orderCountry = formProps.orderCountry;
			invoiceCurrency = formProps.invoiceCurrency;
			invoicePlan = formProps.invoicePlan;
			configurationVersion = formProps.configurationVersion;

			Init();
		}
		//public FormModel(int id) : this(id, null) { }
		
		public FormModel(int id, FormComponent? viewer) : this(id, viewer, null) { }

		public FormModel(int id, FormComponent? viewer, string? configName) {
			formViewer = viewer;
			orderId = id;
			var config = OrderConfigurationManager.LoadConfig(configName);
			if (config == null) {
				Console.WriteLine("No config found " + configName);
				return;
			}
			configurationVersion = config.ConfigurationVersion??"";
			SectionConfig[] sections = config.FormConfig;
			sectionConfigs = new();
			foreach (var section in sections) {
				sectionConfigs.Add(section.Id, section);
			}
			activeSections = new();
			fields = new();
			productCategories = new();
			comments = new();

			Init();
		}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

		private void Init() {
			dependencyProviders = new();
			dependencyListeners = new();
			conditionalListeners = new();
			summaryFields = new();

			if ((currencyRates?.Count??0) == 0 ) {
				currencyRates = ExcelReader.GetCurrencies(null) ?? new() {
					{ "EUR", 10 },
					{ "SEK", 1 }
				};
			}
			if ((planFactors?.Count ?? 0) == 0) {
				planFactors = ExcelReader.GetPlans(null) ?? new() {
					{ "Yearly", (1, 1) },
					{ "Monthly", (0.1m, 12) },
					{ "Once", (1, 0) }
				};
			}
			if ((plans??=new()).Count == 0) {
				foreach (var plan in planFactors!) {
					if (plan.Value.yearlyFactor != 0) {
						plans.Add(plan.Key);
					}
				}
			}
			if ((regions?.Count ?? 0) == 0) {
				regions = ExcelReader.GetRegions(null) ?? new() {
					{ "SE", ("Sweden", "Sverige", "+46") },
					{ "EU", ("Europe", "", "") },
					{ "WW", ("Worldwide", "", "") }
				};
			}

			// Set to users culture to localize correctly
			SystemCulture = (CultureInfo)CultureInfo.GetCultureInfo(DEFAULT_COUNTRY).Clone();
			SystemCulture.NumberFormat.CurrencySymbol = "";
			SystemCulture.NumberFormat.CurrencyDecimalDigits = 0;

			keyValueListeners = new() {
				{ CUSTOMER_NAME, v => customerName = v.StringValue },
				{ ORDER_DATE, v => orderDate = v.DateValue != null ? v.DateValue.Start.Date : null },
				{ ORDER_COUNTRY, v => orderCountry = v.StringValue },
				{ INVOICE_CURRENCY, v => invoiceCurrency = v.StringValue },
				{ INVOICE_PERIOD, v => invoicePlan = v.StringValue }
			};
		}

		#region Regioncode 
		//public static Dictionary<string, (CultureInfo, RegionInfo)> AllRegions { get; set; }

		//public Dictionary<string, (CultureInfo, RegionInfo)> Regions {
		//	get => AllRegions;
		//	private set => AllRegions = value;
		//}

		//static Dictionary<string, (CultureInfo, RegionInfo)> GenerateRegions() {
		//	var regionList = new List<(CultureInfo, RegionInfo)>();
		//	foreach (var item in CultureInfo.GetCultures(CultureTypes.SpecificCultures)) {
		//		regionList.Add((item, new RegionInfo(item.Name)));
		//	}
		//	regionList.Sort((x, y) => x.Item2.EnglishName.CompareTo(y.Item2.EnglishName));
		//	var regions = new Dictionary<string, (CultureInfo, RegionInfo)>();
		//	string lastName = "";
		//	foreach (var region in regionList) {
		//		string code = region.Item2.EnglishName;
		//		if (code != lastName) {
		//			lastName = code;
		//			regions.Add(region.Item1.Name, region);
		//		}
		//	}
		//	return regions;
		//}
		#endregion

		#region Props

		internal int orderId { get; init; }
		internal int orderState { get; set; } = 0;
		public bool IsReadOnly { get => orderState > 0; }

		internal string? User { get; set; }
		private string? orderName;

		internal string? customerName { get; private set; }
		internal DateTime? orderDate { get; private set; }
		internal string? orderCountry { get; private set; }

		internal string? invoiceCurrency { get; private set; }
		internal string? invoicePlan { get; private set; }

		internal string configurationVersion { get; private set; }

		internal string? OrderName {
			get => orderName ?? (
					customerName != null ?
					(sWhitespace.Replace(customerName, "-") /*Where(c => char.IsUpper(c)).ToArray())*/ + '_' + orderDate?.ToShortDateString()/*ToString("ddMMyy")*/) : "New-Order_" + orderDate?.ToShortDateString()
				);
			set => orderName = value == "" ? null : value;
		}
		private static readonly Regex sWhitespace = new Regex(@"\s+");

		public CultureInfo SystemCulture { get; private set; }

		private FormComponent? formViewer;

		#endregion


		#region Collections

		private Dictionary<int, SectionConfig> sectionConfigs;
		private List<int> activeSections;
		private Dictionary<int, Dictionary<int, FieldValue>> fields;
		public Dictionary<int, Dictionary<int, FieldValue>>? removedFields;
		/// <summary>
		/// ToDo: Add input and bool value for marking a comment as revised when in progress state,
		/// and removing a comment in review state. Linking to the field the comment is about.
		/// </summary>
		private Dictionary<string, (string text, bool revised)> comments;

		private Dictionary<string, Dictionary<int, Product>> productCategories;
		private Dictionary<string, decimal> currencyRates;
		private List<string> plans;
		private Dictionary<string, (decimal planFactor, int yearlyFactor)> planFactors;
		private Dictionary<string, (string name, string nativeName, string phonePrefix)> regions;

		private Dictionary<string, Action<FieldValue>> keyValueListeners;

		private Dictionary<string, (IFieldComponent source, Func<FieldValue> provider)> dependencyProviders;
		private Dictionary<string, List<IFieldComponent>> dependencyListeners;
		private Dictionary<string, List<IFieldListener>> conditionalListeners;

		private OrderSummary? summaryComp;
		private List<string>? summaryListeners;
		private List<IFieldComponent> summaryFields;

		#endregion


		#region Dependencies
		/// <summary>
		/// 
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the provider was successfully registered.
		/// <br/> <see langword="false"/> if there is a duplicate which means the form is incorrectly configured and might not behave as expected.
		/// </returns>
		internal bool SetDependencyProvider(IFieldComponent source, string identifier, Func<FieldValue> provider) {
			if (dependencyProviders.ContainsKey(identifier)) {
				Console.WriteLine($"Duplicate provider for '{identifier}' found");
				return false;
			}
			dependencyProviders.Add(identifier, (source, provider));
			if (dependencyListeners.TryGetValue(identifier, out var listeners)) {
				foreach (var listener in listeners) {
					source.NewDependant(listener.Config.DisplayName, listener.HtmlId, listener.Required);
				}
			}
			NotifyDependencyListeners(identifier);
			return true;
		}
		public FieldValue? SetDependencyListener(string identifier, IFieldComponent listener) {
			if (dependencyListeners.TryGetValue(identifier, out var listeners)) {
				if (!listeners.Contains(listener)) {
					listeners.Add(listener);
					if (dependencyProviders.TryGetValue(identifier, out var provider)) {
						provider.source.NewDependant(listener.Config.DisplayName, listener.HtmlId, listener.Required);
						return provider.provider.Invoke();
					}
				}
			}
			else {
				listeners = new();
				listeners.Add(listener);
				dependencyListeners.Add(identifier, listeners);
				if (dependencyProviders.TryGetValue(identifier, out var provider)) {
					provider.source.NewDependant(listener.Config.DisplayName, listener.HtmlId, listener.Required);
					return provider.provider.Invoke();
				}
			}
			return null;
		}
		public FieldValue? SetConditionalListener(string identifier, IFieldListener listener) {
			if (conditionalListeners.TryGetValue(identifier, out var listeners)) {
				if (!listeners.Contains(listener)) {
					listeners.Add(listener);
					if (dependencyProviders.TryGetValue(identifier, out var provider)) {
						return provider.provider.Invoke();
					}
				}
			}
			else {
				listeners = new();
				listeners.Add(listener);
				conditionalListeners.Add(identifier, listeners);
				if (dependencyProviders.TryGetValue(identifier, out var provider)) {
					return provider.provider.Invoke();
				}
			}
			return null;
		}

		public bool TryGetDependencyProvider(string identifier, [MaybeNullWhen(false)] out FieldValue value) {
			if (dependencyProviders.TryGetValue(identifier, out var provider)) {
				value = provider.provider.Invoke();
				return true;
			}
			value = null;
			return false;
		}
		public bool ValidateProviderValue(string identifier) {
			if (dependencyProviders.TryGetValue(identifier, out var value)) {
				return value.source.ValidateFieldvalue();
			}
			return false;
		}
		public bool ValidateDependency(string identifier, [NotNullWhen(false), MaybeNullWhen(true)] out string? validationMessage) {
			if (dependencyProviders.TryGetValue(identifier, out var value)) {
				var isvalid = value.source.ValidateProvider(identifier);
				validationMessage = isvalid ? null : string.Format(MSG_DEP_INVALID, identifier);
				return isvalid;
			}
			validationMessage = string.Format(MSG_DEP_MISSING, identifier);
			return false;
		}
		public void NotifyDependencyListeners(string identifier) {

			if (dependencyProviders.TryGetValue(identifier, out var value)) {
				var dependencyObject = value.provider.Invoke();

				if (keyValueListeners.TryGetValue(identifier, out var action)) {
					action.Invoke(dependencyObject);
				}
				if (dependencyListeners.TryGetValue(identifier, out var listeners)) {
					foreach (var dependant in listeners) {
						dependant.DependencyUpdated(identifier, dependencyObject);
					}
				}
				if (conditionalListeners.TryGetValue(identifier, out var conditionals)) {
					foreach (var dependant in conditionals) {
						dependant.ConditionUpdated(identifier, dependencyObject);
					}
				}
				if (summaryListeners?.Contains(identifier) ?? false) {
					summaryComp!.DependencyUpdated(identifier, dependencyObject);
				}
			}
			else {
				throw new InvalidOperationException($"Something went wrong; tried to update an unregistered dependency with identifier \"{identifier}\"");
			}
		}

		internal void DisposingProvider(string identifier) {
			dependencyProviders.Remove(identifier);
		}
		internal void DisposingListener(string identifier, IFieldComponent listener) {
			if (dependencyListeners.TryGetValue(identifier, out var listeners)) {
				listeners.Remove(listener);
			}
		}
		#endregion

		#region View Controls
		public Dictionary<int, string> GetSectionIdentifiers() {
			Dictionary<int, string> result = new();
			foreach (var section in sectionConfigs) {
				result.Add(section.Key, section.Value.Name);
			}
			return result;
		}
		/**
		 * returns true if there are stored values from a previously removed section
		 */
		public bool ActivateSection(SectionConfig section) {
			if (!activeSections.Contains(section.Id)) {
				activeSections.Add(section.Id);
				fields.Add(section.Id, new());
				return removedFields?.ContainsKey(section.Id) ?? false;
			}
			return false;
		}
		/// <summary>
		/// Not used. 
		/// </summary>
		/// 
		public void DisableSection(SectionConfig section) {
			activeSections.Remove(section.Id);
			(removedFields ??= new()).Add(section.Id, fields[section.Id]);
			fields.Remove(section.Id);
		}
		public bool IsSectionActive(int Id) {
			return activeSections.Contains(Id);
		}
		public SectionConfig? GetSectionConfig(int Id) {
			if (!sectionConfigs.TryGetValue(Id, out SectionConfig? config)) {
				Console.WriteLine($"A section with id {Id} does not exist in configuration version {configurationVersion} ");
			}
			return config;
		}
		public FieldValue RegisterField(FieldValue valueObject, int sectionId, int fieldId) {
			if (!activeSections.Contains(sectionId)) {
				throw new ArgumentOutOfRangeException("Section not active");
			}
			if (valueObject.FieldType == FieldTypes.Sum) {
				return valueObject;
			}
			var dictionary = fields[sectionId];
			if (dictionary.TryGetValue(fieldId, out var oldValueObject)) {
				if (oldValueObject != null) {
					return oldValueObject;
				}
				else {
					dictionary[fieldId] = valueObject;
					return valueObject;
				}
			}
			else {
				dictionary.Add(fieldId, valueObject);
				return valueObject;
			}
		}
		#endregion


		#region Getters for fields
		public FieldValue GetValue(int sectionId, int fieldId) {
			if (!activeSections.Contains(sectionId)) {
				throw new ArgumentOutOfRangeException("Section not active");
			}
			return fields[sectionId][fieldId];
		}

		public string[] GetTableKeys(string[] source) {
			switch (source[1]) {
				case "Plans":
					return plans.ToArray();
				case "Currencies":
					return currencyRates.Keys.ToArray();
				default:
					break;
			}
			return null;
		}
		public IReadOnlyDictionary<int, Product>? GetProducts(string categoryName) {
			Dictionary<int, Product>? products;
			if (!productCategories.TryGetValue(categoryName, out products)) {
				products = ExcelReader.GetProducts(null, categoryName);
				if (products != null) {
					productCategories.Add(categoryName, products);
				}
			}
			return products;
		}

		public IReadOnlyDictionary<string, decimal> GetCurrencies() {
			return currencyRates;
		}
		public decimal GetCurrencyRate(string? currency) {
			return currencyRates.TryGetValue(currency ?? invoiceCurrency ?? DEFAULT_CURRENCY, out var value) ? value : 1;
		}
		public IReadOnlyDictionary<string, (decimal planFactor, int yearlyFactor)> GetPlans() {
			return planFactors;
		}
		public decimal GetPlanFactor(string? plan) {
			return planFactors.TryGetValue(plan ?? PLAN_YEARLY, out var value) ? value.Item1 : 1;
		}

		public (decimal planPrice, int yearlyFactor) GetPrices(int quantity, decimal unitPrice, string plan) {
			if (plan == PLAN_RECURRING && TryGetDependencyProvider(INVOICE_PERIOD, out var val)) {
				plan = val?.StringValue ?? plan;
			}
			(decimal planFactor, int yearlyFactor) planFactor =
				planFactors.TryGetValue(plan, out var value) ? value : (1, 1);

			var planPrice = (unitPrice * planFactor.planFactor) * quantity;
			var prices = (planPrice, yearlyFactor: planFactor.yearlyFactor);
			return prices;
		}
		public string ConvertToCurrencyString(decimal value) {
			return value.ToString("C", SystemCulture).Trim();
		}

		public ReadOnlyDictionary<string, (string name, string nativeName, string phonePrefix)> GetRegions() {
			return new ReadOnlyDictionary<string, (string name, string nativeName, string phonePrefix)>(regions);
		}
		public (string name, string nativeName, string phonePrefix)? GetRegion(string code) {
			return regions.TryGetValue(code, out var value) ? value : null;
		}
		#endregion


		#region Summary
		internal void RegisterSummaryComponent(OrderSummary orderSummary) {
			summaryComp = orderSummary;
			foreach (var field in summaryFields) {
				summaryComp.AddField(field.OwnerSection!.Config, field.HtmlId, field.ValueObject, field.Config);
			}
		}
		internal void SetSummaryListeners(IEnumerable<string> identifiers) {
			summaryListeners = identifiers.ToList();
			foreach (var identifier in summaryListeners) {
				if (dependencyProviders.TryGetValue(identifier, out var value)) {
					summaryComp!.DependencyUpdated(identifier, value.provider.Invoke());
				}
			}
		}
		internal void RegisterSummaryField(IFieldComponent field) {
			if (field.OwnerSection == null) {
				return;
			}
			if (!summaryFields.Contains(field)) {
				summaryFields.Add(field);
				if (summaryComp != null) {
					summaryComp.AddField(field.OwnerSection.Config, field.HtmlId, field.ValueObject, field.Config);
				}
			}
		}
		internal void SummaryFieldUpdated(IFieldComponent field) {
			if (summaryFields.Contains(field)) {
				summaryComp?.FieldUpdated(field);
			}
		}
		#endregion


		#region Comments
		internal void AddComment(string author, string comment) {
			(string text, bool revised) newComment;
			if (comments.TryGetValue(author, out newComment)) {
				newComment.text += "\n";
				newComment.revised = false;
			}
			else {
				newComment = ("", false);
			}
			newComment.text += comment;
			comments[author] = newComment;
		}
		// load other order when order is open
		internal ReadOnlyDictionary<string, (string text, bool revised)> GetComments() {
			return new(comments);
		}
		internal void ToggleRevisedComment(string author) {
			if (comments.TryGetValue(author, out var comment)) {
				comment.revised = !comment.revised;
				comments[author] = comment;
			}
		}
		internal void DeleteComment(string author) {
			if (comments.ContainsKey(author)) {
				comments.Remove(author);
			}
		}
		#endregion


		#region State Controls
		internal void NewViewer(FormComponent newViewer) {
			if(formViewer != null) {
				if (formViewer != newViewer) {
					formViewer.Dispose();
				}
			}
			formViewer = newViewer;
		}

		internal void DisposingSummaryField(IFieldComponent field) {
			if (summaryFields?.Contains(field) ?? false) {
				summaryFields.Remove(field);
				summaryComp?.FieldDeleted(field.OwnerSection!.Config, field.HtmlId);
			}
		}
		internal void DisposingFormComponent(FormComponent? viewer) {
			if (formViewer == viewer) {
				dependencyProviders.Clear();
				dependencyListeners.Clear();
				conditionalListeners.Clear();
				summaryFields.Clear();
				summaryListeners?.Clear();
				summaryListeners = null;
				summaryComp = null;
				formViewer = null;
			}
		}
		internal void DiscardOrder() {
			// should not happen
			if (formViewer != null) {
				_ = 1;
				DisposingFormComponent(formViewer);
			}
			sectionConfigs = null;
			activeSections = null;
			fields = null;
			comments = null;
			removedFields = null;
			productCategories = null;
			currencyRates = null;
			plans = null;
			planFactors = null;
			regions = null;

			dependencyProviders.Clear();
			dependencyListeners.Clear();
			conditionalListeners.Clear();
			summaryFields.Clear();
			summaryListeners?.Clear();
			summaryListeners = null;

			OrderFileManager.DiscardingOrder(orderId);
		}

		internal dynamic GetSaveData() {
			//Dictionary<string, string> jsonstrings = new ();

			Dictionary<int, Dictionary<int, FieldValue>> filteredFields = new();

			foreach (var item in fields) {
				filteredFields.Add(item.Key, new(item.Value.Where(val => val.Value.HasValue())));
			}
			dynamic saveObj = new {
				ModelProps = new ModelProps(orderId, orderState, User, orderName, customerName, orderDate, orderCountry, invoiceCurrency, invoicePlan, configurationVersion),
				sectionConfigs = sectionConfigs ?? new(),
				fields = filteredFields,
				comments = orderState > ORDER_REVIEW ? new() : comments ?? new(),
				productCategories = productCategories ?? new(),
				currencyRates = currencyRates ?? new(),
				plans = plans ?? new(),
				planFactors = planFactors ?? new(),
				regions = regions ?? new(),
				activeSections = activeSections ?? new()
			};
			return saveObj;
		}
		internal static FormModel? LoadOrder(Dictionary<string, dynamic?> loadData) {
			FormModel? model = new(loadData["ModelProps"]) {
				sectionConfigs = (loadData.TryGetValue("sectionConfigs", out var configsValue) ? configsValue : null) ?? new Dictionary<int, SectionConfig>(),
				fields = (loadData.TryGetValue("fields", out var fieldsValue) ? fieldsValue : null) ?? new Dictionary<int, Dictionary<int, FieldValue>>(),
				comments = (loadData.TryGetValue("comments", out var commentsValue) ? commentsValue : null) ?? new Dictionary<string, (string, bool)>(),
				productCategories = (loadData.TryGetValue("productCategories", out var productValue) ? productValue : null) ?? new Dictionary<string, Dictionary<int, Product>>(),
				currencyRates = (loadData.TryGetValue("currencyRates", out var currencyValue) ? currencyValue : null) ?? new Dictionary<string, decimal>(),
				plans = (loadData.TryGetValue("plans", out var plansValue) ? plansValue : null) ?? new List<string>(),
				planFactors = (loadData.TryGetValue("planFactors", out var factorValue) ? factorValue : null) ?? new Dictionary<string, (decimal, int)>(),
				regions = (loadData.TryGetValue("regions", out var regionsValue) ? regionsValue : null) ?? new Dictionary<string, (string name, string nativeName, string phonePrefix)>(),
				activeSections = (loadData.TryGetValue("activeSections", out var activeValue) ? activeValue : null) ?? new List<int>(),
			};
			return model;
		}
		#endregion
	}

	internal record ModelProps(
		int orderId, 
		int orderState, 
		string? User, 
		string? orderName, 
		string? customerName, 
		DateTime? orderDate, 
		string? orderCountry, 
		string? invoiceCurrency, 
		string? invoicePlan, 
		string configurationVersion
		);

	//record ModelProps {
	//	public ModelProps(int orderId, string? user, string? orderName, string? customerName, DateTime? orderDate, string? orderCountry, string? invoiceCurrency, string? invoicePlan) {
	//		this.orderId = orderId;
	//		User = user;
	//		this.orderName = orderName;
	//		this.customerName = customerName;
	//		this.orderDate = orderDate;
	//		this.orderCountry = orderCountry;
	//		this.invoiceCurrency = invoiceCurrency;
	//		this.invoicePlan = invoicePlan;
	//	}

	//	internal int orderId { get; init; }

	//	internal string? User { get; init; }
	//	internal string? orderName { get; set; }

	//	internal string? customerName { get; set; }
	//	internal DateTime? orderDate { get; set; }
	//	internal string? orderCountry { get; set; }

	//	internal string? invoiceCurrency { get; set; }
	//	internal string? invoicePlan { get; set; }

	//}
}

// Below is early code for trying to handle form input by generic methods instead of wrapping in FieldValue.
//public void RegisterField<T, U>(T value, int sectionId, int fieldId) where T : notnull where U : struct {
//	if (!activeSections.ContainsKey(sectionId)) {
//		throw new ArgumentOutOfRangeException("Section not registered");
//	}
//	if (value is U stcVal) {
//		var dictionary = FindDictStruct<U>();
//		if (!dictionary.TryGetValue(sectionId, out var stcDict)) {

//			stcDict = new();
//			dictionary.Add(sectionId, stcDict);
//		}
//		if (!stcDict.ContainsKey(fieldId)) {
//			stcDict.Add(fieldId, default);
//		}
//	}
//	else if (value is object clsVal) {
//		var dictionary = FindDictClass(clsVal);
//		if (!dictionary.TryGetValue(sectionId, out var clsDict)) {

//			clsDict = new();
//			dictionary.Add(sectionId, clsDict);
//		}
//		if (!clsDict.ContainsKey(fieldId)) {
//			clsDict.Add(fieldId, default);
//		}
//	}
//}
//public void RegisterField<T>(int sectionId, int fieldId) {
//	Dictionary<int, Dictionary<int, T?>> dictionary = FindDict<Dictionary<int, Dictionary<int, T?>>>();

//	if (!dictionary.TryGetValue(sectionId, out var stcVal)) {
//		if (!activeSections.ContainsKey(sectionId)) {
//			throw new ArgumentOutOfRangeException("Section not registered");
//		}
//		stcVal = new();
//		dictionary.Add(sectionId, stcVal);
//	}
//	else if (!stcVal.ContainsKey(fieldId)) {
//		stcVal.Add(fieldId, default);
//	}
//}
//public T? GetValue<T, U>(T value, int sectionId, int fieldId) where T : notnull where U : struct {
//	if (value is U stcVal) {
//		var dictionary = FindDictStruct<U>();
//		if (dictionary.TryGetValue(sectionId, out var sectionDict)) {
//			if (sectionDict.TryGetValue(fieldId, out var val)) {
//				if (val is T returnVal) {
//					return returnVal;
//				}
//			}
//		}
//	}
//	return default;
//}
//public T? GetValue<T>(T stcVal, int sectionId, int fieldId) {
//	return GetValue<T>(sectionId, fieldId);
//}
//public T? GetValue<T>(int sectionId, int fieldId) {
//	Dictionary<int, Dictionary<int, T?>> dictionary = FindDict<Dictionary<int, Dictionary<int, T?>>>();

//	if (dictionary.TryGetValue(sectionId, out var sectionDict)) {
//		if (sectionDict.TryGetValue(fieldId, out var stcVal)) {
//			return stcVal;
//		}
//	}
//	throw new ArgumentOutOfRangeException("Field not registered");
//}
//public void ValueChanged<T, U>(T value, int sectionId, int fieldId) where T : notnull where U : struct {
//	//Dictionary<int, Dictionary<int, T?>> dictionary;
//	if (value is U stcVal) {
//		var dictionary = FindDictStruct<U>();
//		if (dictionary.TryGetValue(sectionId, out var sectionDict)) {
//			if (sectionDict.ContainsKey(fieldId)) {
//				sectionDict[fieldId] = stcVal;
//				return;
//			}
//		}
//	}
//	else if (value is object clsVal) {
//		var dictionary = FindDictClass(clsVal);
//		if (dictionary.TryGetValue(sectionId, out var sectionDict)) {
//			if (sectionDict.ContainsKey(fieldId)) {
//				sectionDict[fieldId] = clsVal;
//				return;
//			}
//		}
//	}
//	throw new ArgumentOutOfRangeException("Field not registered");
//	//FindDictClass<Dictionary<int, Dictionary<int, T?>>>();

//	//if (dictionary.TryGetValue(sectionId, out var sectionDict)) {
//	//	if (sectionDict.ContainsKey(fieldId)) {
//	//		sectionDict[fieldId] = stcVal;
//	//		return;
//	//	}
//	//}
//}
//public void ValueChanged<T>(T? stcVal, int sectionId, int fieldId) where T : class {
//	Dictionary<int, Dictionary<int, T?>> dictionary;
//	if (typeof(T).IsValueType) {
//		dictionary = FindDictStruct<T>();
//	}
//		//FindDictClass<Dictionary<int, Dictionary<int, T?>>>();

//	if (dictionary.TryGetValue(sectionId, out var sectionDict)) {
//		if (sectionDict.ContainsKey(fieldId)) {
//			sectionDict[fieldId] = stcVal;
//			return;
//		}
//	}
//	throw new ArgumentOutOfRangeException("Field not registered");
//}


//private Dictionary<int, Dictionary<int, T?>> FindDictClass<T>(T clsVal) where T : class {
//	Dictionary<int, Dictionary<int, T?>> dictionary = FindDictAsd<Dictionary<int, Dictionary<int, T?>>>();
//	return dictionary;
//}
//private Dictionary<int, Dictionary<int, Nullable<T>>> FindDictStruct<T>() where T : struct {
//	Dictionary<int, Dictionary<int, Nullable<T>>> dictionary = FindDictAsd<Dictionary<int, Dictionary<int, Nullable<T>>>>();
//	return dictionary;
//}
//private T FindDictAsd<T>() {

//	Type type = typeof(T);
//	Type type2 = numberFields.GetType();
//	bool same = type == type2;
//	if (numberFields is T decDict) {
//		return decDict;
//	}
//	if (textFields is T strDict) {
//		return strDict;
//	}
//	if (boolFields is T boolDict) {
//		return boolDict;
//	}
//	if (dateFields is T dateDict) {
//		return dateDict;
//	}
//	if (addressFields is T adrDict) {
//		return adrDict;
//	}
//	if (personFields is T psnDict) {
//		return psnDict;
//	}
//	if (choiceFields is T chsDict) {
//		return chsDict;
//	}
//	return default;
//}
//public void RegisterField(Type dataType, int sectionId, int fieldId) {
//	if (dataType == typeof(decimal)) {
//		if (!numberFields.TryGetValue(sectionId, out var dictionary)) {
//			dictionary = new();
//			numberFields.Add(sectionId, dictionary);
//		}
//		else if (!dictionary.ContainsKey(fieldId)) {
//			dictionary.Add(fieldId, null);
//		}
//	}
//	else if (dataType == typeof(string)) {
//		if (!textFields.TryGetValue(sectionId, out var dictionary)) {
//			dictionary = new();
//			textFields.Add(sectionId, dictionary);
//		}
//		dictionary.Add(fieldId, null);
//	}
//	else if (dataType == typeof(bool)) {
//		if (!boolFields.TryGetValue(sectionId, out var dictionary)) {
//			dictionary = new();
//			boolFields.Add(sectionId, dictionary);
//		}
//		dictionary.Add(fieldId, null);
//	}
//	else if (dataType == typeof(DateRange)) {
//		if (!dateFields.TryGetValue(sectionId, out var dictionary)) {
//			dictionary = new();
//			dateFields.Add(sectionId, dictionary);
//		}
//		dictionary.Add(fieldId, null);
//	}
//	else if (dataType == typeof(Address)) {
//		if (!addressFields.TryGetValue(sectionId, out var dictionary)) {
//			dictionary = new();
//			addressFields.Add(sectionId, dictionary);
//		}
//		dictionary.Add(fieldId, null);
//	}
//	else if (dataType == typeof(Person)) {
//		if (!personFields.TryGetValue(sectionId, out var dictionary)) {
//			dictionary = new();
//			personFields.Add(sectionId, dictionary);
//		}
//		dictionary.Add(fieldId, null);
//	}
//	else if (dataType == typeof(string[])) {
//		if (!choiceFields.TryGetValue(sectionId, out var dictionary)) {
//			dictionary = new();
//			choiceFields.Add(sectionId, dictionary);
//		}
//		dictionary.Add(fieldId, null);
//	}
//	//fields[sectionId].Add(field.Id, field);
//}

//public void ValueChanged<T>(T? oldValue, int sectionId, int fieldId) {
//	if (oldValue is decimal dVal) {
//		if (numberFields[sectionId].ContainsKey(fieldId)) {
//			numberFields[sectionId][fieldId] = dVal;
//			return;
//		}
//	}
//	else if (oldValue is string sVal) {
//		if (textFields[sectionId].ContainsKey(fieldId)) {
//			textFields[sectionId][fieldId] = sVal;
//			return;
//		}
//	}
//	else if (oldValue is bool bVal) {
//		if (boolFields[sectionId].ContainsKey(fieldId)) {
//			boolFields[sectionId][fieldId] = bVal;
//			return;
//		}
//	}
//	else if (oldValue is DateRange drVal) {
//		if (dateFields[sectionId].ContainsKey(fieldId)) {
//			dateFields[sectionId][fieldId] = drVal;
//			return;
//		}
//	}
//	else if (oldValue is Address aVal) {
//		if (addressFields[sectionId].ContainsKey(fieldId)) {
//			addressFields[sectionId][fieldId] = aVal;
//			return;
//		}
//	}
//	else if (oldValue is Person pVal) {
//		if (personFields[sectionId].ContainsKey(fieldId)) {
//			personFields[sectionId][fieldId] = pVal;
//			return;
//		}
//	}
//	else if (oldValue is string[] cVal) {
//		if (choiceFields[sectionId].ContainsKey(fieldId)) {
//			choiceFields[sectionId][fieldId] = cVal;
//			return;
//		}
//	}
//	throw new ArgumentOutOfRangeException("Field not registered");
//}

//public T? GetValue<T>(int sectionId, int fieldId) {
//	var dataType = typeof(T);
//	if (dataType == typeof(decimal)) {
//		var val = numberFields[sectionId][fieldId];
//		return val == null ? default(T) : (T)Convert.ChangeType(val, typeof(T));
//		//return (T?)Convert.ChangeType(numberFields[sectionId][fieldId], typeof(T?));
//	}
//	else if (dataType == typeof(string)) {
//		return (T?)Convert.ChangeType(textFields[sectionId][fieldId], typeof(T?));
//	}
//	else if (dataType == typeof(bool)) {
//		return (T?)Convert.ChangeType(boolFields[sectionId][fieldId], typeof(T?));
//	}
//	else if (dataType == typeof(DateRange)) {
//		return (T?)Convert.ChangeType(dateFields[sectionId][fieldId], typeof(T?));
//	}
//	else if (dataType == typeof(Address)) {
//		return (T?)Convert.ChangeType(addressFields[sectionId][fieldId], typeof(T?));
//	}
//	else if (dataType == typeof(Person)) {
//		return (T?)Convert.ChangeType(personFields[sectionId][fieldId], typeof(T?));
//	}
//	else if (dataType == typeof(string[])) {
//		return (T?)Convert.ChangeType(choiceFields[sectionId][fieldId], typeof(T?));
//	}
//	return (T?)Convert.ChangeType(null, typeof(T?));
//}
//	}
//}
