using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OrderForm.Data;
using static OrderForm.Data.FieldOptionStrings;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.JSInterop;

namespace OrderForm.Form.Fields
{
	public partial class ProductField : FieldComponent<ProductCategory?>
	{
		[Inject] IJSRuntime JSRuntime { get; set; }

		public ProductField() : base() {
			ValueExpression = () => ValueObject.ProductValue;
		}

		private IReadOnlyDictionary<int, Product>? products;
		private Dictionary<int, string> productHtmlIds = new();
		private string? providedPlan;
		private decimal? planFactor;
		private string? currencySymbol;
		private decimal? currencyRate;

		private (int quantity, string plan, decimal price)? GetSelected(int articleId) {
			if (Value!.SelectedProducts.TryGetValue(articleId, out var value)) {
				return value;
			}
			return null;
		}

		//private void SetSelectedPlan(int articleId, ChangeEventArgs newPlan) {
		//	var product = products![articleId];

		//	if (newPlan.Value is string plan) {
		//		if (plan != string.Empty) {
		//			if (isPlanProvided) {
		//				SetSelected(articleId, null, defaultPlan, null);
		//			}
		//			else if (plan == noPlanString) {
		//				SetSelected(articleId, null, null, null);
		//			}
		//			else {
		//				SetSelected(articleId, null, product.Plans.Contains(plan) ? plan : defaultPlan, null);
		//			}
		//			return;
		//		}
		//	}
		//	SetSelected(articleId, null, null, null);
		//}

		private void SetSelectedQuantity(int articleId, bool fixedPrice, ChangeEventArgs newQuantity) {
			if (int.TryParse(newQuantity.Value?.ToString(), out int quantity)) {
				if (quantity > 0) {
					SetSelected(articleId, quantity, null, fixedPrice);
					return;
				}
			}
			SetSelected(articleId, null, null, fixedPrice);
			JSRuntime.InvokeVoidAsync("SetElementValue", productHtmlIds[articleId]+hid_prodQuantity, null);
			StateHasChanged();
		}

		private void SetSelectedPrice(int articleId, ChangeEventArgs newPrice) {
			if (decimal.TryParse(newPrice.Value?.ToString(), out decimal price)) {
				if (price > 0) {
					SetSelected(articleId, null, price, false);
					JSRuntime.InvokeVoidAsync("SetElementValue", productHtmlIds[articleId] + hid_prodPrice + "_text", Model.ConvertToCurrencyString(price));
					return;
				}
			}
			SetSelected(articleId, null, null, false);
			JSRuntime.InvokeVoidAsync("SetElementValue", productHtmlIds[articleId] + hid_prodPrice, null);
		}

		private void SetSelected(int articleId, int? newQuantity, decimal? newPrice, bool fixedPrice) {
			if (newQuantity == null && newPrice == null) {
				Value!.SelectedProducts.Remove(articleId);
				ReValidate();
				PropagateChange();
				return;
			}
			if (!(providedPlan != null && planFactor != null)) {
				return;
			}
			var product = products![articleId];

			// Price is always in the default currency
			newPrice = fixedPrice ? product.Price : newPrice*currencyRate;

			if (Value!.SelectedProducts.TryGetValue(articleId, out var value)) {
				value = (
					quantity: product.Single ? 1 : newQuantity ?? value.quantity,
					plan: value.plan,
					price: newPrice ?? value.price);
			}
			else {
				value = (
					quantity: product.Single ? 1 : newQuantity ?? 1,
					plan: product.Plan,
					price: newPrice ?? 0);
			}
			Value.SelectedProducts[articleId] = value;
			ReValidate();
			PropagateChange();
		}

		protected override bool CompareToValue(ProductCategory? value) {
			return Value == value;
		}

		protected override void OnInitialized() {
			base.OnInitialized();
			if (OwnerSection != null) {
				ValueChanged = EventCallback.Factory.Create<ProductCategory?>(ValueObject, (value) => ValueObject.ProductValue = value);
				CurrentValue = ValueObject.ProductValue;
			}

			if (Config.Constraints?.TryGetValue("Category", out var categoryString) ?? false) {
				if (Value != null) {
					if ((products = Model.GetProducts(categoryString)) == null) {
						throw new InvalidDataException("Category does not exist");
					}
					if (!Value.Restore(products)) {
						throw new InvalidDataException("Configuration and data missmatch");
					}
				}
				else if ((products = ExcelReader.GetProducts(null, categoryString)) == null) {
					throw new ArgumentException("Category does not exist in file.", "Category");
				}
				else {
					// try - catch? if a category is ever empty, which it shouldn't be
					Value = new(products);
				}
				foreach (var product in Value.Products) {
					productHtmlIds.Add(product, HtmlId + '_' + product);
				}
				//ToDo: move lambdas to methods for lower overhead, like protected void SetCurrency(FieldValue currency)
				RegisterDependency(INVOICE_CURRENCY, (c) => { 
					currencySymbol = c.StringValue;
					currencyRate = Model.GetCurrencyRate(currencySymbol);
				});
				RegisterDependency(INVOICE_PERIOD, (c) => {
					providedPlan = c.StringValue;
					planFactor = Model.GetPlanFactor(providedPlan);
				});
			}
			else {
				throw new ArgumentNullException("Category", "ProductField must have an assigned Constraint[\"Category\"].");
			}
		}

		public override bool ValidateFieldvalue() {
			if (Value == null || Value.SelectedProducts.Count == 0) {
				return false;
			}
			return true;
		}

		protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out ProductCategory? result, [NotNullWhen(false)] out string? validationErrorMessage) {
			throw new NotImplementedException();
		}
	}
}