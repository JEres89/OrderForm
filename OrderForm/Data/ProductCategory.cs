using NPOI.SS.Formula.Functions;

namespace OrderForm.Data
{
	public class ProductCategory
	{
		public ProductCategory() { }
		public ProductCategory(IReadOnlyDictionary<int, Product> category) {
			var categoryRow = category.First().Value;
			CategoryId = categoryRow.ArticleId;
			Name = categoryRow.Name;
			Requires = categoryRow.Requires;
			LongName = categoryRow.Description;
			Products = category.Keys.ToArray()[1..];
		}

		internal bool Restore(IReadOnlyDictionary<int, Product> category) {
			var categoryRow = category.First().Value;
			if(categoryRow.ArticleId != CategoryId) {
				return false;
			}
			foreach (var productId in SelectedProducts) {
				if (!category.ContainsKey(productId.Key)) {
					return false;
				}
			}
			Products = category.Keys.ToArray()[1..];
			Name = categoryRow.Name;
			Requires = categoryRow.Requires;
			LongName = categoryRow.Description;
			return true;
		}

		public int CategoryId { get; init; }
		internal string Name { get; private set; }
		internal string? Requires { get; private set; }
		internal string LongName { get; private set; }

		// key: ArticleId
		// Price is always in the base currency
		public Dictionary<int, (int quantity, string plan, decimal price)> SelectedProducts { get; set; } = new();

		internal int[] Products { get; private set; }
	}
}
