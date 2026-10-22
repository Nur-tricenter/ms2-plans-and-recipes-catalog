using Joseco.DDD.Core.Abstractions;

namespace Catalog.Domain.Event.MealPlan;

/// <summary>
/// A meal plan was created.
/// </summary>
/// <remarks>
/// Carries the author's id only. The user NAME is deliberately absent: the catalog projects it
/// from the identity service's own event, and repeating it here would publish a second copy that
/// could contradict the first.
/// <para>
/// <see cref="AuthorId"/> is nullable for the seeded catalogue, which has no author. It is not
/// nullable for anything a user creates — the command requires it.
/// </para>
/// <para>
/// Single fully parameterized constructor. The outbox persists events through a Newtonsoft
/// converter with <c>TypeNameHandling.All</c>, which prefers a public parameterless constructor and
/// would then build this record with every get-only property left at its default — silently, with
/// no error. See <c>FoodCreated</c>.
/// </para>
/// </remarks>
public record MealPlanCreated : DomainEvent
{
    public Guid PlanId { get; }
    public string Name { get; }
    public string DurationType { get; }
    public int TotalDays { get; }
    public int MealsPerDay { get; }

    /// <summary>
    /// The owning account's id, or null when the plan is a seeded sample with no author.
    /// </summary>
    public Guid? AuthorId { get; }

    public MealPlanCreated(
        Guid planId,
        string name,
        string durationType,
        int totalDays,
        int mealsPerDay,
        Guid? authorId)
    {
        OccuredOn = DateTime.UtcNow;
        PlanId = planId;
        Name = name;
        DurationType = durationType;
        TotalDays = totalDays;
        MealsPerDay = mealsPerDay;
        AuthorId = authorId;
    }
}