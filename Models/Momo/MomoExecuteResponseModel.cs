namespace ShopNgocLan.Models.Momo;

public class MomoExecuteResponseModel
{
    public string OrderId { get; set; }
    public string Amount { get; set; }
    public string OrderInfo { get; set; }

    // ?? THU?C TÍNH B? THI?U GÂY L?I NÀY
    public string PartnerCode { get; set; }

    // Thông tin k?t qu? và b?o m?t
    public int ErrorCode { get; set; }       // Mã l?i (0: Thành công)
    public string Message { get; set; }
    public string TransId { get; set; }       // ID giao d?ch MoMo
    public string Signature { get; set; }     // Ch? ký xác th?c

    // Tr??ng h? tr? (Dùng n?i b? trong Service/Controller)
    public bool IsVerified { get; set; } = false;

    // B? sung thêm RequestId n?u b?n c?n nó trong logic callback/notify
    public string RequestId { get; set; }

}