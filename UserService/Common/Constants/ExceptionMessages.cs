namespace UserService.Common.Constants
{
    public class ExceptionMessages
    {
        public static class Auth
        {
            public const string InvalidCredentials = "Invalid username or password.";
            public const string UserAlreadyExists = "User with given username or email already exists.";
        }

        public static class User
        {
            public const string UserNotFound = "User not found.";
            public const string UsernameTaken = "Username is already taken.";
            public const string EmailTaken = "Email is already taken.";
        }
    }
}
