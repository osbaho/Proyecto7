using System.Linq;

namespace Managers
{
    /// <summary>
    /// Helper class for join code generation and validation
    /// </summary>
    public static class JoinCodeValidator
    {
        private const int CODE_LENGTH = 6;
        private const string VALID_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        /// <summary>
        /// Generates a random 6-character alphanumeric join code
        /// </summary>
        public static string GenerateRandomCode()
        {
            var random = new System.Random();
            return new string(Enumerable.Range(0, CODE_LENGTH)
                .Select(_ => VALID_CHARS[random.Next(VALID_CHARS.Length)])
                .ToArray());
        }

        /// <summary>
        /// Validates if the code is in the correct format (6 alphanumeric characters)
        /// </summary>
        public static bool IsValidFormat(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            if (code.Length != CODE_LENGTH)
                return false;

            return code.All(c => VALID_CHARS.Contains(char.ToUpper(c)));
        }
    }
}
