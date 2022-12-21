using Microsoft.AspNetCore.Components;
using OrderForm.Data;
using OrderForm.Pages;
using static OrderForm.Data.FormConfig.SectionConfig;
using static OrderForm.Data.FormConfig;
using static OrderForm.Data.FieldOptionStrings;

namespace OrderForm.Form
{
    public partial class SectionPlaceholder : IDisposable
	{
		[Parameter]
		[EditorRequired]
		public FormComponent Form { get; set; } = default!;
		[Parameter]
		[EditorRequired]
		public (int, string) SectionIdentifiers { get; set; }

		private bool disposedValue;

		protected override void OnInitialized() {
			if (SectionIdentifiers.Item1 == -1) {
				throw new ArgumentNullException($"Section must have an Id, Parameter:{nameof(SectionIdentifiers)}");
			}
		}

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {
					// TODO: dispose managed state (managed objects)
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~SectionComponent()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose() {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}


	}
}