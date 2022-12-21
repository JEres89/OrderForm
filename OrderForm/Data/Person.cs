using BlazorDateRangePicker;

namespace OrderForm.Data
{
    public class Person
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateRange? BirthDate { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public Address? WorkAddress { get; set; }
        public Address? HomeAddress { get; set; }
    }
}
