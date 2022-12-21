using OrderForm.Data;

namespace OrderForm.Form.Ext
{
	public interface IFieldListener
	{
		public void ConditionUpdated(string dependencyIdentifier, FieldValue dependencyObject);

	}
}
