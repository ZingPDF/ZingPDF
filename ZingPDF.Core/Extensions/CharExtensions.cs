namespace ZingPdf.Core.Extensions
{
    internal static class CharExtensions
    {
        public static bool IsInteger(this char input)
        {
            if (input < '0' || input > '9')
                    return false;

            return true;
        }
    }
}
