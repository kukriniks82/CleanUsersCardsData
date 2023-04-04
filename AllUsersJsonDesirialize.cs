
using System.Text.Json.Serialization;

namespace CleanUsersCardsData
{
    public class User
    {
        public string id { get; set; }
        public int ID { get; set; }
        public string Hash { get; set; }
        public bool IsSystemUser { get; set; }
        public bool IsLoginDisabled { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string FullName { get; set; }
        public string Organization { get; set; }
        public string Department { get; set; }
        public string JobPosition { get; set; }
        public string Address { get; set; }
        public string FiredDate { get; set; }
        public string Comments { get; set; }
        public string PhotoRevision { get; set; }
        public string Photo { get; set; }
        public string IsPhotoGet { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool IsChangePasswordNeeded { get; set; }
        public string PasswordSetDate { get; set; }
        public Value Phones { get; set; }
        public Value EMails { get; set; }
        public Value ICQUINs { get; set; }
        public Value SkypeAccounts { get; set; }
        public Value SIDs { get; set; }
        public Value YahooAccounts { get; set; }
        public Value LocalUserDNs { get; set; }
        public Value SocialNetworkIDs { get; set; }
        public Value TelegramAccounts { get; set; }
        public Value IPAddressLeases { get; set; }
    }
    public class Value
    {
        public string id { get; set; }
        public string[] values { get; set; }
    }
    public class UserArray
    {
        public string id { get; set; }
        public User[] values { get; set; }
    }
    public class SinglUser //для десериализации пользователя полученного по ID
    {
        [JsonPropertyName("$type")]
        public string type { get; set; }
        public int ID { get; set; }
        public string Hash { get; set; }
        public bool IsSystemUser { get; set; }
        public bool IsLoginDisabled { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string FullName { get; set; }
        public string Organization { get; set; }
        public string Department { get; set; }
        public string JobPosition { get; set; }
        public string Address { get; set; }
        public string HiredDate { get; set; }
        public string FiredDate { get; set; }
        public string Comments { get; set; }
        public string PhotoRevision { get; set; }
        public string Photo { get; set; }
        public bool IsPhotoGet { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool IsChangePasswordNeeded { get; set; }
        public string PasswordSetDate { get; set; }
        public string[] Phones { get; set; }
        public string[] EMails { get; set; }
        public string[] ICQUINs { get; set; }
        public string[] SkypeAccounts { get; set; }
        public string[] SIDs { get; set; }
        public string[] YahooAccounts { get; set; }
        public string[] LocalUserDNs { get; set; }
        public string[] SocialNetworkIDs { get; set; }
        public string[] TelegramAccounts { get; set; }
        public string[] IPAddressLeases { get; set; }
    }
}
