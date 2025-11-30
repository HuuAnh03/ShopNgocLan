using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using RestSharp;
using ShopNgocLan.Models.Momo;
using ShopNgocLan.Models.Order;
using System.Security.Cryptography;
using System.Text;

namespace ShopNgocLan.Services;

public class MomoService : IMomoService
{
    private readonly IOptions<MomoOptionModel> _options;

    public MomoService(IOptions<MomoOptionModel> options)
    {
        _options = options;
    }

    public async Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(OrderInfoModel model)
    {        
        model.OrderInfo = "Khách hàng: " + model.FullName + ". Nội dung: " + model.OrderInfo;
        var rawData =
            $"partnerCode={_options.Value.PartnerCode}&accessKey={_options.Value.AccessKey}&requestId={model.OrderId}&amount={model.Amount}&orderId={model.OrderId}&orderInfo={model.OrderInfo}&returnUrl={_options.Value.ReturnUrl}&notifyUrl={_options.Value.NotifyUrl}&extraData=";

        var signature = ComputeHmacSha256(rawData, _options.Value.SecretKey);

        var client = new RestClient(_options.Value.MomoApiUrl);
        var request = new RestRequest() { Method = Method.Post };
        request.AddHeader("Content-Type", "application/json; charset=UTF-8");

        // Create an object representing the request data
        var requestData = new
        {
            accessKey = _options.Value.AccessKey,
            partnerCode = _options.Value.PartnerCode,
            requestType = _options.Value.RequestType,
            notifyUrl = _options.Value.NotifyUrl,
            returnUrl = _options.Value.ReturnUrl,
            orderId = model.OrderId,
            amount = model.Amount.ToString(),
            orderInfo = model.OrderInfo,
            requestId = model.OrderId,
            extraData = "",
            signature = signature
        };

        request.AddParameter("application/json", JsonConvert.SerializeObject(requestData), ParameterType.RequestBody);

        var response = await client.ExecuteAsync(request);

        return JsonConvert.DeserializeObject<MomoCreatePaymentResponseModel>(response.Content);
    }

    public  MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection)
    {
        // Tạo Model từ Query String, sử dụng TryGetValue để tránh lỗi KeyNotFound
        var responseModel = new MomoExecuteResponseModel
        {
            PartnerCode = collection.TryGetValue("partnerCode", out var pCode) ? pCode.ToString() : string.Empty,
            OrderId = collection.TryGetValue("orderId", out var oId) ? oId.ToString() : string.Empty,
            RequestId = collection.TryGetValue("requestId", out var rId) ? rId.ToString() : string.Empty,
            Amount = collection.TryGetValue("amount", out var amt) ? amt.ToString() : string.Empty,
            OrderInfo = collection.TryGetValue("orderInfo", out var oInfo) ? oInfo.ToString() : string.Empty,
            Message = collection.TryGetValue("message", out var msg) ? msg.ToString() : string.Empty,
            TransId = collection.TryGetValue("transId", out var tId) ? tId.ToString() : string.Empty,
            Signature = collection.TryGetValue("signature", out var sig) ? sig.ToString() : string.Empty
        };

        // Chuyển errorCode sang int an toàn
        if (collection.TryGetValue("errorCode", out var errCodeStr) && int.TryParse(errCodeStr, out int errorCode))
        {
            responseModel.ErrorCode = errorCode;
        }

        // --- 1. XÁC THỰC CHỮ KÝ (SECURITY CHECK) ---
        // Cần lấy tất cả các tham số MoMo gửi về (theo thứ tự tài liệu MoMo)
        var rawData =
            $"partnerCode={responseModel.PartnerCode}&accessKey={_options.Value.AccessKey}&requestId={responseModel.RequestId}&amount={responseModel.Amount}&orderId={responseModel.OrderId}&orderInfo={responseModel.OrderInfo}&orderType={collection["orderType"]}&transId={responseModel.TransId}&message={responseModel.Message}&localMessage={collection["localMessage"]}&responseTime={collection["responseTime"]}&errorCode={responseModel.ErrorCode}&payType={collection["payType"]}&extraData={collection["extraData"]}";

        // Tính toán lại chữ ký
        var calculatedSignature = ComputeHmacSha256(rawData, _options.Value.SecretKey);

        // So sánh chữ ký tính toán với chữ ký nhận được
        if (calculatedSignature.Equals(responseModel.Signature, StringComparison.OrdinalIgnoreCase))
        {
            responseModel.IsVerified = true;
        }
        // --- KẾT THÚC XÁC THỰC ---

        return responseModel;
    }

    private string ComputeHmacSha256(string message, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        byte[] hashBytes;

        using (var hmac = new HMACSHA256(keyBytes))
        {
            hashBytes = hmac.ComputeHash(messageBytes);
        }

        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        return hashString;
    }
}