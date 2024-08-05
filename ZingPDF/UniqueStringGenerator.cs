namespace ZingPDF
{
    internal static class UniqueStringGenerator
    {
        public static string Generate(int length = 8)
        {
            // Generate a new GUID and convert it to a Base64 string
            var guid = Guid.NewGuid();
            var base64String = Convert.ToBase64String(guid.ToByteArray());

            // Remove non-alphanumeric characters and truncate to the desired length
            var cleanString = base64String.Replace("/", "").Replace("+", "").Replace("=", "");
            return cleanString.Substring(0, Math.Min(length, cleanString.Length));
        }
    }
}
