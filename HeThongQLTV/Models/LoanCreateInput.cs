using System.ComponentModel.DataAnnotations;
namespace HeThongQLTV.Models;

public class LoanCreateInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn độc giả.")]
    public int MemberId { get; set; }
    public List<LoanBookInput> Items { get; set; } = [];
}

public class LoanBookInput
{
    [Range(1, int.MaxValue)]
    public int BookId { get; set; }
    [Range(0, 10000, ErrorMessage = "Số lượng mượn phải là số nguyên từ 0 đến 10.000.")]
    public int Quantity { get; set; }
}
