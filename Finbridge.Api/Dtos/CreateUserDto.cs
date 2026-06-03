namespace Finbridge.Api.Dtos
{
    public class CreateUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string PlaceOfBirth { get; set; } = string.Empty;
    }
}