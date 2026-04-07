using Daylifood.Models;
using Microsoft.AspNetCore.Http;

namespace Daylifood.Services;

public interface IVnPayService
{
    string CreatePaymentUrl(Order order, string clientIpAddress);
    VnPayCallbackResult ParseCallback(IQueryCollection query);
}
