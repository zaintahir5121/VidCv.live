using aibabag.Models;

namespace aibabag.Services;

public interface IDetailedAstrologyService
{
    Task<DetailedAstrologyInsight> GenerateDetailedInsights(User user, string zodiacSign, CancellationToken cancellationToken = default);
}

public sealed class DetailedAstrologyService(IAiTextService aiTextService) : IDetailedAstrologyService
{
    public async Task<DetailedAstrologyInsight> GenerateDetailedInsights(User user, string zodiacSign, CancellationToken cancellationToken = default)
    {
        var prompt = BuildDetailedPrompt(user, zodiacSign);
        var aiResponse = await aiTextService.GenerateAsync(prompt, string.Empty, cancellationToken);
        var lines = ParseLines(aiResponse);

        return new DetailedAstrologyInsight
        {
            UserId = user.Id,
            CalculatedAt = DateTime.UtcNow,
            FavoriteMovieGenre = Get(lines, "favorite_movie_genre"),
            MovieInsights = Get(lines, "movie_insights"),
            EntertainmentPreferences = Get(lines, "entertainment_preferences"),
            CelebrityMatch = Get(lines, "celebrity_match"),
            FavoriteCuisine = Get(lines, "favorite_cuisine"),
            FoodInsights = Get(lines, "food_insights"),
            LuckyFood = Get(lines, "lucky_food"),
            DietaryRecommendations = Get(lines, "dietary_recommendations"),
            FavoriteAnimal = Get(lines, "favorite_animal"),
            PetCompatibility = Get(lines, "pet_compatibility"),
            AnimalInsights = Get(lines, "animal_insights"),
            FriendshipTraits = Get(lines, "friendship_traits"),
            IdealFriendType = Get(lines, "ideal_friend_type"),
            SocialInsights = Get(lines, "social_insights"),
            GroupDynamics = Get(lines, "group_dynamics"),
            CareerPath = Get(lines, "career_path"),
            FutureOpportunities = Get(lines, "future_opportunities"),
            SuccessInsights = Get(lines, "success_insights"),
            BusinessVentures = Get(lines, "business_ventures"),
            InvestmentAdvice = Get(lines, "investment_advice"),
            PastKarma = Get(lines, "past_karma"),
            PastLifeInsights = Get(lines, "past_life_insights"),
            HistoricalPatterns = Get(lines, "historical_patterns"),
            MarriageInsights = Get(lines, "marriage_insights"),
            IdealPartnerType = Get(lines, "ideal_partner_type"),
            RomanceAdvice = Get(lines, "romance_advice"),
            LoveMatches = Get(lines, "love_matches"),
            MarriageCompatibility = Get(lines, "marriage_compatibility"),
            FavoriteGenres = Get(lines, "favorite_genres"),
            HobbyRecommendations = Get(lines, "hobby_recommendations"),
            CreativeOutlets = Get(lines, "creative_outlets"),
            ArtisticTendencies = Get(lines, "artistic_tendencies"),
            ChildParenting = Get(lines, "child_parenting"),
            IdealChildTraits = Get(lines, "ideal_child_traits"),
            FamilyPlanning = Get(lines, "family_planning"),
            FamilyInsights = Get(lines, "family_insights"),
            ParentalRelationships = Get(lines, "parental_relationships"),
            SiblingDynamics = Get(lines, "sibling_dynamics"),
            FamilyHarmony = Get(lines, "family_harmony"),
            JobFitness = Get(lines, "job_fitness"),
            OfficeInsights = Get(lines, "office_insights"),
            WorkStyle = Get(lines, "work_style"),
            CareerGrowth = Get(lines, "career_growth"),
            LeadershipQualities = Get(lines, "leadership_qualities"),
            BossCompatibility = Get(lines, "boss_compatibility"),
            ManagementStyle = Get(lines, "management_style"),
            LeadershipPath = Get(lines, "leadership_path"),
            ColleagueRelationships = Get(lines, "colleague_relationships"),
            TeamworkInsights = Get(lines, "teamwork_insights"),
            NetworkingAbility = Get(lines, "networking_ability"),
            FinancialOutlook = Get(lines, "financial_outlook"),
            WealthInsights = Get(lines, "wealth_insights"),
            MoneyManagement = Get(lines, "money_management"),
            LuckyMonthsForWealth = Get(lines, "lucky_months_for_wealth"),
            HealthInsights = Get(lines, "health_insights"),
            MentalWellbeing = Get(lines, "mental_wellbeing"),
            ExerciseRecommendations = Get(lines, "exercise_recommendations"),
            VulnerabilityAreas = Get(lines, "vulnerability_areas"),
            TravelInsights = Get(lines, "travel_insights"),
            LuckyDestinations = Get(lines, "lucky_destinations"),
            AdventureType = Get(lines, "adventure_type"),
            SpiritualPath = Get(lines, "spiritual_path"),
            PersonalGrowth = Get(lines, "personal_growth"),
            MeditationAdvice = Get(lines, "meditation_advice"),
            Strengths = Get(lines, "strengths"),
            Weaknesses = Get(lines, "weaknesses"),
            HiddenTalents = Get(lines, "hidden_talents"),
            PersonalityType = Get(lines, "personality_type"),
            DailyForecast = Get(lines, "daily_forecast"),
            WeeklyForecast = Get(lines, "weekly_forecast"),
            MonthlyForecast = Get(lines, "monthly_forecast"),
            YearlyForecast = Get(lines, "yearly_forecast"),
            LoveRating = GetRating(lines, "love_rating"),
            CareerRating = GetRating(lines, "career_rating"),
            HealthRating = GetRating(lines, "health_rating"),
            FinanceRating = GetRating(lines, "finance_rating"),
            FriendshipRating = GetRating(lines, "friendship_rating"),
            CreativityRating = GetRating(lines, "creativity_rating"),
            LuckRating = GetRating(lines, "luck_rating")
        };
    }

    private static Dictionary<string, string> ParseLines(string input)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = input.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            var idx = line.IndexOf(':');
            if (idx <= 0 || idx >= line.Length - 1)
            {
                continue;
            }

            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static string Get(IReadOnlyDictionary<string, string> lines, string key)
    {
        if (lines.TryGetValue(key, out var value))
        {
            return value;
        }

        return "No insight generated.";
    }

    private static int GetRating(IReadOnlyDictionary<string, string> lines, string key)
    {
        if (lines.TryGetValue(key, out var value) && int.TryParse(value, out var parsed))
        {
            return Math.Clamp(parsed, 1, 10);
        }

        return 7;
    }

    private static string BuildDetailedPrompt(User user, string zodiacSign)
    {
        var birthDate = user.DateOfBirth?.ToString("yyyy-MM-dd") ?? "unknown";
        return $"""
Provide astrology insights for a user in strict key:value format, one per line.
No markdown, no numbering, no extra text.
User:
- Name: {user.FullName}
- Birthday: {birthDate}
- Zodiac sign: {zodiacSign}
- Chinese zodiac: {user.ChineseZodiac}
Return exactly these keys:
favorite_movie_genre, movie_insights, entertainment_preferences, celebrity_match,
favorite_cuisine, food_insights, lucky_food, dietary_recommendations,
favorite_animal, pet_compatibility, animal_insights,
friendship_traits, ideal_friend_type, social_insights, group_dynamics,
career_path, future_opportunities, success_insights, business_ventures, investment_advice,
past_karma, past_life_insights, historical_patterns,
marriage_insights, ideal_partner_type, romance_advice, love_matches, marriage_compatibility,
favorite_genres, hobby_recommendations, creative_outlets, artistic_tendencies,
child_parenting, ideal_child_traits, family_planning,
family_insights, parental_relationships, sibling_dynamics, family_harmony,
job_fitness, office_insights, work_style, career_growth, leadership_qualities,
boss_compatibility, management_style, leadership_path,
colleague_relationships, teamwork_insights, networking_ability,
financial_outlook, wealth_insights, money_management, lucky_months_for_wealth,
health_insights, mental_wellbeing, exercise_recommendations, vulnerability_areas,
travel_insights, lucky_destinations, adventure_type,
spiritual_path, personal_growth, meditation_advice,
strengths, weaknesses, hidden_talents, personality_type,
daily_forecast, weekly_forecast, monthly_forecast, yearly_forecast,
love_rating, career_rating, health_rating, finance_rating, friendship_rating, creativity_rating, luck_rating.
Each value should be short (max 18 words). Ratings must be integers 1-10.
""";
    }
}
