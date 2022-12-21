namespace OrderForm.Data
{
	public enum FieldTypes
	{
		/// <summary>
		/// Quantity, always positive, no decimals.
		/// </summary>
		Number = 0,

		/// <summary>
		/// Currency, currently no decimal places.
		/// </summary>
		//	Constraints:
		//			@INVOICE_CURRENCY@ = "True"/"False"/"{AccessName}" (False)
		//			"Negative" = "True"/"False" (False) - Sets the field to only hold negative values (for discount fields)
		//			PRODUCT_GROUP = "Licenses" - groups total costs of products together in summary
		Price = 1,

		/// <summary>
		/// Single line arbitrary text, anything goes.
		/// </summary>
		Text = 2,

		/// <summary>
		/// Resizable field of arbitrary text, anything goes.
		/// </summary>
		Multiline = 3,

		/// <summary>
		/// Checkbox, always has a value, unchecked = false, checked = true.
		/// </summary>
		Boolean = 4,

		/// <summary>
		/// Single date selector
		/// </summary>
		//	Constraints:
		//			"Span" = "355"/"?355" - Sets the time in days between this and the other Date field
		//					Setting this value also Requires a DEP_CONDITION constraint referencing another Date field
		//					'?' - If present it is not enforced, only added as initial value
		Date = 5,

		/// <summary>
		/// Double date selector, defining a period of time
		/// </summary>
		//	Constraints:
		//			"Span" = "30"/"?30" - Sets the time in days between the first and the second dates
		//					'?' - If present it is not enforced, only added as initial value
		Duration = 6,

		/// <summary>
		/// Phone number formatted text string, prevents any input that is not a valid phone number
		/// </summary>
		Phone = 7,

		/// <summary>
		/// Email address formatted text string, prevents any input that is not a valid email address
		/// </summary>
		Email = 8,

		/// <summary>
		/// A composite field which combines a few fields into a single address value
		/// </summary>
		//	Constraints:
		//			@ORDER_COUNTRY@ = "True"/"False"/"This" (False) - Source of address country
		//					"True" - Use the orders globally set country, i.e the customers country of operation
		//					"False" - (default) Use a specific country for this field only
		//					@THIS_STRING@ - Set this field to be the source of said global country
		//			"FullAddress" = "True"/"False" (True) - Whether field is a complete street address, or only a country
		Address = 9,

		/// <summary>
		/// Internet URL formatted text string, prevents any input that is not a valid URL
		/// </summary>
		Url = 10,

		/// <summary>
		/// An element with a number of choices
		/// </summary>
		//	Radio buttons for single selection from less than 5 choices,
		//	Checkboxes for multiple selection from less than 5 choices,
		//	Dropdown list for single or multiple selections from 5 or more choices
		//
		//	Constraints:
		//			"Choices" = "One,Two,Three"/"DB:Tbl" - Source of choices
		//					"One,Two,Three" - Choices separated by comma
		//					"SOURCE:SetName" - A collection with source and name separated by colon
		//							"EXL:*" - for tables stored in the Excel file
		//			"Multiple" = "#" (1) - Max number of choices 1+
		//			@DEFAULT@ = "1,4,5" - choice(s) selected by default
		Choice = 11,

		/// <summary>
		/// A composite field which combines up to 7 fields into one value describing a person
		/// </summary>
		// Constraints:
		//		"RequiredFlags" = "yyyyyyyy"/"nnnnnnnn" : Which personal fields are required
		//				index (position in text), (default), Value
		//				i = 0 (y) : First name
		//				i = 1 (y) : Last name
		//				i = 2 (n) : Birthdate
		//				i = 3 (y) : Phone number
		//				i = 4 (y) : Email address
		//				i = 5 (n) : Work address
		//				i = 6 (n) : Home address
		//				i =  () :
		//		"
		Person = 12,

		/// <summary>
		/// A composite field listing products in the specified category loaded from the product list
		/// </summary>
		//	Constraints:
		//			"Category" = "CAT01" - Restrict which product category to display
		//			PRODUCT_GROUP = "Licenses" - Groups total costs of products together in summary
		Product = 13,

		/// <summary>
		/// A field only for displaying information, some formatting and controlling validation for groups of fields. <br />
		///	If an info field is added to a section, all fields between that and the next Info field are included in the group. <br />
		///	Inserting an empty Info field (nothing but the <see cref="FormConfig.SectionConfig.FieldConfig.InputType">FieldType</see> value set) ends any previous group, but does not create its own.
		/// </summary>
		//	Values in config:
		//			DisplayName - A subheading for the set 
		//			Required - Sets all group fields to be required
		//			Constraints - Sets dependency or condition for entire group ( better performance than if all fields checks the same once each)
		//	Constraints:
		//			@DEP_CONDITION@ - conditions for when the group will be enabled
		//			@DEP_VALID@ - ( Not implemented )
		Info = 14,

		/// <summary>
		/// A currency sum field only for displaying the periodic subtotal of referred fields
		/// </summary>
		//	Constraints:
		//			@DEP_CONDITION@ = "CAT01/CAT02/..." - Displays the sum of the numeric values of all valid fields, AccessNames separated by slash
		Sum = 15,

		//	Constraints applicable to all fields: 
		//			@DEP_CONDITION@ = "CAT01,CAT02/DISC:-1000" - Conditions for the field to be enabled
		//					"CAT01,CAT02" - Names separated by comma, ALL needs to have valid non-empty input
		//					"CAT01/CAT02" - Names separated by slash, ANY of the fields must have a valid input
		//							Slash and comma can be combined, but slash is checked after comma,
		//							so "CAT01/CAT03,CAT02" means that "CAT02" must allways valid, and either "CAT01" and/or "CAT03" must be
		//							valid
		//					"@INVOICE_CURRENCY@:SEK" - Name and value separated by colon, field must have that specific value
		//							Colon can be added after any field name so "@INVOICE_PERIOD@:Monthly,@INVOICE_CURRENCY@:SEK"
		//							means that period has to be Monthly AND currency has to be SEK
		//							Colon is checked last so "@INVOICE_CURRENCY@:SEK/EUR" would not work, EUR would be interpreted
		//							as an AccessName.
		//			@DEP_VALID@ = "" -  

	}
}
