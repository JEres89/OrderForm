using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using OrderForm.Data;
using OrderForm.Form;
using System.Reflection.Metadata;
using static OrderForm.Data.FieldOptionStrings;

namespace OrderForm.Pages
{
	/// <summary>
	/// Handles general form state 
	/// <list type="bullet">
	/// <item>Which order is viewed</item>
	/// <item>Changing the state of the order</item>
	/// <item>Container for all components of the form</item>
	/// </list>
	/// </summary>
	/// ToDo: lock editing orders which are being viewed by another user
	public partial class FormComponent : IDisposable
	{

		//public FormComponent(string User, string? CustomerName) {
		//	this.User = User;
		//	this.CustomerName = CustomerName;
		//}

		[Inject]
		ActiveOrderTracker ActiveOrder { get; set; }
		[Inject]
		NavigationManager NavMan { get; set; }
		[Inject]
		IJSRuntime JSRuntime { get; set; }

		[Parameter]
		public string OrderIdString {
			get => orderId.ToString();
			set => int.TryParse(value, out orderId);
		}

		#region Operational variables (Required to operate)
		/// <summary>
		/// Id of current page. Never set directly, only set by navigating to /orderform/{id}.
		/// They are assumed to match at all times.
		/// </summary>
		private int orderId;
		protected FormModel? model;
		private EditContext? orderContext;

		private Dictionary<int, string> sectionChoices;
		/// <summary>
		/// At this time these are the fields used to generate the order name.
		/// Any other identifiers (<see cref="FieldConfig.AccessName"/>) of fields used to display information in this component can be added.
		/// </summary>
		/// ToDo: register these in FormModel instead of checking every <see cref="OnFieldChanged(object?, FieldChangedEventArgs)"/> trigger
		/// maybe ToDo: generalize and move to configuration
		private static List<string> listeningForFields = new() {
				CUSTOMER_NAME,
				ORDER_DATE
			};
		#endregion

		#region Order state variables
		/// <summary>
		/// The sections which are used in this order, fetched from FormModel when loading form and/or added to when a section is activated by the user (General section, or section with id 0, is always added)
		/// </summary>
		private List<int> activeSections = new();
		private Dictionary<int, SectionComponent> sections = new();
		#endregion

		#region Component state variables
		/// <summary>
		/// Is the form initialized and ready to be rendered?
		/// </summary>
		private bool formReady = false;
		/// <summary>
		/// Have any input changed since loading order?
		/// </summary>
		private bool hasChanges = false;
		/// <summary>
		/// Did the user try to open another order 
		/// </summary>
		private bool promptCloseOrder = false;
		/// <summary>
		/// Did the user try to open an order already used by someone else? 
		/// </summary>
		private bool promptOrderBusy = false;
		/// <summary>
		/// Verification failed and the verification button is disabled until an input changes
		/// </summary>
		private bool disableStateButton = false;
		private string? validationError;
		private string? commentAuthor;
		private string? newComment;
		#endregion

		private bool disposedValue;

		#region Blazor operational methods
		/// <summary>
		/// New OrderForm page loads start here (by refreshing page in browser or navigating from different location)
		/// This establishes whether it is a new or existing session.
		/// If there is a loaded order in the session this always opens and initializes that first and then prompts
		/// the user what to do.
		/// If orderId is 0, a new unused Id is requested, and a redirection is made to the URL with that Id.
		/// Else the form is initialized with the provided Id, load if it exists or create a new one.
		/// </summary>
		protected override void OnInitialized() {
			ActiveOrder.RegisterViewer(this);
			bool shouldInit = false;
			if (ActiveOrder.ActiveModel != null) {
				model = ActiveOrder.ActiveModel;
				shouldInit = true;
			}
			else if (orderId == 0) {
				NavMan.NavigateTo("/orderform/" + OrderFileManager.GetNextId(), false);
				//NewOrder();
			}
			else {
				shouldInit = true;
			}

			if (shouldInit) {
				InitForm();
			}
		}
		/// <summary>
		/// 
		/// </summary>
		protected override void OnParametersSet() {
			if (formReady) {
				int activeId = model?.orderId ?? ActiveOrder.ActiveModel!.orderId;
				if (orderId != activeId) {
					promptCloseOrder = true;
				}
			}
			else if (orderId != 0) {
				InitForm();
			}
			else {
				Console.WriteLine("Redirecting to new ID");
			}
			base.OnParametersSet();
		}

		protected override void OnAfterRender(bool firstRender) {
			base.OnAfterRender(firstRender);
			if (promptCloseOrder) {
				promptCloseOrder = false;
				JSRuntime.InvokeVoidAsync("ShowDialog", "discardFormConfirm");
			}
			else if (promptOrderBusy) {
				promptOrderBusy = false;
				JSRuntime.InvokeVoidAsync("ShowDialog", "orderBusy");
			}
		}

		private void OnFieldChanged(object? sender, FieldChangedEventArgs e) {
			var fieldIdentifier = e.FieldIdentifier;
			hasChanges = true;
			if (disableStateButton) {
				disableStateButton = false;
				validationError = null;
				StateHasChanged();
			}
			if (fieldIdentifier.Model is FieldValue field) {
				if (listeningForFields.Contains(field.FieldIdentifier!)) {
					StateHasChanged();
				}

			}
			//else {
			//	//Unknown field type, other validation required
			//}
		}
		//protected override bool ShouldRender() {
		//	return base.ShouldRender();
		//}
		#endregion

		#region Order management methods 
		private void InitForm() {
			if (model == null) {
				if (OrderFileManager.OrderExists(orderId)) {
					var loadData = OrderFileManager.LoadFromFile(orderId);
					if (loadData != null) {
						model = FormModel.LoadOrder(loadData)!;
					}
					else {
						promptOrderBusy = true;
						return;
					}
				}
			}

			if (model == null) {
				CreateOrder();
			}
			else {
				OpenOrder();
			}
			ActiveOrder.RegisterOrder(model!);
			formReady = true;
		}
		private void CreateOrder() {
			model = new(orderId, this);
			sectionChoices = model.GetSectionIdentifiers();

			if (sectionChoices.ContainsKey(0)) {
				activeSections.Add(0);
			}
			else {
				throw new Exception("The form configuration does not contain a general section.");
			}
			if (orderContext == null) {
				orderContext = new(model);
				orderContext.OnFieldChanged += OnFieldChanged;
			}
			hasChanges = true;
		}
		private void OpenOrder() {
			model!.NewViewer(this);
			sectionChoices = model!.GetSectionIdentifiers();

			foreach (var entry in sectionChoices) {
				if (model.IsSectionActive(entry.Key)) {
					activeSections.Add(entry.Key);
				}
			}
			if (orderContext == null) {
				orderContext = new(model);
				orderContext.OnFieldChanged += OnFieldChanged;
			}
			hasChanges = false;
		}

		private void SaveOrder() {
			var orderName = model!.OrderName;
			model.OrderName = orderName;
			var orderJsons = model.GetSaveData();
			OrderFileManager.SaveToFile(orderJsons);
			hasChanges = false;
		}
		private void NewOrder() {
			Dispose(false);
			ActiveOrder.DiscardOrder();
			hasChanges = false;
			formReady = false;
			if (orderId == 0) {
				NavMan.NavigateTo("/orderform/" + OrderFileManager.GetNextId(), false);
			}
			else {
				InitForm();
			}
		}
		/// <summary>
		/// Because the SectionComponents are cached in Blazor, even if they are disposed, we need a new session if 
		/// we want to initialize them with new data. 
		/// Alternatively implement a reinit in the OnParametersSet() method of sections, and possibly fields.
		/// </summary>
		private void GoToOrder(int id) {
			Dispose(false);
			ActiveOrder.DiscardOrder();
			if (id==0) {
				id = OrderFileManager.GetNextId();
			}
			NavMan.NavigateTo("/orderform/" + id, true);
		}

		private const int DIA_Open_Discard = 0, DIA_Open_Reopen = 1, DIA_Open_Save = 2;
		private const int DIA_Busy_New = 3, DIA_Busy_Load = 4;
		private void DialogClose(int action) {
			switch (action) {
				case DIA_Open_Discard:
					GoToOrder(orderId);
					break;
				case DIA_Open_Reopen:
					model ??= ActiveOrder.ActiveModel;
					NavMan.NavigateTo("/orderform/" + model?.orderId, false);
					break;
				case DIA_Open_Save:
					SaveOrder();
					GoToOrder(orderId);
					break;
				case DIA_Busy_New:
					orderId = 0;
					GoToOrder(orderId);
					break;
				case DIA_Busy_Load:
					NavMan.NavigateTo("/loadorder", false);
					break;
				default:
					break;
			}
		}
		#endregion

		#region Order state methods
		internal void RegisterSection(SectionComponent section) {
			//AddFormSection(section.SectionId);
			int newSectionId = section.SectionId;
			if (!activeSections.Contains(newSectionId)) {
				activeSections.Add(newSectionId);
			}
			if (sections.ContainsKey(newSectionId)) {
				SectionComponent oldSection = sections[newSectionId];
				if (oldSection != null) {
					if (oldSection != section) {
						oldSection.Dispose();
					}
					else
						return;
				}
				sections[newSectionId] = section;
			}
			else {
				sections.Add(newSectionId, section);
			}
		}
		//private void AddFormSection(int sectionId) {
		//	if (!activeSections.Contains(sectionId)) {
		//		activeSections.Add(sectionId);
		//	}
		//	//StateHasChanged();
		//}
		private bool VerifyOrder() {
			bool isValid = true;
			//System.Threading.Thread.Sleep(5000);
			foreach (var section in activeSections) {
				
				isValid &= sections[section].IsValid;
			}
			return isValid;
		}
		#endregion

		#region Order attesting methods
		// ToDo: Combine attesting state change methods
		private void SendToReview() {
			JSRuntime.InvokeVoidAsync("ToggleDisplay", "stateSpinner1", 0);

			if (orderContext?.Validate() ?? false)
			{
				model!.orderState = FormModel.ORDER_REVIEW;
				SaveOrder();
				GoToOrder(orderId);
			}
			//if (VerifyOrder()) {
			//	model!.orderState = FormModel.ORDER_REVIEW;
			//	SaveOrder();
			//	GoToOrder(orderId);
			//}
			else {
				disableStateButton = true;
				// show message if order is invalid
				validationError = "Order is not filled out correctly";
			}
			JSRuntime.InvokeVoidAsync("ToggleDisplay", "stateSpinner1", 0);
		}
		private void ReturnForRevision() {
			JSRuntime.InvokeVoidAsync("ToggleDisplay", "stateSpinner1", 0);
			disableStateButton = true;
			model!.orderState = FormModel.ORDER_IN_PROGRESS;
			SaveOrder();
			GoToOrder(orderId);
			JSRuntime.InvokeVoidAsync("ToggleDisplay", "stateSpinner1", 0);
		}
		private void ApproveOrder() {
			JSRuntime.InvokeVoidAsync("ToggleDisplay", "stateSpinner2", 0);
			disableStateButton = true;
			model!.orderState = FormModel.ORDER_APPROVED;
			SaveOrder();
			GoToOrder(orderId);
			JSRuntime.InvokeVoidAsync("ToggleDisplay", "stateSpinner2", 0);
		}
		private void RetractOrder() {
			JSRuntime.InvokeVoidAsync("ToggleDisplay", "stateSpinner1", 0);
			disableStateButton = true;
			model!.orderState = FormModel.ORDER_REVIEW;
			SaveOrder();
			GoToOrder(orderId);
			JSRuntime.InvokeVoidAsync("ToggleDisplay", "stateSpinner1", 0);
		}
		private void Archive() {
			JSRuntime.InvokeVoidAsync("ToggleDisplay", "stateSpinner2", 0);
			disableStateButton = true;
			model!.orderState = FormModel.ORDER_ACTIONED;
			SaveOrder();
			GoToOrder(orderId);
			JSRuntime.InvokeVoidAsync("ToggleDisplay", "stateSpinner2", 0);
		}

		private void AddComment() {
			if (commentAuthor != null && newComment != null) {
				model!.AddComment(commentAuthor, newComment);
				commentAuthor = null;
				newComment = null;
				JSRuntime.InvokeVoidAsync("ToggleDisplay", "newComment");
			}
		}
		private void ToggleRevisedComment(string key) {
			model!.ToggleRevisedComment(key);
		}
		private void RemarkComment(string key) {
			if (newComment != null) {
				model!.AddComment(key, $"<br><i>{newComment}</i>");
				newComment= null;
				JSRuntime.InvokeVoidAsync("ToggleChildrenOfId", $"comment_{key}_remark");
			}
		}
		private void DeleteComment(string key) {
			model!.DeleteComment(key);
		}
		#endregion

		#region IDisposable
		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				// TODO: dispose managed state (managed objects)
				formReady = false;
				foreach (var section in sections.Values) {
					section.Dispose();
				}
				sections.Clear();
				activeSections.Clear();
				sectionChoices = default!;
				model?.DisposingFormComponent(this);
				model = null;

				if (disposing) {
					if(orderContext != null) {
						orderContext.OnFieldChanged -= OnFieldChanged;
					}
					orderContext = null;
					ActiveOrder.DisposingViewer(this);
					ActiveOrder = null!;
					// TODO: free unmanaged resources (unmanaged objects) and override finalizer
					// TODO: set large fields to null
					disposedValue = true;
				}

			}
		}
		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~FormComponent()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }
		public void Dispose() {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}