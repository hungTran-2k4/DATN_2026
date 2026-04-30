using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace DATN.Application.Common;

public static class SlugHelper
{
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Chuyển sang chữ thường
        string slug = text.ToLowerInvariant().Trim();

        // Thay thế các ký tự tiếng Việt có dấu
        slug = ReplaceVietnameseChars(slug);

        // Thay thế các ký tự không phải chữ cái hoặc số bằng dấu gạch ngang
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

        // Thay thế nhiều khoảng trắng liên tiếp bằng một khoảng trắng
        slug = Regex.Replace(slug, @"\s+", " ").Trim();

        // Thay thế khoảng trắng bằng dấu gạch ngang
        slug = slug.Replace(' ', '-');

        // Loại bỏ các dấu gạch ngang dư thừa
        slug = Regex.Replace(slug, @"-+", "-");

        return slug.Trim('-');
    }

    private static string ReplaceVietnameseChars(string text)
    {
        string[] vietnameseSigns = new string[]
        {
            "aAeEoOuUiIyYdD",
            "áàảãạâấầẩẫậăắằẳẵặ",
            "ÁÀẢÃẠÂẤẦẨẪẬĂẮẰẲẴẶ",
            "éèẻẽẹêếềểễệ",
            "ÉÈẺẼẸÊẾỀỂỄỆ",
            "óòỏõọôốồổỗộơớờởỡợ",
            "ÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢ",
            "úùủũụưứừửữự",
            "ÚÙỦŨỤƯỨỪỬỮỰ",
            "íìỉĩị",
            "ÍÌỈĨỊ",
            "ýỳỷỹỵ",
            "ÝỲỶỸỴ",
            "đ",
            "Đ"
        };

        for (int i = 1; i < vietnameseSigns.Length; i++)
        {
            for (int j = 0; j < vietnameseSigns[i].Length; j++)
            {
                text = text.Replace(vietnameseSigns[i][j], vietnameseSigns[0][i - 1]);
            }
        }

        return text;
    }
}
