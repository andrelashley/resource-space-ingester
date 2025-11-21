using System.Security.Cryptography;
using System.Text;

namespace ResourceSpace.Ingester.Utils
{
    public static class Helpers
    {
        public static string Sha256(string s)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public static int ExtractRsId(string filePath)
        {
            string file = Path.GetFileNameWithoutExtension(filePath);

            // Must start with RSxxxx
            if (!file.StartsWith("RS", StringComparison.OrdinalIgnoreCase))
                return -1;

            int i = 2; // after "RS"
            var digits = new StringBuilder();

            while (i < file.Length && char.IsDigit(file[i]))
            {
                digits.Append(file[i]);
                i++;
            }

            if (digits.Length == 0)
                return -1;

            return int.Parse(digits.ToString());
        }

        public static string CleanUtf(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";

            return Encoding.UTF8.GetString(
                Encoding.Convert(
                    Encoding.GetEncoding("ISO-8859-1"),
                    Encoding.UTF8,
                    Encoding.GetEncoding("ISO-8859-1").GetBytes(s)
                )
            )
            .Replace("\u00A0", " ") // NBSP
            .Normalize(NormalizationForm.FormC);
        }
    }
}
