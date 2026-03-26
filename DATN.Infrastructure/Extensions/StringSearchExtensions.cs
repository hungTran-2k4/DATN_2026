using System;
using System.Text;
using System.Globalization;
using SD.LLBLGen.Pro.ORMSupportClasses;

namespace DATN.Infrastructure.Extensions;

public static class StringSearchExtensions
{
    /// <summary>
    /// Tạo điều kiện tìm kiếm không phân biệt dấu tiếng Việt (Sử dụng extension unaccent và ILIKE trong PostgreSQL).
    /// </summary>
    public static IPredicate UnaccentILike(this IEntityFieldCore field, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) 
            return new PredicateExpression();

        // 1. Loại bỏ dấu trên C# (giảm tải cho DB)
        string normalizedTerm = RemoveDiacritics(searchTerm).ToLowerInvariant();
        string pattern = $"%{normalizedTerm}%";

        // 2. Clone field để không ảnh hưởng đến singleton gốc
        var clonedField = CloneField(field);

        // 3. Gán function unaccent thông qua DbFunctionCall lên field cloned
        clonedField.ExpressionToApply = new DbFunctionCall("unaccent", new object[] { field });

        // 4. CaseSensitiveCollation = false trên PostgresDQE sẽ sinh ra phép toán ILIKE
        return new FieldLikePredicate(clonedField, null, pattern) { CaseSensitiveCollation = false };
    }

    private static IEntityFieldCore CloneField(IEntityFieldCore field)
    {
        // LLBLGen Pro EntityField và EntityField2 implement ICloneable
        if (field is ICloneable cloneable)
        {
            return (IEntityFieldCore)cloneable.Clone();
        }

        // Reflection dự phòng
        var cloneMethod = field.GetType().GetMethod("Clone", Type.EmptyTypes);
        if (cloneMethod != null)
        {
            return (IEntityFieldCore)cloneMethod.Invoke(field, null)!;
        }

        return field;
    }

    /// <summary>
    /// Hàm xử lý xóa bỏ dấu tiếng Việt
    /// </summary>
    public static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }
        
        // Fix riêng cho chữ Đ/đ vì FormD không phân tách được
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D');
    }
}
