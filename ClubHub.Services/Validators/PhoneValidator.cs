namespace ClubHub.Application.Validators;

public class PhoneValidator
{
    public bool IsValid(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return false;

        phone = Normalize(phone);

        if (!IsDigitsOnly(phone))
            return false;

        return IsValidVietnamPhone(phone);
    }

    private string Normalize(string phone)
    {
        return phone.Replace(" ", "")
                    .Replace("-", "")
                    .Replace(".", "");
    }

    private bool IsDigitsOnly(string input)
    {
        foreach (char c in input)
        {
            if (!char.IsDigit(c))
                return false;
        }
        return true;
    }

    private bool IsValidVietnamPhone(string phone)
    {
        if (phone.StartsWith("84"))
            phone = "0" + phone.Substring(2);

        if (phone.Length != 10)
            return false;

        string[] validPrefixes = { "03", "05", "07", "08", "09" };

        foreach (var prefix in validPrefixes)
        {
            if (phone.StartsWith(prefix))
                return true;
        }

        return false;
    }
}