using FinancesApp_Module_User.Domain;
using FluentAssertions;

namespace FinancesApp_Tests.UserTests.Domain;

public class UserDomainTests
{
    #region Constructor Tests - Valid Cases

    [Fact]
    public void Constructor_Should_Create_User_With_Valid_Data()
    {
        var name = "John Doe";
        var email = "john.doe@example.com";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-25);
        var profileImage = "https://example.com/profile.jpg";

        var user = new User(name, email, dateOfBirth, profileImage);

        user.Name.Should().Be(name);
        user.Email.Should().Be(email);
        user.DateOfBirth.Should().Be(dateOfBirth);
        user.ProfileImage.Should().Be(profileImage);
        user.Age.Should().Be(25);
    }

    [Fact]
    public void Constructor_Should_Create_User_With_Empty_ProfileImage()
    {
        var name = "Jane Smith";
        var email = "jane@example.com";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-30);
        var profileImage = "";

        var user = new User(name, email, dateOfBirth, profileImage);

        user.ProfileImage.Should().BeEmpty();
        user.Age.Should().Be(30);
    }

    [Fact]
    public void Constructor_Should_Create_User_With_Minimum_Age_16()
    {
        var name = "Young User";
        var email = "young@example.com";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-16).AddDays(-1);
        var profileImage = "";

        var user = new User(name, email, dateOfBirth, profileImage);

        user.DateOfBirth.Should().Be(dateOfBirth);
        user.Age.Should().Be(16);
    }

    [Fact]
    public void Constructor_Should_Create_User_With_Maximum_Age_120()
    {
        var name = "Old User";
        var email = "old@example.com";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-120);
        var profileImage = "";

        var user = new User(name, email, dateOfBirth, profileImage);

        user.DateOfBirth.Should().Be(dateOfBirth);
        user.Age.Should().Be(120);
    }

    [Fact]
    public void ReconstructionConstructor_Should_Create_User_With_All_Fields()
    {
        var id = Guid.NewGuid();
        var name = "John Doe";
        var email = "john@example.com";
        var registeredAt = DateTimeOffset.UtcNow.AddMonths(-6);
        var modifiedAt = DateTimeOffset.UtcNow.AddDays(-1);
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-30);
        var profileImage = "https://example.com/profile.jpg";

        var user = new User(id, name, email, registeredAt, modifiedAt, dateOfBirth, profileImage);

        user.Id.Should().Be(id);
        user.Name.Should().Be(name);
        user.Email.Should().Be(email);
        user.RegisteredAt.Should().Be(registeredAt);
        user.ModifiedAt.Should().Be(modifiedAt);
        user.DateOfBirth.Should().Be(dateOfBirth);
        user.ProfileImage.Should().Be(profileImage);
        user.Age.Should().Be(30);
    }

    [Fact]
    public void ReconstructionConstructor_Should_Not_Validate_Age()
    {
        var id = Guid.NewGuid();
        var name = "Young Kid";
        var email = "kid@example.com";
        var registeredAt = DateTimeOffset.UtcNow;
        var modifiedAt = DateTimeOffset.UtcNow;
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-10);
        var profileImage = "";

        var user = new User(id, name, email, registeredAt, modifiedAt, dateOfBirth, profileImage);

        user.DateOfBirth.Should().Be(dateOfBirth);
        user.Age.Should().Be(10);
    }

    [Fact]
    public void EmptyConstructor_Should_Create_User_With_Default_Values()
    {
        var user = new User();

        user.Id.Should().Be(Guid.Empty);
        user.Name.Should().BeEmpty();
        user.Email.Should().BeEmpty();
        user.ProfileImage.Should().BeEmpty();
        user.RegisteredAt.Should().Be(default);
        user.ModifiedAt.Should().Be(default);
        user.DateOfBirth.Should().Be(default);
    }

    #endregion

    #region Name Validation Tests

    [Fact]
    public void Constructor_Should_Throw_When_Name_Is_Null()
    {
        string name = null!;
        var email = "test@example.com";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-25);
        var profileImage = "";

        var act = () => new User(name, email, dateOfBirth, profileImage);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be empty.");
    }

    [Fact]
    public void Constructor_Should_Throw_When_Name_Is_Empty()
    {
        var name = "";
        var email = "test@example.com";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-25);
        var profileImage = "";

        var act = () => new User(name, email, dateOfBirth, profileImage);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be empty.");
    }

    [Fact]
    public void Constructor_Should_Throw_When_Name_Is_Whitespace()
    {
        var name = "   ";
        var email = "test@example.com";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-25);
        var profileImage = "";

        var act = () => new User(name, email, dateOfBirth, profileImage);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be empty.");
    }

    #endregion

    #region Email Validation Tests

    [Fact]
    public void Constructor_Should_Throw_When_Email_Is_Null()
    {
        var name = "John Doe";
        string email = null!;
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-25);
        var profileImage = "";

        var act = () => new User(name, email, dateOfBirth, profileImage);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Email cannot be empty.");
    }

    [Fact]
    public void Constructor_Should_Throw_When_Email_Is_Empty()
    {
        var name = "John Doe";
        var email = "";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-25);
        var profileImage = "";

        var act = () => new User(name, email, dateOfBirth, profileImage);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Email cannot be empty.");
    }

    [Fact]
    public void Constructor_Should_Throw_When_Email_Is_Whitespace()
    {
        var name = "John Doe";
        var email = "   ";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-25);
        var profileImage = "";

        var act = () => new User(name, email, dateOfBirth, profileImage);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Email cannot be empty.");
    }

    [Fact]
    public void Constructor_Should_Throw_When_Email_Does_Not_Contain_At_Symbol()
    {
        var name = "John Doe";
        var email = "invalidemail.com";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-25);
        var profileImage = "";

        var act = () => new User(name, email, dateOfBirth, profileImage);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Email must contain '@' symbol.");
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user@domain.co.uk")]
    [InlineData("name+tag@company.org")]
    [InlineData("simple@test.io")]
    public void Constructor_Should_Accept_Valid_Email_Formats(string email)
    {
        var name = "John Doe";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-25);
        var profileImage = "";

        var user = new User(name, email, dateOfBirth, profileImage);

        user.Email.Should().Be(email);
    }

    #endregion

    #region DateOfBirth Validation Tests

    [Fact]
    public void Constructor_Should_Throw_When_DateOfBirth_Is_In_Future()
    {
        var name = "John Doe";
        var email = "john@example.com";
        var dateOfBirth = DateTimeOffset.UtcNow.AddDays(1);
        var profileImage = "";

        var act = () => new User(name, email, dateOfBirth, profileImage);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Date of birth cannot be in the future.");
    }

    [Fact]
    public void Constructor_Should_Accept_DateOfBirth_Today()
    {
        var name = "Newborn";
        var email = "newborn@example.com";
        var dateOfBirth = DateTimeOffset.UtcNow;
        var profileImage = "";

        var act = () => new User(name, email, dateOfBirth, profileImage);

        act.Should().Throw<ArgumentException>()
            .WithMessage("You're too young buddy.");
    }

    #endregion

    #region Age Validation Tests

    [Fact]
    public void Constructor_Should_Throw_When_User_Is_Too_Young()
    {
        var name = "Young Kid";
        var email = "kid@example.com";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-15);
        var profileImage = "";

        var act = () => new User(name, email, dateOfBirth, profileImage);

        act.Should().Throw<ArgumentException>()
            .WithMessage("You're too young buddy.");
    }

    [Fact]
    public void Constructor_Should_Throw_When_User_Is_Exactly_15_Years_Old()
    {
        var name = "Teen User";
        var email = "teen@example.com";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-15).AddDays(-364);
        var profileImage = "";

        var act = () => new User(name, email, dateOfBirth, profileImage);

        act.Should().Throw<ArgumentException>()
            .WithMessage("You're too young buddy.");
    }

    [Fact]
    public void Constructor_Should_Throw_When_User_Is_Too_Old()
    {
        var name = "Ancient User";
        var email = "ancient@example.com";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-121);
        var profileImage = "";

        var act = () => new User(name, email, dateOfBirth, profileImage);

        act.Should().Throw<ArgumentException>()
            .WithMessage("You're not that old buddy.");
    }

    [Fact]
    public void Constructor_Should_Accept_User_Exactly_16_Years_Old()
    {
        var name = "Young Adult";
        var email = "youngadult@example.com";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-16);
        var profileImage = "";

        var user = new User(name, email, dateOfBirth, profileImage);

        user.Age.Should().Be(16);
    }

    [Theory]
    [InlineData(-16)]
    [InlineData(-18)]
    [InlineData(-25)]
    [InlineData(-40)]
    [InlineData(-65)]
    [InlineData(-100)]
    [InlineData(-120)]
    public void Constructor_Should_Accept_Valid_Ages(int yearsAgo)
    {
        var name = "Valid User";
        var email = "valid@example.com";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(yearsAgo);
        var profileImage = "";

        var user = new User(name, email, dateOfBirth, profileImage);

        user.Age.Should().BeInRange(16, 120);
    }

    #endregion

    #region Age Property Tests

    [Fact]
    public void Age_Should_Calculate_Correctly_When_Birthday_Has_Passed_This_Year()
    {
        var today = DateTimeOffset.UtcNow;
        var dateOfBirth = new DateTimeOffset(
            today.Year - 25,
            today.Month > 1 ? today.Month - 1 : 12,
            today.Day,
            0, 0, 0,
            TimeSpan.Zero);

        var user = new User("John Doe", "john@example.com", dateOfBirth, "");

        var age = user.Age;

        age.Should().Be(25);
    }

    [Fact]
    public void Age_Should_Calculate_Correctly_When_Birthday_Has_Not_Passed_This_Year()
    {
        var today = DateTimeOffset.UtcNow;
        var dateOfBirth = new DateTimeOffset(
            today.Year - 25,
            today.Month < 12 ? today.Month + 1 : 1,
            today.Day,
            0, 0, 0,
            TimeSpan.Zero);

        var user = new User("John Doe", "john@example.com", dateOfBirth, "");

        var age = user.Age;

        age.Should().Be(24);
    }

    [Fact]
    public void Age_Should_Calculate_Correctly_On_Birthday()
    {
        var today = DateTimeOffset.UtcNow;
        var dateOfBirth = new DateTimeOffset(
            today.Year - 30,
            today.Month,
            today.Day,
            0, 0, 0,
            TimeSpan.Zero);

        var user = new User("John Doe", "john@example.com", dateOfBirth, "");

        var age = user.Age;

        age.Should().Be(30);
    }

    [Fact]
    public void Age_Should_Calculate_Correctly_Day_Before_Birthday()
    {
        var today = DateTimeOffset.UtcNow;
        var dateOfBirth = new DateTimeOffset(
            today.Year - 30,
            today.Month,
            today.Day < 28 ? today.Day + 1 : 1,
            0, 0, 0,
            TimeSpan.Zero);

        var user = new User("John Doe", "john@example.com", dateOfBirth, "");

        var age = user.Age;

        age.Should().Be(29);
    }

    [Fact]
    public void Age_Should_Calculate_Correctly_Day_After_Birthday()
    {
        var today = DateTimeOffset.UtcNow;
        var dateOfBirth = new DateTimeOffset(
            today.Year - 30,
            today.Month,
            today.Day > 1 ? today.Day - 1 : 28,
            0, 0, 0,
            TimeSpan.Zero);

        var user = new User("John Doe", "john@example.com", dateOfBirth, "");

        var age = user.Age;

        age.Should().Be(30);
    }

    [Fact]
    public void Age_Should_Handle_Leap_Year_Birthday()
    {
        var dateOfBirth = new DateTimeOffset(2000, 2, 29, 0, 0, 0, TimeSpan.Zero);
        var user = new User("Leap Year Baby", "leap@example.com", dateOfBirth, "");

        var age = user.Age;

        var today = DateTimeOffset.UtcNow;
        var expectedAge = today.Year - 2000;

        if (today.Month < 2 || (today.Month == 2 && today.Day < 29))
        {
            expectedAge--;
        }

        age.Should().Be(expectedAge);
    }

    [Theory]
    [InlineData(1, 15)]
    [InlineData(2, 28)]
    [InlineData(3, 31)]
    [InlineData(6, 30)]
    [InlineData(12, 25)]
    public void Age_Should_Calculate_Correctly_For_Various_Birth_Dates(int month, int day)
    {
        var today = DateTimeOffset.UtcNow;
        var birthYear = today.Year - 25;
        var dateOfBirth = new DateTimeOffset(birthYear, month, day, 0, 0, 0, TimeSpan.Zero);

        var user = new User("Test User", "test@example.com", dateOfBirth, "");

        var age = user.Age;

        var expectedAge = 25;

        if (today.Month < month || (today.Month == month && today.Day < day))
        {
            expectedAge = 24;
        }

        age.Should().Be(expectedAge);
    }

    [Fact]
    public void Age_Should_Be_Consistent_When_Called_Multiple_Times()
    {
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-30);
        var user = new User("John Doe", "john@example.com", dateOfBirth, "");

        var age1 = user.Age;
        var age2 = user.Age;
        var age3 = user.Age;

        age1.Should().Be(age2);
        age2.Should().Be(age3);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_Should_Handle_Name_With_Special_Characters()
    {
        var name = "José María O'Brien-Smith";
        var email = "jose@example.com";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-30);
        var profileImage = "";

        var user = new User(name, email, dateOfBirth, profileImage);

        user.Name.Should().Be(name);
    }

    [Fact]
    public void Constructor_Should_Handle_Long_Names()
    {
        var name = new string('A', 100);
        var email = "test@example.com";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-25);
        var profileImage = "";

        var user = new User(name, email, dateOfBirth, profileImage);

        user.Name.Should().Be(name);
    }

    [Fact]
    public void Constructor_Should_Handle_Long_Profile_Image_URL()
    {
        var name = "John Doe";
        var email = "john@example.com";
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-25);
        var profileImage = "https://example.com/" + new string('a', 500) + ".jpg";

        var user = new User(name, email, dateOfBirth, profileImage);

        user.ProfileImage.Should().Be(profileImage);
    }

    [Fact]
    public void ReconstructionConstructor_Should_Not_Validate_Email_Format()
    {
        var id = Guid.NewGuid();
        var name = "Test User";
        var email = "invalidemail.com";
        var registeredAt = DateTimeOffset.UtcNow;
        var modifiedAt = DateTimeOffset.UtcNow;
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-25);
        var profileImage = "";

        var user = new User(id, name, email, registeredAt, modifiedAt, dateOfBirth, profileImage);

        user.Email.Should().Be(email);
    }

    [Fact]
    public void ReconstructionConstructor_Should_Not_Validate_Name()
    {
        var id = Guid.NewGuid();
        var name = "";
        var email = "test@example.com";
        var registeredAt = DateTimeOffset.UtcNow;
        var modifiedAt = DateTimeOffset.UtcNow;
        var dateOfBirth = DateTimeOffset.UtcNow.AddYears(-25);
        var profileImage = "";

        var user = new User(id, name, email, registeredAt, modifiedAt, dateOfBirth, profileImage);

        user.Name.Should().BeEmpty();
    }

    #endregion
}