using System.Diagnostics.CodeAnalysis;

namespace OrderForm.Data
{
	public struct Product
	{
		public Product(int articleId, string? requires, decimal? price, string plan, bool single, string name, string description) {
			ArticleId = articleId;
			Requires = requires;
			Price = price;
			Plan = plan;
			Single = single;
			Name = name;
			Description = description;
		}

		public int ArticleId { get; }
		public string? Requires { get; }
		/// <summary>
		///  Price is always in the base currency.
		///  When displaying as different currency divide by CurrencyRate,
		///  when storing multiply by CurrencyRate.
		/// </summary>
		public decimal? Price { get; }
		public string Plan { get; }
		public bool Single { get; }
		public string Name { get; }
		public string Description { get; }

		public override bool Equals([NotNullWhen(true)] object? obj) {
			if (obj == null)
				return false;

			if (obj is Product product) {
				return ArticleId == product.ArticleId;
			}
			return false;
		}
	}
}
