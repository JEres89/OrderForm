using OrderForm.Data;
using OrderForm.Pages;

namespace OrderForm.Form
{
	public class ActiveOrderTracker : IDisposable
	{
		private bool disposedValue;

		public ActiveOrderTracker() { }

		public FormModel? ActiveModel { get; private set; }
		public FormComponent? ActiveViewer { get; private set; }

		internal void RegisterOrder(FormModel newModel) {
			if (ActiveModel == newModel) {
				return;
			}
			if (ActiveModel != null) {
				DiscardOrder();
			}
			ActiveModel = newModel;
		}
		internal void RegisterViewer(FormComponent newViewer) {
			if (ActiveViewer == newViewer) {
				return;
			}
			ActiveViewer = newViewer;
		}
		internal void DisposingViewer(FormComponent oldViewer) {
			if (ActiveViewer == oldViewer) {
				ActiveViewer = null;
				//Task.Run(() => DisposedViewerTimer());
			}
		}
		//private async Task DisposedViewerTimer() {
		//	System.Threading.Thread.Sleep(10000);
		//	if (ActiveViewer == null) {
		//		DiscardOrder();
		//	}
		//}

		internal void DiscardOrder(FormModel discardModel) {
			if (ActiveModel == discardModel) {
				DiscardOrder();
			}
		}
		internal void DiscardOrder() {
			ActiveModel?.DiscardOrder();
			ActiveModel = null;
		}

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {
					ActiveViewer?.Dispose();
					ActiveViewer = null;
					ActiveModel?.DiscardOrder();
					ActiveModel = null;
				}
				disposedValue = true;
			}
		}

		public void Dispose() {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
