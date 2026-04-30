using System;
using System.Text;
using SD.LLBLGen.Pro.ORMSupportClasses;

namespace DATN.Infrastructure.Extensions;

public static class StringSearchExtensions
{
    /// <summary>
    /// Tìm kiếm không phân biệt dấu tiếng Việt.
    /// Tách từ khóa thành từng từ và yêu cầu TẤT CẢ các từ đều phải có mặt (AND).
    /// </summary>
    public static IPredicate UnaccentILike(this IEntityFieldCore field, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new PredicateExpression();

        var words = searchTerm.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 1)
        {
            return BuildWordPredicate(field, words[0]);
        }

        // Nhiều từ: AND tất cả lại
        var combined = new PredicateExpression();
        foreach (var word in words)
        {
            combined.AddWithAnd(BuildWordPredicate(field, word));
        }
        return combined;
    }

    private static IPredicate BuildWordPredicate(IEntityFieldCore field, string word)
    {
        string normalized = RemoveDiacritics(word).ToLowerInvariant();
        string pattern = $"%{normalized}%";

        var clonedField = CloneField(field);
        clonedField.ExpressionToApply = new DbFunctionCall("unaccent", new object[] { field });

        return new FieldLikePredicate(clonedField, null, pattern) { CaseSensitiveCollation = false };
    }

    private static IEntityFieldCore CloneField(IEntityFieldCore field)
    {
        if (field is ICloneable cloneable)
            return (IEntityFieldCore)cloneable.Clone();
        return field;
    }

    public static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        text = text.Normalize(NormalizationForm.FormC);

        string[] arr1 = { "á","à","ả","ã","ạ","â","ấ","ầ","ẩ","ẫ","ậ","ă","ắ","ằ","ẳ","ẵ","ặ",
            "đ","é","è","ẻ","ẽ","ẹ","ê","ế","ề","ể","ễ","ệ","í","ì","ỉ","ĩ","ị",
            "ó","ò","ỏ","õ","ọ","ô","ố","ồ","ổ","ỗ","ộ","ơ","ớ","ờ","ở","ỡ","ợ",
            "ú","ù","ủ","ũ","ụ","ư","ứ","ừ","ử","ữ","ự","ý","ỳ","ỷ","ỹ","ỵ" };

        string[] arr2 = { "a","a","a","a","a","a","a","a","a","a","a","a","a","a","a","a","a",
            "d","e","e","e","e","e","e","e","e","e","e","e","i","i","i","i","i",
            "o","o","o","o","o","o","o","o","o","o","o","o","o","o","o","o","o",
            "u","u","u","u","u","u","u","u","u","u","u","y","y","y","y","y" };

        for (int i = 0; i < arr1.Length; i++)
        {
            text = text.Replace(arr1[i], arr2[i]);
            text = text.Replace(arr1[i].ToUpper(), arr2[i].ToUpper());
        }
        return text;
    }
}
