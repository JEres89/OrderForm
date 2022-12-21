using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using OrderForm;
using OrderForm.Shared;
using OrderForm.Data;
using OrderForm.Form;
using static OrderForm.Data.FormConfig;
using static OrderForm.Data.FormConfig.SectionConfig;
using static OrderForm.Data.FieldOptionStrings;

namespace OrderForm.Form
{
	public sealed partial class OrderSummary
	{
		[Parameter]
		public FormModel Model { get; set; }
		private int summaryWidth = 30;
		private decimal yearlyOrderSum = 0;
		private decimal oneOffOrderSum = 0;
		private decimal orderPlanFactor = 1;
		private decimal orderCurrencyRate = 1;
		private string orderInvoicePlan = PLAN_YEARLY;
		private string orderCurrency = DEFAULT_CURRENCY;
		private Dictionary<string, Action<FieldValue>> listeningProviders;

		private Dictionary<SectionConfig, Dictionary<string, (FieldValue, FieldConfig)>> fieldValues = new();
		// Summary:
		//	key = HtmlId
		private Dictionary<string, (FieldValue, FieldConfig)> productValues = new();

		private Dictionary<string, ProductGroupValue> groupValues = new();

		protected override void OnInitialized() {
			base.OnInitialized();

			Model.RegisterSummaryComponent(this);
			listeningProviders = new();
			listeningProviders.Add(INVOICE_CURRENCY, SetCurrency);
			listeningProviders.Add(INVOICE_PERIOD, SetInvoicePeriod);
			Model.SetSummaryListeners(listeningProviders.Keys);
		}

		internal void DependencyUpdated(string identifier, FieldValue dependencyObject) {
			listeningProviders?[identifier].Invoke(dependencyObject);
		}

		private void SetCurrency(FieldValue value) {
			if (value.StringValue == null || value.StringValue == orderCurrency) {
				return;
			}
			orderCurrency = value.StringValue;
			var oldFactor = orderCurrencyRate;
			if (oldFactor != (orderCurrencyRate = Model.GetCurrencyRate(orderCurrency))) {
				foreach (var group in groupValues) {
					if (group.Value.SubTotals.ContainsKey(PLAN_DISCOUNT)) {
						RecountGroupSummary(group.Value);
						RecountOrderSum();
					}
				}
			}
			StateHasChanged();
		}
		private void SetInvoicePeriod(FieldValue value) {
			if (value.StringValue == null || value.StringValue == orderInvoicePlan) {
				return;
			}
			orderInvoicePlan = value.StringValue;
			orderPlanFactor = Model.GetPlanFactor(orderInvoicePlan);
			RecountAllSummary();
		}

		internal void FieldUpdated(IFieldComponent field) {
			if (productValues.TryGetValue(field.HtmlId, out var value)) {
				if (RecountGroupSummary(GetProductGroup(value.Item2)!)) {
					RecountOrderSum();
					StateHasChanged();
				}
			}
			else {
				StateHasChanged();
			}
		}

		internal void FieldDeleted(SectionConfig sectionConfig, string htmlId) {
			if (fieldValues.TryGetValue(sectionConfig, out var fields)) {
				fields.Remove(htmlId);
			}
			if (productValues.TryGetValue(htmlId, out var field)) {
				var group = GetProductGroup(field.Item2);
				group.FieldIds.Remove(htmlId);
				if (group.FieldIds.Count == 0) {
					groupValues.Remove(group.ProductGroup);
				}
				productValues.Remove(htmlId);
				if (RecountGroupSummary(group)) {
					RecountOrderSum();
				}
			}
		}

		internal void AddField(SectionConfig sectionConfig, string htmlId, FieldValue value, FieldConfig config) {
			if (config.InputType == FieldTypes.Product || (config.Constraints?.ContainsKey(PRODUCT_GROUP)??false)) {
				if (!groupValues.ContainsKey(PRODUCT_GROUP)) {
					groupValues[PRODUCT_GROUP] = new(PRODUCT_GROUP);
				}
				productValues[htmlId] = (value, config);

				var group = GetProductGroup(config, true);
				group.FieldIds.Add(htmlId);
				if (RecountGroupSummary(group)) {
					RecountOrderSum();
				}
			}
			else {
				if (config.InputType == FieldTypes.Price) {

				}
				if (!fieldValues.ContainsKey(sectionConfig)) {
					fieldValues.Add(sectionConfig, new());
				}
				fieldValues[sectionConfig][htmlId] = (value, config);
			}
		}


		private ProductGroupValue GetProductGroup(FieldConfig config, bool createIfNotExist = false) {
			if (!(config.Constraints?.TryGetValue(PRODUCT_GROUP, out var groupName) ?? false)) {
				groupName = PRODUCT_GROUP;
			}
			if (!groupValues.TryGetValue(groupName, out var group)) {
				if (createIfNotExist) {
					group = new(groupName);
					groupValues.Add(groupName, group);
				}
				else
					group = groupValues[PRODUCT_GROUP];
			}
			return group;
		}

		private void RecountAllSummary() {
			foreach (var group in groupValues.Values) {
				RecountGroupSummary(group);
			}
			RecountOrderSum();
			StateHasChanged();
		}
		private bool RecountGroupSummary(ProductGroupValue group) {
			decimal yearTotal = 0;
			decimal onceTotal = 0;
			Dictionary<string, decimal> periodSubTotals = new();
			foreach (string fieldId in group.FieldIds) {
				var fieldValue = productValues[fieldId].Item1;
				if (fieldValue.FieldType == FieldTypes.Price) {
					if (fieldValue.NumberValue == 0m) {
						continue; 
					}
					var yearlyFactor = Model.GetPrices(1, fieldValue.NumberValue, orderInvoicePlan).yearlyFactor;
					var price = fieldValue.NumberValue;
					yearTotal += price * yearlyFactor;

					string plan = price < 0 ? PLAN_DISCOUNT : PLAN_RECURRING;

					var subTotal = periodSubTotals.TryGetValue(plan, out var sub) ? sub : 0;
					subTotal += price;
					periodSubTotals[plan] = subTotal;
				}
				else {
					var selectedProducts = fieldValue.ProductValue?.SelectedProducts;
					if (selectedProducts != null && selectedProducts.Count > 0) {
						foreach (var article in fieldValue.ProductValue!.SelectedProducts) {
							//string plan = article.Value.plan! == PLAN_RECURRING ? orderInvoicePlan : article.Value.plan!;
							var (planPrice, yearlyFactor) = Model.GetPrices(article.Value.quantity, article.Value.price, article.Value.plan!);
							if (yearlyFactor == 0) {
								onceTotal += planPrice;
							}
							else {
								yearTotal += planPrice * yearlyFactor;

								var subTotal = periodSubTotals.TryGetValue(article.Value.plan ?? orderInvoicePlan, out var sub) ? sub : 0;
								subTotal += planPrice;
								periodSubTotals[article.Value.plan ?? orderInvoicePlan] = subTotal;
							}
						}
					}
				}
			}
			bool hasChanged = false;
			if(group.YearTotal != yearTotal) {
				group.YearTotal = yearTotal;
				hasChanged = true;
			}
			if (group.OnceTotal != onceTotal) {
				group.OnceTotal = onceTotal;
				hasChanged = true;
			}
			if (group.SubTotals.Count != periodSubTotals.Count || group.SubTotals.Any((s) => periodSubTotals[s.Key] != s.Value)) {
				group.SubTotals = periodSubTotals;
				hasChanged = true;
			}
			return hasChanged;
		}

		private void RecountOrderSum() {
			yearlyOrderSum = 0;
			oneOffOrderSum = 0;
			foreach (var group in groupValues.Values) {
				yearlyOrderSum += group.YearTotal;
				oneOffOrderSum += group.OnceTotal;
			}
		}

		private record ProductGroupValue
		{
			public ProductGroupValue(string productGroup) {
				ProductGroup = productGroup;
				YearTotal = 0;
				OnceTotal = 0;
				SubTotals = new();
				FieldIds = new();
				FocusIndex = -1;
			}

			public string ProductGroup;
			public decimal YearTotal;
			public decimal OnceTotal;
			public Dictionary<string, decimal> SubTotals;
			public List<string> FieldIds;
			public int FocusIndex;
			public string Focus => FocusIndex < 0 ? "" : FieldIds[FocusIndex];
			public void NextFocus() => FocusIndex = FieldIds.Count == 0 ? -1 : (FocusIndex+1 >= FieldIds.Count) ? 0 : FocusIndex+1;
		}

		private string ConvertFieldValueToString((FieldValue, FieldConfig) field) {
			var value = field.Item1;
			switch (value.FieldType) {
				case FieldTypes.Number:
					if (value.NumberValue != 0m) {
						return ((int)value.NumberValue).ToString();
					}
					break;
				case FieldTypes.Price:
					if (value.NumberValue != 0m) {
						string currency = "";
						if (string.IsNullOrEmpty(value.StringValue)) {
							if (Model.TryGetDependencyProvider("CurrencySource", out var currencyObject)) {
								currency = currencyObject.StringValue;
							}
						}
						else {
							currency = value.StringValue;
						}
						if (string.IsNullOrEmpty(currency)) {
							currency = "Currency not selected";
						}
						return value.NumberValue.ToString() + ' ' + currency;
					}
					break;
				case FieldTypes.Text:
				case FieldTypes.Multiline:
				case FieldTypes.Phone:
				case FieldTypes.Email:
				case FieldTypes.Url:
					return value.StringValue ?? string.Empty;
				case FieldTypes.Boolean:
					return value.BoolValue.ToString();
				case FieldTypes.Date:
					if (value.DateValue != null) {
						string format = field.Item2.Placeholder ?? "yyyy-MM-dd";
						return value.DateValue.Start.ToString(format);
					}
					break;
				case FieldTypes.Duration:
					if (value.DateValue != null) {
						string format = field.Item2.Placeholder ?? "yyyy-MM-dd";
						return value.DateValue.Start.ToString(format) + " - " + value.DateValue.End.ToString(format);
					}
					break;
				case FieldTypes.Address:
					if (value.AddressValue != null) {
						string address = "";
						address += value.AddressValue.Street != null ? value.AddressValue.Street + ", " : "";
						address += value.AddressValue.PostCode != null ? value.AddressValue.PostCode + " " : "";
						address += value.AddressValue.City != null ? value.AddressValue.City + ", " : "";
						address += value.AddressValue.CountryCode != null ? Model.GetRegion(value.AddressValue.CountryCode)?.name : "";
						return address;
					}
					break;
				case FieldTypes.Choice:
					if (value.ChoiceValue != null) {
						List<string> processedStrings = new();
						foreach (var choice in value.ChoiceValue) {
							if (value.ChoiceValue.Length > 4) {
								processedStrings.Add(choice.Length > (summaryWidth / 2 - 2) ? choice.Substring(0, (summaryWidth / 2 - 4)) + ".." : choice);
							}
							else {
								processedStrings.Add(choice.Length > summaryWidth - 2 ? choice.Substring(0, summaryWidth - 4) + ".." : choice);
							}
						}
						return string.Join(", ", processedStrings);
					}
					break;
				case FieldTypes.Person:
					if (value.PersonValue != null) {
						return value.PersonValue.FirstName + " " + value.PersonValue.LastName;
					}
					break;
				default:
					break;
			}

			return string.Empty;
		}
	}
}