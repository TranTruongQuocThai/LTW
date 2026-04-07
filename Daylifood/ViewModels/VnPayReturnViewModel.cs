using Daylifood.Models;

namespace Daylifood.ViewModels;

public class VnPayReturnViewModel
{
    public Order? Order { get; set; }
    public VnPayCallbackResult Callback { get; set; } = new();
}
