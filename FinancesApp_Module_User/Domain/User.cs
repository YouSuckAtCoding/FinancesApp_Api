namespace FinancesApp_Module_User.Domain;
public class User
{
    private int age;

    public Guid Id { get; }
    public string Name { get; private set; } = "";
    public string Email { get; private set; } = "";
    public DateTimeOffset RegisteredAt { get; private set; }
    public DateTimeOffset ModifiedAt { get; private set; }
    public DateTimeOffset DateOfBirth { get; private set; }
    public string ProfileImage { get; private set; } = "";
    public int Age
    {
        get
        {
            var today = DateTimeOffset.UtcNow;
            var age = today.Year - DateOfBirth.Year;

            if (today.Month < DateOfBirth.Month ||
                (today.Month == DateOfBirth.Month && today.Day < DateOfBirth.Day))
            {
                age--;
            }

            return age;
        }
        private set => age = value;

    }

    public User(Guid id, 
                string name,
                string email,
                DateTimeOffset registeredAt,
                DateTimeOffset modifiedAt,
                DateTimeOffset dateOfBirth, 
                string profileImage)
    {
        Id = id;
        Name = name;
        Email = email;
        RegisteredAt = registeredAt;
        ModifiedAt = modifiedAt;
        DateOfBirth = dateOfBirth;
        ProfileImage = profileImage;
    }

    public User(string name, 
                string email, 
                DateTimeOffset dateOfBirth, 
                string profileImage)
    {
        Id = Guid.NewGuid();
        Name = ValidateName(name);
        Email = ValidateEmail(email);
        DateOfBirth = ValidateDateOfBirth(dateOfBirth);
        ValidateAge(Age);
        ProfileImage = profileImage;
        RegisteredAt = DateTimeOffset.Now;
        ModifiedAt = DateTimeOffset.Now;
    }

    public User(Guid id, 
                string name,
                string email,
                DateTimeOffset dateOfBirth,
                string profileImage)
    {
        Id = id;
        Name = name;
        Email = email;
        DateOfBirth = dateOfBirth;
        ProfileImage = profileImage;
        ModifiedAt = DateTimeOffset.Now;
    }

    public User()
    {}

    private static string ValidateName(string name)
    {
        if(string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.");
        if (name.Length > 100)
            throw new ArgumentException("Name cannot be longer than 100 characters.");

        return name;
    }

    private static string ValidateEmail(string email)
    {
        if(string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty.");
        if(!email.Contains("@"))
            throw new ArgumentException("Email must contain '@' symbol.");
        if(email.Length > 50)
            throw new ArgumentException("Email cannot be longer than 50 characters.");
        return email;
    }

    private static DateTimeOffset ValidateDateOfBirth(DateTimeOffset dateOfBirth)
    {
        var today = DateTimeOffset.UtcNow;

        if (dateOfBirth > today)
            throw new ArgumentException("Date of birth cannot be in the future.");       

        return dateOfBirth;
    }
    private void ValidateAge(int age)
    {
        if (Age > 120)
            throw new ArgumentException("You're not that old buddy.");
        if (Age < 16)
            throw new ArgumentException("You're too young buddy.");
        return;
    }    
}
