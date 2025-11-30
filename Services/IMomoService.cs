using Microsoft.Extensions.Primitives;
using ShopNgocLan.Models.Momo;
using ShopNgocLan.Models.Order;

namespace ShopNgocLan.Services;

public interface IMomoService
{
    Task<MomoCreatePaymentResponseModel> CreatePaymentAsync(OrderInfoModel model);
    MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection);
}