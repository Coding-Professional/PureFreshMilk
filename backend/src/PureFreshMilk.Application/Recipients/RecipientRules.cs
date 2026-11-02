using PureFreshMilk.Application.Common;
using PureFreshMilk.Domain.Common;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.Application.Recipients;

/// <summary>Checks shared by the recipient use cases.</summary>
internal static class RecipientRules
{
    /// <summary>Turns the millilitres in a request into a <see cref="Quantity"/>, naming the field if it is invalid.</summary>
    public static Quantity ToQuantity(int millilitres)
    {
        try
        {
            return Quantity.FromMillilitres(millilitres);
        }
        catch (DomainException exception) when (exception.Field is null)
        {
            throw new DomainException(exception.Message, "expectedMl");
        }
    }

    public static async Task EnsureNameIsFreeAsync(
        IRecipientRepository recipients,
        HouseholdId householdId,
        string name,
        RecipientId? exceptRecipient,
        CancellationToken cancellationToken)
    {
        if (await recipients.NameInUseByActiveRecipientAsync(householdId, name, exceptRecipient, cancellationToken))
        {
            throw new ConflictException(
                ErrorCodes.RecipientNameTaken,
                $"Another recipient who is not archived is already called '{name}'.");
        }
    }

    public static async Task<Recipient> RequireAsync(
        IRecipientRepository recipients, HouseholdId householdId, Guid id, CancellationToken cancellationToken)
    {
        var recipient = id == Guid.Empty
            ? null
            : await recipients.FindAsync(householdId, RecipientId.From(id), cancellationToken);

        return recipient ?? throw new NotFoundException(ErrorCodes.RecipientNotFound, "There is no such recipient.");
    }
}
