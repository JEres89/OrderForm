using System.ComponentModel.DataAnnotations;
using static OrderForm.Data.FormConfig.SectionConfig;
//using static OrderForm.Data.FieldOptionStrings;
//using static OrderForm.Data.OrderConfigurationManager;

namespace OrderForm.Data
{
	/// <summary>
	/// When editing the default form configuration in this file, the corresponding file 
	/// will be generated when no existing 'DefaultConfiguration.json' file can be found.
	/// The values of the constants used this file is what the config parser translates 
	/// into the actual const values, so this class can't be used directly.
	/// </summary>
	public static class FormConfig
	{

		#region Generate config referencing these constants when saving a config from within the application

		private const string CUSTOMER_NAME = "@CUSTOMER_NAME@";
		private const string ORDER_DATE = "@ORDER_DATE@";
		private const string ORDER_COUNTRY = "@ORDER_COUNTRY@";
		private const string INVOICE_CURRENCY = "@INVOICE_CURRENCY@";
		private const string INVOICE_PERIOD = "@INVOICE_PERIOD@";
		private const string DEP_PROVIDER = "@DEP_PROVIDER@";
		private const string DEP_VALID = "@DEP_VALID@";
		private const string DEP_CONDITION = "@DEP_CONDITION@";
		private const string PRODUCT_GROUP = "@PRODUCT_GROUP@";
		private const string THIS_STRING = "@THIS_STRING@";
		private const string DEFAULT = "@DEFAULT@";
		private const string TRUE_STRING = "@TRUE_STRING@";
		private const string FALSE_STRING = "@FALSE_STRING@";
		private const string PLAN_DISCOUNT = "@PLAN_DISCOUNT@";

		#endregion

		public sealed class SectionConfig
		{
			public int Id { get; init; }
			public string Name { get; init; }
			public FieldConfig[] Fields { get; init; }
			public sealed class FieldConfig
			{
				public FieldTypes InputType { get; init; }
				// Internal name used to identify fields for dependencies. Is optional but must be unique within the form if assigned.
				public string? AccessName { get; init; }
				public string DisplayName { get; init; } = "";
				public string? Description { get; init; }
				public string? Placeholder { get; init; }
				// Todo: implement and parse Default field in fields where applicable
				//public string? Default { get; init; }
				public bool Required { get; init; } = false;
				public bool AsSummary { get; init; } = false;

				public Dictionary<string, string>? Constraints { get; init; }
				//public Type? CustomType { get; init; }
				//public (string, FieldTypes)[]? CustomFields { get; init; }
			}
		}

		private static SectionConfig[] Sections =
		{
			new() {
				Id = 0,
				Name = "General",
				Fields = new FieldConfig[]
				{
					new() { DisplayName = "Mandatory Information to Support",  Required = true, InputType = FieldTypes.Info
					},
					new() { AccessName  = ORDER_DATE, DisplayName = "Order Date",  Required = true, InputType = FieldTypes.Date, AsSummary = true
					},
					new() { DisplayName = "Our Representative", Placeholder = "First and Last name", Required = true, InputType = FieldTypes.Text, AsSummary = true // from current user when authed
					},
					new() { AccessName	= CUSTOMER_NAME, DisplayName = "Customer", Placeholder = "Official company name", Required = true, InputType = FieldTypes.Text, AsSummary = true
					},
					new() { DisplayName = "Existing customer?", InputType = FieldTypes.Choice, Constraints = new(new KeyValuePair<string,string>[]{new("Choices", "Yes,No")})
					},
					new() { DisplayName = "Customer contact", Placeholder = "Contact person", Required = true, InputType = FieldTypes.Person, AsSummary = true
					},

					new() { DisplayName = "Optional Information to Support", InputType = FieldTypes.Info
					},
					new() { DisplayName = "Special instructions for support", Required = false, InputType = FieldTypes.Multiline, AsSummary = true
					},

					new() { DisplayName = "Mandatory Information to Administration", Required= true, InputType = FieldTypes.Info
					},
					new() { DisplayName = "VIES number", Description = "VIES registered EU VAT/IVA/Org number (EU customers only).\nCheck the <a target=\"\" href=\"https://ec.europa.eu/taxation_customs/vies/\">VIES database</a>. Companies with domestic VAT number only, will be subject to VAT.", Placeholder = "SE999999999901", Required = true, InputType = FieldTypes.Text, AsSummary = true
					},
					new() { AccessName  = INVOICE_CURRENCY, DisplayName = "Currency", Required = true, InputType = FieldTypes.Choice, Constraints = new(new KeyValuePair<string,string>[]{new("Choices", "EXL:Currencies") }), AsSummary = true
					},
					new() { AccessName  = INVOICE_PERIOD, DisplayName = "Invoicing Period", Required = true, InputType = FieldTypes.Choice, Constraints = new(new KeyValuePair<string,string>[]{new("Choices", "EXL:Plans"), new(DEFAULT, "0") }), AsSummary = true
					},
					new() { DisplayName = "Invoice/Company Address", InputType = FieldTypes.Address, Required = true, Constraints = new(new KeyValuePair<string,string>[]{new(ORDER_COUNTRY, THIS_STRING) })
					},
					new() { DisplayName = "Invoice Email", InputType = FieldTypes.Email, Required = true, Constraints = new(new KeyValuePair<string,string>[]{ new(DEP_CONDITION, "SnailMail:"+FALSE_STRING) })
					},
					new() { AccessName = "SnailMail", DisplayName = "Mail invoice", InputType = FieldTypes.Boolean
					},
					new() { DisplayName = "Invoice reference person", Required = true, InputType = FieldTypes.Person, Constraints = new(new KeyValuePair<string,string>[]{new("RequiredFlags", "yynnynn") })
					},
					new() { DisplayName = "Optional Information to Administration", InputType = FieldTypes.Info
					},
					new() { DisplayName = "Special instructions to Administration", Required = false, InputType = FieldTypes.Multiline, AsSummary = true
					}
					//,new { DisplayName = "", Description = "", InputType = FieldTypes.Text
					//},
					//new { DisplayName = "", Description = "", InputType = FieldTypes.Text
					//}
				}
			},
			new(){
				Id = 1,
				Name = "Products",
				Fields = new FieldConfig[]
				{
					new() { AccessName = "CAT01", DisplayName = "Category 1 products", InputType = FieldTypes.Product, Required = false, Constraints = new(new KeyValuePair<string,string>[]{new("Category", "CAT01"), new(PRODUCT_GROUP,"Products") }), AsSummary = true
					},
					new() { AccessName = "CAT02", DisplayName = "Category 2 products", InputType = FieldTypes.Product, Required = false, Constraints = new(new KeyValuePair<string,string>[]{new("Category", "CAT02"), new(PRODUCT_GROUP,"Products") }), AsSummary = true
					},
					new() { AccessName = "subtotal_Products",  DisplayName = "Total Yearly Product Price", InputType = FieldTypes.Sum, Required = false, Constraints = new(new KeyValuePair<string,string>[]{ new(DEP_CONDITION, "CAT01/CAT02") }), AsSummary = false
					},
					new() { AccessName = "discount_Products",  DisplayName = "Discount", InputType = FieldTypes.Price, Required = false, Constraints = new(new KeyValuePair<string,string>[]{new(DEP_CONDITION, "CAT01/CAT02"), new(PRODUCT_GROUP, "Products"), new("Negative", TRUE_STRING)}), AsSummary = true
					},
					new() { DisplayName = "Additional Product information", InputType = FieldTypes.Info, Constraints = new(new KeyValuePair<string,string>[]{new(DEP_CONDITION, "CAT01/CAT02") })
					},
					new() { AccessName  = "end_Sub", DisplayName = "Subscription end-month", Description = "If day of start-month ≤10: previous month next year. If day of start-month >10, current month next year", Required = true, Placeholder	= "yyyy-MM", InputType = FieldTypes.Date, Constraints = new(new KeyValuePair<string,string>[]{new(DEP_CONDITION, ORDER_DATE), new("Span", "?355"), new(PRODUCT_GROUP,"Products")}),
					},
				}
			},/*
			new() {
				Id = 5,
				Name = "Test (includes one of every kind of input)",
				Fields = new FieldConfig[]
				{
					new() { DisplayName = "Number", Description = "Number Test", Placeholder = "nn", InputType = FieldTypes.Number
					},
					new() { DisplayName = "Price", Description = "Price Test", Placeholder = "nn.nn", InputType = FieldTypes.Price
					},
					new() { DisplayName = "Text", Description = "Text Test", Placeholder = "Text Test", InputType = FieldTypes.Text
					},
					new() { DisplayName = "Multiline", Description = "Multiline Test", Placeholder = "Multiline Test", InputType = FieldTypes.Multiline
					},
					new() { DisplayName = "Boolean", Description = "Boolean Test", InputType = FieldTypes.Boolean
					},
					new() { DisplayName = "Date", Description = "Date Test", Placeholder = "Year-Mn-Dy", InputType = FieldTypes.Date
					},
					new() { DisplayName = "Duration", Description = "Duration Test", InputType = FieldTypes.Duration, Constraints = new(new KeyValuePair<string,string>[]{new("MinSpan", "12345") })
					},
					new() { DisplayName = "Phone", Description = "Phone Test", Placeholder = "", InputType = FieldTypes.Phone
					},
					new() { DisplayName = "Email", Description = "Email Test", InputType = FieldTypes.Email
					},
					new() { DisplayName = "Address", Description = "Address Test", InputType = FieldTypes.Address, Constraints = new(new KeyValuePair<string,string>[]{new(ORDER_COUNTRY, FALSE_STRING) })
					},
					new() { DisplayName = "Url", Description = "Url Test", InputType = FieldTypes.Url
					},
					new() { DisplayName = "Many multichoice", Description = "Choice Test", InputType = FieldTypes.Choice, Constraints = new(new KeyValuePair<string,string>[]{new("Choices", "A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z"), new("Multiple", "5") })
					},
					new() { DisplayName = "Many singlechoice", Description = "Choice Test", InputType = FieldTypes.Choice, Constraints = new(new KeyValuePair<string,string>[]{new("Choices", "A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z") })
					},
					new() { DisplayName = "Checkbox multichoice", Description = "Choice Test", InputType = FieldTypes.Choice, Constraints = new(new KeyValuePair<string,string>[]{new("Choices", "One,Two,Three,Four"), new("Multiple", "2") })
					},
					new() { DisplayName = "Single choice", Description = "Choice Test", InputType = FieldTypes.Choice, Constraints = new(new KeyValuePair<string,string>[]{new("Choices", "One,Two,Three")})
					},
					new() { DisplayName = "Person", Description = "Person Test", InputType = FieldTypes.Person, Constraints = new(new KeyValuePair<string,string>[]{new("RequiredFlags", "yyyyyyy") })
					},
					new() { DisplayName = "Product", Description = "Product Test", InputType = FieldTypes.Product, Constraints = new(new KeyValuePair<string,string>[]{new("Category", "CAT01") })
					}
				}
			}*/

		};
		private static SectionConfig FormCreator = new() {
			Id = -1,
			Name = "FormCreator",
			Fields = new FieldConfig[]
				{
					//,new { DisplayName = "", Description = "", InputType = FieldTypes.Text
					//},
					//new { DisplayName = "", Description = "", InputType = FieldTypes.Text
					//}
				}
		};

		internal static SectionConfig[] GetSectionConfigs() {
			return Sections;
		}
	}
}
