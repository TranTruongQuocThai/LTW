using Daylifood.Models;
using Daylifood.Services;

namespace Daylifood.ViewModels;

public sealed class MomoReturnViewModel
{
    public Order? Order { get; init; }
    public MomoCallbackResult Callback { get; init; } = null!;
}
