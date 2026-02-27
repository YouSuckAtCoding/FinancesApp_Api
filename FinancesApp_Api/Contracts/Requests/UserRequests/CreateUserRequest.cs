using FinancesApp_Module_User.Domain;

namespace FinancesApp_Api.Contracts.Requests.UserRequests;

public partial class UserController
{
    public record CreateUserRequest(DateTimeOffset DateOfBirth)
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string ProfileImage { get; set; } = "";

        internal User MapToUser()
        {
            return new User(Name, Email, DateOfBirth, ProfileImage);
        }
    }
}
