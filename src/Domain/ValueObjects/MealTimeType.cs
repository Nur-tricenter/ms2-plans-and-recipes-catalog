namespace Catalog.Domain.ValueObjects
{
    public enum MealTimeType
    {
        Breakfast = 1,
        MidMorning = 2,
        Lunch = 3,
        Snack = 4,
        Dinner = 5
    }

    public static class MealTimeTypeExtensions
    {
        public static string ToDisplayName(this MealTimeType type) => type switch
        {
            MealTimeType.Breakfast => "Breakfast",
            MealTimeType.MidMorning => "Mid Morning",
            MealTimeType.Lunch => "Lunch",
            MealTimeType.Snack => "Snack",
            MealTimeType.Dinner => "Dinner",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}