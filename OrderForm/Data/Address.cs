using System.Globalization;
using System.Linq;
using System.Text;

namespace OrderForm.Data
{
    /// <summary>
    /// DO NOT USE AS KEYS IN HASHTABLES
    /// </summary>
    public class Address
    {
        public string? CountryCode { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? PostCode { get; set; }

        public static Address? Copy(Address? source)
        {
            if (source == null) return null;
            return new Address { CountryCode = source.CountryCode, Street = source.Street, City = source.City, PostCode = source.PostCode };
        }

		public string ToString(string? countryName) {
      StringBuilder addrString = new();
      addrString.Append(Street?.Append('\n'));
      addrString.Append(
        City?.Concat(
          (PostCode?.ToCharArray().Prepend(' ').Prepend(',').Append('\n'))
          ??
          "\n")
        ??
        PostCode?.Append(
          '\n'
          )
        );
			addrString.Append(countryName);

			return addrString.ToString();
		}

		public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (base.Equals(obj)) return true;

            if (obj is Address other)
            {
                if (CountryCode != other.CountryCode) return false;
                if (Street != other.Street) return false;
                if (City != other.City) return false;
                if (PostCode != other.PostCode) return false;
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Tuple.Create(CountryCode, Street, City, PostCode).GetHashCode();
            //return CountryCode?.GetHashCode() ?? 0 ^ Street?.GetHashCode() ?? 0 ^ City?.GetHashCode() ?? 0 ^ PostCode?.GetHashCode() ?? 0;

        }
    }
}
