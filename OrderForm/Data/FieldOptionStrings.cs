
namespace OrderForm.Data
{
	// ToDo: move these constants to a configuration file, or atleast a reference file, so they can be edited by someone who is not familiar with C# (i.e front-end developers).
	// ToDo: introduce constants/variables for all text/css descriptors for the same reason as above.
	internal static class FieldOptionStrings
	{
		/** 'Singleton' dependencies - a single instance per form **/

		public const string CUSTOMER_NAME = "CustomerName";

		public const string ORDER_DATE = "OrderDate";
		public const string ORDER_COUNTRY = "OrderCountry";

		public const string INVOICE_CURRENCY = "InvoiceCurrency";
		public const string INVOICE_PERIOD = "InvoicePeriod";


		/** Keywords **/

		// The configured field is the provider for the specified dependency value
		public const string DEP_PROVIDER = "Provider";
		// The configured field requires a valid value in field to allow input. Is visible and explicitly stated
		public const string DEP_VALID = "ValidField";
		// The configured field is not enabled (hidden and not included in form submition) unless the conditional field is filled. '/' separates identifiers which require one of them, ',' separates groups which are all required, ':' after an identifier specifies the value it must have to be a valid condition
		public const string DEP_CONDITION = "ConditionalField";

		public const string PRODUCT_GROUP = "ProductGroup";

		public const string THIS_STRING = "This";
		public const string DEFAULT = "default";
		public const string TRUE_STRING = "True";
		public const string FALSE_STRING = "False";

		public const string DEFAULT_CURRENCY = "SEK";
		public const string DEFAULT_COUNTRY = "sv-SE";

		public const string PLAN_YEARLY = "Yearly";
		public const string PLAN_RECURRING = "Recurring";
		public const string PLAN_ONCE = "Once";
		public const string PLAN_FREE = "Free";
		public const string PLAN_DISCOUNT = "Discount";


		/** Validation message strings **/
		public const string MSG_REQ_NULL = "Field is required and cannot be empty.";
		public const string MSG_DEP_INVALID = "Dependency {0} is not valid.";
		public const string MSG_DEP_MISSING = "Dependency {0} is not registered.";
		public const string MSG_REQ_DEP = "This field is a dependency to a required field.";


		/** Html ID modifiers **/
		public const string hid_compLinkTarget = "_atarg";
		public const string hid_compDescription = "_desc";
		public const string hid_compContent = "_content";
		public const string hid_compDepend = "_dep";
		public const string hid_prodQuantity = "_quantity";
		public const string hid_prodPrice = "_price";
		public const string hid_addrCountry = "_country";
		public const string hid_addrStreet = "_street";
		public const string hid_addrCity = "_city";
		public const string hid_addrPostCode = "_postcode";
		public const string hid_choiSingle = "_selectsingle";
		public const string hid_choiMulti = "_selectmulti";
		public const string hid_choiSelected = "_selected";
		public const string hid_choiRadio = "_radio";
		public const string hid_choiCheck = "_check";
		public const string hid_persFname = "_firstname";
		public const string hid_persLname = "_lastname";
		public const string hid_persBdate = "_birthdate";
		public const string hid_persPhone = "_phonenumber";
		public const string hid_persEmail = "_email";
		public const string hid_persWaddr = "_workaddress";
		public const string hid_persHaddr = "_homeaddress";


		/** CSS class groups **/
		public const string Css_form = "formCol form-control ";
		public const string Css_formSection = "formSection border border-secondary border-3 rounded-3 p-2 ";
		public const string Css_formRow = "formRow d-flex row ";
		public const string Css_summary = "summary border border-1 rounded-3 p-1 mb-2 ";
		public const string Css_fieldSet = "fieldSet border border-2 rounded-3 p-1 mb-2 ";

		public const string Css_dialogForm = "w-100 d-inline-flex justify-content-around ";
		public const string Css_dialogBtn = "dialog-btn form-control-sm ";


		public const string Css_req_valid = "border-success ";
		public const string Css_req_inValid = "border-danger ";
		public const string Css_opt_valid = "border-secondary ";
		public const string Css_opt_inValid = "border-warning ";
		public const string Css_dependants = "bg-warning border-1 rounded-3 ";

		public const string Css_multifield = "multifield border border-dark border-1 p-1 mb-2 ";
		public const string Css_multifieldCol = "col-sm flex-sm-shrink-1 p-0 ";

		public const string Css_fieldRow = "fieldRow row col align-items-sm-center me-0 my-1 ps-2";
		public const string Css_productRow = "productRow row col align-items-sm-center border border-1 form-control-sm mx-0 my-1 ";
		public const string Css_productRowHeader = "productRowHeader row align-items-sm-center form-control-sm mh-0 ";


		public const string Css_fieldLegend = "fieldLegend fs-6 ";
		public const string Css_fieldName = "fieldName col-sm-5 col-form-label-sm p-0 ms-1 ";
		
		public const string Css_fieldInput = "fieldInput col-sm form-control form-control-sm ";
		public const string Css_productInput = "productInput form-control form-control-sm text-end p-0 px-1";
		public const string Css_productInputHeader = "productInput productInputHeader form-control form-control-sm p-0 px-1";
		public const string Css_productInput_sm = "productInput-sm form-control form-control-sm text-center p-0 ";
		public const string Css_productInputHeader_sm = "productInput-sm productInputHeader form-control form-control-sm p-0 ";
		public const string Css_checkField = "checkField form-check-input p-0 ";
		

	}
}