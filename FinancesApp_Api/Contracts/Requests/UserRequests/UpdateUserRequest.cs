using FinancesApp_Module_User.Domain;

namespace FinancesApp_Api.Contracts.Requests.UserRequests;

public record UpdateUserRequest(Guid Id, DateTimeOffset DateOfBirth)
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string ProfileImage { get; set; } = "";

    internal User MapToUser()
    {
        return new User(Id, Name, Email, DateOfBirth, ProfileImage);
    }
}