using Robust.Shared.Serialization;
using Content.Shared.Access.Components;
using System.Text;
namespace Content.Shared.Cargo
{
    [NetSerializable, Serializable]
    public sealed class CargoOrderData
    {
        public int OrderNumber;
        public string ProductId;
        public int Amount;
        public string Requester;
        // public String RequesterRank; // TODO Figure out how to get Character ID card data
        // public int RequesterId;
        public string Reason;
        public bool Approved => Approver is not null;
        public string? Approver;

        public CargoOrderData(int orderNumber, string productId, int amount, string requester, string reason)
        {
            OrderNumber = orderNumber;
            ProductId = productId;
            Amount = amount;
            Requester = requester;
            Reason = reason;
        }

        public void SetApproverData(IdCardComponent? idCard)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(idCard?.FullName))
            {
                sb.Append($"{idCard.FullName} ");
            }
            if (!string.IsNullOrWhiteSpace(idCard?.JobTitle))
            {
                sb.Append($"({idCard.JobTitle})");
            }

            if (sb.Length > 0)
            {
                Approver = sb.ToString();
            }
        }
    }
}
